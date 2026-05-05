import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import {
  ProjectApiService,
  projectDetailToSummary
} from '@core/api/project.service';
import { PagedList } from '@shared/models/api-response';
import { CreateIssueDialogComponent } from './create-issue.dialog';
import { SavedFilterPickerComponent } from './saved-filter-picker.component';
import { BulkEditToolbarComponent } from './bulk-edit-toolbar.component';
import { StatusCacheService } from '@core/api/status-cache.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { CheckboxModule } from 'primeng/checkbox';

@Component({
  selector: 'app-issues-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    InputTextModule,
    AppPageHeaderComponent,
    CreateIssueDialogComponent,
    SavedFilterPickerComponent,
    BulkEditToolbarComponent,
    CheckboxModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="listTitleKey() | translate">
      <button pButton (click)="dialogVisible.set(true)" [label]="'issue.create' | translate"></button>
    </app-page-header>

    <app-saved-filter-picker
      [currentJql]="jqlFilter"
      (applied)="onSavedFilterApplied($event)" />

    <div class="filters">
      <input
        pInputText
        [(ngModel)]="textFilter"
        (keyup.enter)="reload()"
        [placeholder]="'issue.search_placeholder' | translate" />
      <input
        pInputText
        class="jql-input"
        [(ngModel)]="jqlFilter"
        (keyup.enter)="reload()"
        [placeholder]="'issue.jql_placeholder' | translate" />
      <button pButton (click)="reload()" [label]="'common.search' | translate"></button>
    </div>

    <app-bulk-edit-toolbar
      [selectedIds]="selectedIds()"
      (clear)="clearSelection()"
      (applied)="onBulkApplied()" />

    <p-table [value]="page()?.items ?? []" [loading]="loading()" stripedRows>
      <ng-template pTemplate="header">
        <tr>
          <th class="w-check">
            <p-checkbox [ngModel]="allChecked()" [binary]="true"
                        (onChange)="toggleAll($event.checked)" />
          </th>
          <th class="w-key">{{ 'issue.key' | translate }}</th>
          <th>{{ 'issue.summary' | translate }}</th>
          <th class="w-status">{{ 'issue.status' | translate }}</th>
          <th class="w-pri">{{ 'issue.priority' | translate }}</th>
          <th class="w-date">{{ 'issue.created_at' | translate }}</th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-r>
        <tr>
          <td class="w-check">
            <p-checkbox [ngModel]="isSelected(r.id)" [binary]="true"
                        (onChange)="toggleOne(r.id, $event.checked)" />
          </td>
          <td>
            <a [routerLink]="['/issues', r.key]"><code>{{ r.key }}</code></a>
          </td>
          <td>{{ r.summary }}</td>
          <td>
            <span class="status-pill" [attr.data-cat]="statusCat(r.currentStatusId)">
              {{ statusName(r.currentStatusId) }}
            </span>
          </td>
          <td><span class="pri pri-{{ r.priority }}">P{{ r.priority }}</span></td>
          <td>{{ r.createdAt | date: 'short' }}</td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr>
          <td colspan="6" class="empty">{{ 'issue.empty' | translate }}</td>
        </tr>
      </ng-template>
    </p-table>

    <app-create-issue-dialog
      [fixedProjectId]="fixedProjectId()"
      [(visible)]="dialogVisible"
      (created)="onIssueCreated()" />
  `,
  styles: [`
    .filters { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 16px; align-items: center; }
    .filters input { flex: 1 1 220px; max-width: 420px; }
    .jql-input { flex: 2 1 280px; max-width: 560px; font-family: ui-monospace, monospace; font-size: 12px; }
    .w-check { width: 32px; padding-left: 8px; padding-right: 4px; }
    .w-key { width: 100px; }
    .w-status { width: 130px; }
    .w-pri { width: 70px; }
    .w-date { width: 140px; }
    code { font-size: 12px; color: var(--c-text); }
    .status-pill {
      display: inline-block; padding: 2px 8px; border-radius: 10px;
      font-size: 11px; font-weight: 600;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .status-pill[data-cat="1"] { background: var(--c-surface-3); color: var(--c-text-muted); }
    .status-pill[data-cat="2"] { background: #dbeafe; color: #1e40af; }
    .status-pill[data-cat="3"] { background: #d1fae5; color: #065f46; }
    .pri {
      display: inline-block; width: 24px; height: 22px; line-height: 22px; text-align: center;
      border-radius: 3px; font-size: 11px; font-weight: 600;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .pri-4, .pri-5 { background: var(--c-accent-danger); color: white; }
    .empty { text-align: center; color: var(--c-text-muted); padding: 32px; }
  `]
})
export class IssuesPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(IssueApiService);
  private readonly projApi = inject(ProjectApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly statusCache = inject(StatusCacheService);
  private readonly ctx = inject(WorkspaceContextService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly page = signal<PagedList<IssueSummary> | null>(null);
  readonly loading = signal(false);
  readonly dialogVisible = signal(false);
  readonly fixedProjectId = signal<string | null>(null);
  readonly listTitleKey = signal('issue.title');

  // F5: bulk selection state.
  readonly selectedIdsSet = signal<ReadonlySet<string>>(new Set());
  readonly selectedIds = computed(() => Array.from(this.selectedIdsSet()));
  readonly allChecked = computed(() => {
    const items = this.page()?.items ?? [];
    if (items.length === 0) return false;
    const sel = this.selectedIdsSet();
    return items.every(i => sel.has(i.id));
  });

  textFilter = '';
  jqlFilter = '';
  private scopedToProject = false;

  ngOnInit(): void {
    const variant = this.route.snapshot.data['issueListVariant'];
    if (variant === 'backlog') {
      this.listTitleKey.set('nav.backlog');
    }
    /** Global /issues: default JQL so list is only issues assigned to the logged-in user (see JqlLiteParser on BE). */
    if (variant === 'my') {
      this.listTitleKey.set('nav.my_issues');
      this.jqlFilter = 'assignee = currentUser()';
    }

    const projectKey = this.route.snapshot.paramMap.get('projectKey');
    if (projectKey) {
      this.scopedToProject = true;
      this.projApi.getDetailForMemberByKey(projectKey).subscribe({
        next: (detail) => {
          this.fixedProjectId.set(detail.id);
          this.ctx.setProject(projectDetailToSummary(detail));
          this.reload();
        },
        error: () => this.reload()
      });
    } else {
      this.reload();
    }
  }

  ngOnDestroy(): void {
    if (this.scopedToProject) {
      this.ctx.setProject(null);
    }
  }

  reload(): void {
    this.loading.set(true);
    this.api
      .search({
        projectId: this.fixedProjectId(),
        textSearch: this.textFilter || null,
        jql: this.jqlFilter.trim() || null,
        pageIndex: 1,
        pageSize: 50,
        sort: 'key'
      })
      .subscribe({
        next: (p) => {
          this.page.set(p);
          this.loading.set(false);
          void this.warmStatusCacheFor(p.items).then(() => this.cdr.markForCheck());
        },
        error: () => this.loading.set(false)
      });
  }

  onIssueCreated(): void {
    this.reload();
  }

  /** F2: SavedFilterPicker emit JQL → set vào ô input + reload luôn. */
  onSavedFilterApplied(jql: string): void {
    this.jqlFilter = jql;
    this.reload();
  }

  // ── F5: Bulk selection ─────────────────────────────────────────

  isSelected(id: string): boolean {
    return this.selectedIdsSet().has(id);
  }

  toggleOne(id: string, checked: boolean): void {
    const next = new Set(this.selectedIdsSet());
    if (checked) next.add(id); else next.delete(id);
    this.selectedIdsSet.set(next);
  }

  toggleAll(checked: boolean): void {
    if (!checked) {
      this.selectedIdsSet.set(new Set());
      return;
    }
    const items = this.page()?.items ?? [];
    this.selectedIdsSet.set(new Set(items.map(i => i.id)));
  }

  clearSelection(): void {
    this.selectedIdsSet.set(new Set());
  }

  /** Sau bulk apply: clear selection + reload (issue đã đổi status/assignee/labels…). */
  onBulkApplied(): void {
    this.clearSelection();
    this.reload();
  }

  /** Preload workflow statuses per project so cross-project search resolves status names. */
  private async warmStatusCacheFor(items: readonly IssueSummary[]): Promise<void> {
    const ids = new Set<string>();
    const fixed = this.fixedProjectId();
    if (fixed) ids.add(fixed);
    for (const i of items) {
      if (i.projectId) ids.add(i.projectId);
    }
    await Promise.all([...ids].map((id) => this.statusCache.ensureProjectLoaded(id)));
  }

  statusName(statusId: string): string {
    return this.statusCache.nameOf(statusId) ?? statusId.slice(0, 8) + '…';
  }

  statusCat(statusId: string): number {
    return this.statusCache.categoryOf(statusId) ?? 1;
  }
}
