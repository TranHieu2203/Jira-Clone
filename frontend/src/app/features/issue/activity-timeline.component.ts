import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { switchMap, tap } from 'rxjs';
import { ActivityApiService, ActivityItem } from '@core/api/activity.service';

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
                <span class="actor">{{ a.actorUserId.slice(0, 8) }}…</span>
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
export class ActivityTimelineComponent {
  private readonly api = inject(ActivityApiService);

  readonly issueId = input.required<string>();

  readonly items = signal<ActivityItem[]>([]);
  readonly loading = signal(false);

  constructor() {
    toObservable(this.issueId)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap((id) => this.api.listByIssue(id, 1, 100)),
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

  /** ngx-translate interpolation object — primitives only (no nested objects). */
  interp(payload: Record<string, unknown> | null): Record<string, string | number> {
    if (!payload) return {};
    const out: Record<string, string | number> = {};
    for (const [key, v] of Object.entries(payload)) {
      if (v === null || v === undefined) {
        out[key] = '';
      } else if (typeof v === 'object') {
        out[key] = JSON.stringify(v);
      } else if (typeof v === 'number') {
        out[key] = v;
      } else {
        out[key] = String(v);
      }
    }
    return out;
  }
}
