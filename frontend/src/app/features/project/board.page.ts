import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, model, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import {
  ProjectApiService,
  ProjectDetail,
  projectDetailToSummary
} from '@core/api/project.service';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import { AvailableTransition, Workflow, WorkflowApiService, WorkflowStatus } from '@core/api/workflow.service';
import { AuthService } from '@core/auth/auth.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { NotificationService } from '@core/notification/notification.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { filter, firstValueFrom, interval, switchMap } from 'rxjs';

interface Column {
  status: WorkflowStatus;
  issues: IssueSummary[];
}

interface PendingTransitionPick {
  issue: IssueSummary;
  prevIssues: IssueSummary[];
  options: AvailableTransition[];
}

interface SelectOpt<T> {
  label: string;
  value: T;
}

interface SwimlaneRow {
  key: string;
  assigneeId: string | null;
  label: string;
}

@Component({
  selector: 'app-board-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    CdkDropListGroup, CdkDropList, CdkDrag,
    ButtonModule, DialogModule, SelectModule, AppPageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="p.name + ' — ' + ('nav.board' | translate)">
        <span class="meta">{{ totalIssues() }} {{ 'board.issues_count' | translate }}</span>
      </app-page-header>

      @if (!loading() && columns().length > 0) {
        <div class="toolbar">
          <label class="filt">
            <span>{{ 'board.filter_assignee' | translate }}</span>
            <p-select [(ngModel)]="filterAssigneeId"
                      [options]="assigneeFilterOptions()"
                      optionLabel="label"
                      optionValue="value"
                      [showClear]="true"
                      [disabled]="swimlaneLayout() === 'assignee'"
                      appendTo="body"
                      styleClass="filt-select" />
          </label>
          <label class="filt">
            <span>{{ 'board.filter_issue_type' | translate }}</span>
            <p-select [(ngModel)]="filterIssueTypeId"
                      [options]="issueTypeFilterOptions()"
                      optionLabel="label"
                      optionValue="value"
                      [showClear]="true"
                      appendTo="body"
                      styleClass="filt-select" />
          </label>
          <label class="filt layout-select">
            <span>{{ 'board.layout' | translate }}</span>
            <select [(ngModel)]="swimlaneLayout" class="layout-native">
              <option value="flat">{{ 'board.layout_flat' | translate }}</option>
              <option value="assignee">{{ 'board.layout_assignee' | translate }}</option>
            </select>
          </label>
        </div>
      }

      @if (loading()) {
        <div class="empty">{{ 'common.loading' | translate }}</div>
      } @else if (columns().length === 0) {
        <div class="empty">{{ 'board.no_workflow' | translate }}</div>
      } @else if (swimlaneLayout() === 'assignee' && swimlaneRows().length === 0) {
        <div class="empty">{{ 'board.column_empty' | translate }}</div>
      } @else if (swimlaneLayout() === 'assignee') {
        @for (lane of swimlaneRows(); track lane.key) {
          <div class="swim-section">
            <div class="swim-head">{{ lane.label }}</div>
            <div class="board" cdkDropListGroup>
              @for (col of columnsForLane(lane.assigneeId); track col.status.id) {
                <div class="col">
                  <header class="col-head" [attr.data-cat]="col.status.category">
                    <span class="col-name">{{ col.status.name }}</span>
                    <span class="col-count">{{ col.issues.length }}</span>
                  </header>
                  <div class="col-body"
                       cdkDropList
                       [cdkDropListData]="col"
                       (cdkDropListDropped)="onDrop($event)">
                    @for (issue of col.issues; track issue.id) {
                      <div class="card" cdkDrag [cdkDragData]="issue">
                        <div class="card-key">
                          <a [routerLink]="['/issues', issue.key]">{{ issue.key }}</a>
                          <span class="pri pri-{{ issue.priority }}">P{{ issue.priority }}</span>
                        </div>
                        <div class="card-summary">{{ issue.summary }}</div>
                        @if (issue.assigneeId) {
                          <div class="card-assignee" [title]="issue.assigneeId">
                            {{ initialsOf(issue.assigneeId) }}
                          </div>
                        }
                      </div>
                    }
                    @if (col.issues.length === 0) {
                      <div class="col-empty">{{ 'board.column_empty' | translate }}</div>
                    }
                  </div>
                </div>
              }
            </div>
          </div>
        }
      } @else {
        <div class="board" cdkDropListGroup>
          @for (col of columns(); track col.status.id) {
            <div class="col">
              <header class="col-head" [attr.data-cat]="col.status.category">
                <span class="col-name">{{ col.status.name }}</span>
                <span class="col-count">{{ col.issues.length }}</span>
              </header>
              <div class="col-body"
                   cdkDropList
                   [cdkDropListData]="col"
                   (cdkDropListDropped)="onDrop($event)">
                @for (issue of col.issues; track issue.id) {
                  <div class="card" cdkDrag [cdkDragData]="issue">
                    <div class="card-key">
                      <a [routerLink]="['/issues', issue.key]">{{ issue.key }}</a>
                      <span class="pri pri-{{ issue.priority }}">P{{ issue.priority }}</span>
                    </div>
                    <div class="card-summary">{{ issue.summary }}</div>
                    @if (issue.assigneeId) {
                      <div class="card-assignee" [title]="issue.assigneeId">
                        {{ initialsOf(issue.assigneeId) }}
                      </div>
                    }
                  </div>
                }
                @if (col.issues.length === 0) {
                  <div class="col-empty">{{ 'board.column_empty' | translate }}</div>
                }
              </div>
            </div>
          }
        </div>
      }

      <p-dialog [(visible)]="pickVisible"
                (onHide)="onTransitionPickHide()"
                [modal]="true"
                [style]="{ width: '420px' }"
                [header]="'board.pick_transition' | translate">
        @if (pendingPick(); as pick) {
          <p class="pick-hint">{{ 'board.pick_transition_hint' | translate }}</p>
          <div class="pick-actions">
            @for (t of pick.options; track t.id) {
              <button pButton type="button" class="pick-btn"
                      (click)="confirmTransitionPick(t)"
                      [label]="t.name + ' → ' + t.toStatusName"></button>
            }
          </div>
        }
      </p-dialog>
    }
  `,
  styles: [`
    .toolbar {
      display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-end;
      margin-bottom: 16px; padding: 0 4px;
    }
    .filt { display: flex; flex-direction: column; gap: 6px; font-size: 11px;
      text-transform: uppercase; letter-spacing: 0.5px; color: var(--c-text-muted);
      min-width: 200px;
    }
    ::ng-deep .filt-select { width: 100%; max-width: 280px; }
    .layout-native {
      width: 100%; max-width: 280px; padding: 8px 10px; border-radius: var(--radius);
      border: 1px solid var(--c-border); background: var(--c-surface); color: var(--c-text);
      font-size: 14px;
    }
    .swim-section { margin-bottom: 20px; }
    .swim-head {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px 4px;
    }
    .pick-hint { font-size: 13px; color: var(--c-text-muted); margin: 0 0 12px; }
    .pick-actions { display: flex; flex-direction: column; gap: 8px; }
    .pick-btn { width: 100%; justify-content: center; }
    .meta { font-size: 13px; color: var(--c-text-muted); }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
    .board {
      display: flex; gap: 12px; align-items: flex-start;
      overflow-x: auto; padding-bottom: 8px;
      min-height: calc(100vh - 200px);
    }
    .col {
      flex: 0 0 280px;
      background: var(--c-surface-2);
      border-radius: var(--radius);
      display: flex; flex-direction: column;
      max-height: calc(100vh - 180px);
    }
    .col-head {
      display: flex; align-items: center; justify-content: space-between;
      padding: 10px 12px;
      border-bottom: 2px solid var(--c-border);
      font-size: 12px; font-weight: 600;
      text-transform: uppercase; letter-spacing: 0.5px;
      color: var(--c-text-muted);
    }
    .col-head[data-cat="2"] { border-bottom-color: #3b82f6; }
    .col-head[data-cat="3"] { border-bottom-color: #10b981; }
    .col-name { color: var(--c-text); }
    .col-count {
      background: var(--c-surface-3); padding: 1px 8px; border-radius: 10px;
      font-size: 10px; color: var(--c-text-muted);
    }
    .col-body {
      flex: 1; padding: 8px; display: flex; flex-direction: column; gap: 6px;
      overflow-y: auto;
      min-height: 100px;
    }
    .card {
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 10px;
      cursor: grab;
      box-shadow: var(--shadow-sm);
      transition: box-shadow 0.1s, transform 0.1s;
    }
    .card:hover { box-shadow: var(--shadow-md); }
    .card:active { cursor: grabbing; }
    .card-key {
      display: flex; align-items: center; justify-content: space-between;
      font-size: 11px; margin-bottom: 6px;
    }
    .card-key a {
      font-family: monospace; color: var(--c-text-muted);
      text-decoration: none;
    }
    .card-key a:hover { color: var(--c-text); text-decoration: underline; }
    .card-summary {
      font-size: 13px; color: var(--c-text);
      line-height: 1.4;
      display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .card-assignee {
      margin-top: 8px;
      display: inline-flex; align-items: center; justify-content: center;
      width: 22px; height: 22px; border-radius: 50%;
      background: var(--c-text); color: var(--c-on-primary);
      font-size: 9px; font-weight: 600;
    }
    .pri {
      display: inline-block; width: 22px; line-height: 16px; text-align: center;
      border-radius: 3px; font-size: 9px; font-weight: 700;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .pri-4, .pri-5 { background: var(--c-accent-danger); color: white; }
    .col-empty {
      padding: 16px; text-align: center; color: var(--c-text-subtle);
      font-size: 12px; font-style: italic;
    }
    .cdk-drag-preview {
      box-shadow: var(--shadow-md);
      border-radius: var(--radius);
    }
    .cdk-drag-placeholder {
      opacity: 0.3;
    }
    .cdk-drag-animating {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
    .col-body.cdk-drop-list-dragging .card:not(.cdk-drag-placeholder) {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
  `]
})
export class BoardPageComponent implements OnInit {
  /** Khoảng làm mới board khi có workflow (MVP polling, defer SignalR). */
  private static readonly PollIntervalMs = 30_000;

  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly projApi = inject(ProjectApiService);
  private readonly issueApi = inject(IssueApiService);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly auth = inject(AuthService);
  private readonly notif = inject(NotificationService);
  private readonly statusCache = inject(StatusCacheService);
  private readonly workspaceCtx = inject(WorkspaceContextService);

  readonly project = signal<ProjectDetail | null>(null);
  readonly workflow = signal<Workflow | null>(null);
  readonly issuesAll = signal<IssueSummary[]>([]);
  readonly loading = signal(false);

  readonly filterAssigneeId = model<string | null>(null);
  readonly filterIssueTypeId = model<string | null>(null);
  /** flat = một hàng cột; assignee = một swimlane mỗi người được giao (+ hàng chưa giao). */
  readonly swimlaneLayout = model<'flat' | 'assignee'>('flat');

  readonly pickVisible = model(false);
  readonly pendingPick = signal<PendingTransitionPick | null>(null);
  private pickCommitted = false;
  private issuePollingStarted = false;

  readonly columns = computed(() => this.buildColumns(this.baseIssuesForBoard()));

  readonly swimlaneRows = computed((): SwimlaneRow[] => {
    if (this.swimlaneLayout() !== 'assignee') return [];

    const issues = this.baseIssuesForBoard();
    const ids = new Set<string>();
    let unassigned = false;
    for (const i of issues) {
      if (!i.assigneeId) unassigned = true;
      else ids.add(i.assigneeId);
    }

    const rows: SwimlaneRow[] = [];
    if (unassigned)
      rows.push({ key: '__unassigned', assigneeId: null, label: '—' });

    for (const id of [...ids].sort()) {
      rows.push({
        key: id,
        assigneeId: id,
        label: `${this.initialsOf(id)} · ${id.slice(0, 8)}…`
      });
    }

    return rows;
  });

  readonly totalIssues = computed(() => this.baseIssuesForBoard().length);

  private baseIssuesForBoard(): IssueSummary[] {
    let issues = this.issuesAll();
    const ft = this.filterIssueTypeId();
    if (ft) issues = issues.filter((i) => i.issueTypeId === ft);

    const fa = this.filterAssigneeId();
    if (this.swimlaneLayout() === 'flat' && fa)
      issues = issues.filter((i) => i.assigneeId === fa);

    return issues;
  }

  private buildColumns(issues: IssueSummary[]): Column[] {
    const fullWf = this.workflow();
    if (!fullWf) return [];

    const grouped = new Map<string, IssueSummary[]>();
    for (const issue of issues) {
      const list = grouped.get(issue.currentStatusId) ?? [];
      list.push(issue);
      grouped.set(issue.currentStatusId, list);
    }

    return fullWf.statuses
      .slice()
      .sort((a, b) => a.order - b.order)
      .map((s) => ({ status: s, issues: grouped.get(s.id) ?? [] }));
  }

  columnsForLane(assigneeId: string | null): Column[] {
    let issues = this.baseIssuesForBoard();
    issues = issues.filter((i) =>
      assigneeId === null ? !i.assigneeId : i.assigneeId === assigneeId);
    return this.buildColumns(issues);
  }

  readonly assigneeFilterOptions = computed((): SelectOpt<string | null>[] => {
    const ids = new Set<string>();
    for (const i of this.issuesAll()) {
      if (i.assigneeId) ids.add(i.assigneeId);
    }
    const opts: SelectOpt<string | null>[] = [];
    for (const id of [...ids].sort()) {
      opts.push({
        label: `${this.initialsOf(id)} · ${id.slice(0, 8)}…`,
        value: id
      });
    }
    return opts;
  });

  readonly issueTypeFilterOptions = computed((): SelectOpt<string | null>[] => {
    const p = this.project();
    if (!p) return [];
    return p.issueTypes
      .filter((t) => !t.isSubtask)
      .sort((a, b) => a.order - b.order)
      .map((t) => ({ label: t.name, value: t.id }));
  });

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    this.loading.set(true);
    void this.bootstrap(key);
  }

  private async bootstrap(projectKey: string): Promise<void> {
    try {
      const detail = await firstValueFrom(this.projApi.getDetailForMemberByKey(projectKey));
      this.workspaceCtx.setProject(projectDetailToSummary(detail));
      this.project.set(detail);

      const workflows = await firstValueFrom(this.wfApi.listByProject(detail.id));

      const wf = workflows[0];
      if (!wf) {
        this.issuesAll.set([]);
        this.workflow.set(null);
        this.loading.set(false);
        return;
      }

      const fullWf = await firstValueFrom(this.wfApi.getById(wf.id));
      this.workflow.set(fullWf);
      this.statusCache.putMany(fullWf.statuses);

      const page = await firstValueFrom(this.issueApi.search({
        projectId: detail.id,
        pageIndex: 1,
        pageSize: 200,
        sort: 'key',
        includeArchived: false
      }));

      this.issuesAll.set(page.items);
      this.loading.set(false);
      this.startIssuePolling();
    } catch {
      this.loading.set(false);
    }
  }

  /** Đồng bộ danh sách issue định kỳ (không toast; lỗi bỏ qua). */
  private startIssuePolling(): void {
    if (this.issuePollingStarted) return;
    this.issuePollingStarted = true;
    interval(BoardPageComponent.PollIntervalMs)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter(() => {
          const hasProject = this.project() !== null;
          const hasWf = this.workflow() !== null;
          return hasProject && hasWf && !this.loading() && !this.pickVisible();
        }),
        switchMap(() => {
          const projectId = this.project()!.id;
          return this.issueApi.search({
            projectId,
            pageIndex: 1,
            pageSize: 200,
            sort: 'key',
            includeArchived: false
          });
        })
      )
      .subscribe({
        next: (page) => this.issuesAll.set(page.items),
        error: () => {
          /* silent — không làm gián đoạn board */
        }
      });
  }

  async onDrop(event: CdkDragDrop<Column>): Promise<void> {
    const targetCol = event.container.data;
    const sourceCol = event.previousContainer.data;
    const issue = event.item.data as IssueSummary;
    if (!issue) return;
    if (sourceCol.status.id === targetCol.status.id) return;

    const prevIssues = this.issuesAll().map((i) => ({ ...i }));
    this.issuesAll.update((list) =>
      list.map((i) =>
        i.id === issue.id ? { ...i, currentStatusId: targetCol.status.id } : i
      )
    );

    const userId = this.auth.user()?.id;
    if (!userId) {
      this.issuesAll.set(prevIssues);
      return;
    }

    const projectId = this.project()?.id;
    if (!projectId) {
      this.issuesAll.set(prevIssues);
      return;
    }

    try {
      const available: AvailableTransition[] = await firstValueFrom(
        this.wfApi.getAvailableTransitions(projectId, issue.issueTypeId, sourceCol.status.id, userId)
      );
      const matching = available.filter((t) => t.toStatusId === targetCol.status.id);
      if (matching.length === 0) {
        this.notif.error({
          messageKey: 'board.no_transition',
          traceId: '-',
          errors: null
        });
        this.issuesAll.set(prevIssues);
        return;
      }

      if (matching.length === 1) {
        await this.runTransition(issue, matching[0].id, prevIssues);
        return;
      }

      this.pickCommitted = false;
      this.pendingPick.set({ issue, prevIssues, options: matching });
      this.pickVisible.set(true);
    } catch {
      this.issuesAll.set(prevIssues);
    }
  }

  onTransitionPickHide(): void {
    if (!this.pickCommitted) {
      const pick = this.pendingPick();
      if (pick) this.issuesAll.set(pick.prevIssues);
    }
    this.pickCommitted = false;
    this.pendingPick.set(null);
  }

  confirmTransitionPick(t: AvailableTransition): void {
    const pick = this.pendingPick();
    if (!pick) return;
    this.pickCommitted = true;
    const issue = pick.issue;
    const snapshot = pick.prevIssues;
    this.pendingPick.set(null);
    this.pickVisible.set(false);
    void this.runTransition(issue, t.id, snapshot);
  }

  private async runTransition(
    issue: IssueSummary,
    transitionId: string,
    rollbackSnapshot: IssueSummary[]
  ): Promise<void> {
    try {
      await firstValueFrom(this.issueApi.transition(issue.id, {
        transitionId,
        inputs: null,
        comment: null
      }));
    } catch {
      this.issuesAll.set(rollbackSnapshot);
    }
  }

  initialsOf(userId: string): string {
    return userId.slice(0, 2).toUpperCase();
  }
}
