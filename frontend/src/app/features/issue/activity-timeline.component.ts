import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { ActivityApiService, ActivityItem } from '@core/api/activity.service';
import { CustomFieldApiService, CustomField } from '@core/api/custom-field.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { UserApiService, UserSummary } from '@core/api/user.service';
import { IssueThreadRealtimePayload, WorkspaceHubService } from '@core/realtime/workspace-hub.service';

@Component({
  selector: 'app-activity-timeline',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="timeline">
      <h3>{{ 'activity.title' | translate }}</h3>

      @if (loading()) {
        <div class="empty">{{ 'common.loading' | translate }}</div>
      } @else if (items().length === 0) {
        <div class="empty">{{ 'activity.empty' | translate }}</div>
      }

      <ul class="list">
        @for (a of items(); track a.id) {
          <li class="row">
            <time>{{ a.occurredAt | date:'short' }}</time>
            <div class="body">
              <span class="msg">{{ a.kind | translate: interp(a.payload) }}</span>
              @if (a.actorUserId) {
                <span class="actor">{{ actorLabel(a.actorUserId) }}</span>
              }
            </div>
          </li>
        }
      </ul>
    </section>
  `,
  styles: [`
    .timeline { margin-top: 8px; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px;
    }
    .empty { color: var(--c-text-muted); font-size: 13px; padding: 8px 0; }
    .list { list-style: none; margin: 0; padding: 0; }
    .row {
      display: grid; grid-template-columns: 130px 1fr; gap: 12px;
      padding: 10px 0; border-bottom: 1px solid var(--c-border); font-size: 13px;
    }
    time { color: var(--c-text-muted); font-size: 12px; }
    .body { display: flex; flex-wrap: wrap; gap: 8px; align-items: baseline; }
    .msg { color: var(--c-text); }
    .actor { font-family: monospace; font-size: 11px; color: var(--c-text-muted); }
  `]
})
export class ActivityTimelineComponent implements OnInit, OnDestroy {
  private readonly api = inject(ActivityApiService);
  private readonly users = inject(UserApiService);
  private readonly cfApi = inject(CustomFieldApiService);
  private readonly statusCache = inject(StatusCacheService);
  private readonly hub = inject(WorkspaceHubService);

  readonly issueId = input.required<string>();

  readonly items = signal<ActivityItem[]>([]);
  readonly loading = signal(false);

  private readonly userCache = new Map<string, UserSummary>();
  private readonly fieldNameById = signal<Record<string, string>>({});

  /**
   * F12: realtime — bất kỳ IssueEvent nào cũng có thể tạo activity entry
   * (status/assignee/comment/link/attachment), nên reload luôn list.
   * Filter theo issueId hiện tại tránh reload khi event khác issue.
   */
  private readonly issueRealtimeHandler = (_payload: IssueThreadRealtimePayload): void => {
    const id = this.issueId();
    if (id) this.fetch(id);
  };

  constructor() {
    toObservable(this.issueId)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap((id) =>
          this.api.listByIssue(id, 1, 100).pipe(
            // Preload field definitions once so we can map GUIDs -> display names.
            switchMap((page) =>
              this.cfApi.list().pipe(
                tap((fields) => this.setFieldNameMap(fields)),
                map(() => page),
                catchError(() => of(page))
              )
            )
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (page) => {
          this.items.set(page.items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  ngOnInit(): void {
    this.hub.addIssueListener(this.issueRealtimeHandler);
  }

  ngOnDestroy(): void {
    this.hub.removeIssueListener(this.issueRealtimeHandler);
  }

  private fetch(id: string): void {
    this.api.listByIssue(id, 1, 100).subscribe({
      next: (page) => this.items.set(page.items),
      error: () => { /* keep current */ },
    });
  }

  actorLabel(userId: string): string {
    const cached = this.userCache.get(userId);
    if (cached) return `@${cached.userName}`;
    // Best-effort async lookup; render fallback until loaded.
    this.users.getById(userId).pipe(catchError(() => of(null))).subscribe((u) => {
      if (!u) return;
      this.userCache.set(userId, u);
      // force signal write to trigger template refresh
      this.items.set([...this.items()]);
    });
    return userId.slice(0, 8) + '…';
  }

  /** ngx-translate interpolation object — primitives only (no nested objects). */
  interp(payload: Record<string, unknown> | null): Record<string, string | number> {
    if (!payload) return {};
    const out: Record<string, string | number> = {};
    const fieldNameMap = this.fieldNameById();
    for (const [key, v] of Object.entries(payload)) {
      if (v === null || v === undefined) {
        out[key] = '';
      } else if (typeof v === 'object') {
        out[key] = JSON.stringify(v);
      } else if (typeof v === 'number') {
        out[key] = v;
      } else {
        const s = String(v);
        // Improve UX: map known GUID payload values to friendly labels.
        if ((key === 'fromStatusId' || key === 'toStatusId') && this.isGuid(s)) {
          out[key] = this.statusCache.nameOf(s) ?? s.slice(0, 8) + '…';
        } else if ((key === 'oldAssigneeId' || key === 'newAssigneeId' || key === 'userId') && this.isGuid(s)) {
          out[key] = this.actorLabel(s);
        } else if (key === 'field' && this.isGuid(s)) {
          out[key] = fieldNameMap[s] ?? s.slice(0, 8) + '…';
        } else if (key === 'transitionId' && this.isGuid(s)) {
          out[key] = s.slice(0, 8) + '…';
        } else {
          out[key] = s;
        }
      }
    }
    return out;
  }

  private setFieldNameMap(fields: CustomField[]): void {
    const map: Record<string, string> = {};
    for (const f of fields) {
      map[f.id] = f.name;
    }
    this.fieldNameById.set(map);
  }

  private isGuid(s: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(s);
  }
}
