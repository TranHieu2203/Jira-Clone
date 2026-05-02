import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ButtonModule } from 'primeng/button';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { ProjectApiService, ProjectDetail } from '@core/api/project.service';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import { AvailableTransition, Workflow, WorkflowApiService, WorkflowStatus } from '@core/api/workflow.service';
import { AuthService } from '@core/auth/auth.service';
import { NotificationService } from '@core/notification/notification.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { firstValueFrom, forkJoin } from 'rxjs';

interface Column {
  status: WorkflowStatus;
  issues: IssueSummary[];
}

@Component({
  selector: 'app-board-page',
  standalone: true,
  imports: [
    CommonModule, RouterModule, TranslateModule,
    CdkDropListGroup, CdkDropList, CdkDrag,
    ButtonModule, AppPageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="p.name + ' — ' + ('nav.board' | translate)">
        <span class="meta">{{ totalIssues() }} {{ 'board.issues_count' | translate }}</span>
      </app-page-header>

      @if (loading()) {
        <div class="empty">{{ 'common.loading' | translate }}</div>
      } @else if (columns().length === 0) {
        <div class="empty">{{ 'board.no_workflow' | translate }}</div>
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
    }
  `,
  styles: [`
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
    /* CDK drag preview */
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
export class BoardPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly projApi = inject(ProjectApiService);
  private readonly issueApi = inject(IssueApiService);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly auth = inject(AuthService);
  private readonly notif = inject(NotificationService);
  private readonly translate = inject(TranslateService);
  private readonly statusCache = inject(StatusCacheService);

  readonly project = signal<ProjectDetail | null>(null);
  readonly workflow = signal<Workflow | null>(null);
  readonly columns = signal<Column[]>([]);
  readonly loading = signal(false);

  readonly totalIssues = computed(() =>
    this.columns().reduce((sum, c) => sum + c.issues.length, 0)
  );

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    this.loading.set(true);
    this.bootstrap(key);
  }

  ngOnDestroy(): void {}

  private async bootstrap(projectKey: string): Promise<void> {
    try {
      // 1. Find project by key (via list mine — for MVP)
      const projects = await firstValueFrom(this.projApi.listMine());
      const summary = projects.find(p => p.key === projectKey.toUpperCase());
      if (!summary) {
        this.loading.set(false);
        return;
      }

      // 2. Load project detail + workflows
      const result = await firstValueFrom(forkJoin({
        detail: this.projApi.getById(summary.id),
        workflows: this.wfApi.listByProject(summary.id)
      }));

      this.project.set(result.detail);

      const wf = result.workflows[0];
      if (!wf) {
        // No workflow yet → show empty (lazy-provision occurs on first issue create)
        this.columns.set([]);
        this.loading.set(false);
        return;
      }

      // Load full workflow (listByProject returns summary; get details for transitions)
      const fullWf = await firstValueFrom(this.wfApi.getById(wf.id));
      this.workflow.set(fullWf);
      this.statusCache.putMany(fullWf.statuses);

      // 3. Load issues for this project
      const page = await firstValueFrom(this.issueApi.search({
        projectId: summary.id,
        pageIndex: 1,
        pageSize: 200,
        sort: 'key',
        includeArchived: false
      }));

      // 4. Group issues by currentStatusId, build column list in workflow order
      const grouped = new Map<string, IssueSummary[]>();
      for (const issue of page.items) {
        const list = grouped.get(issue.currentStatusId) ?? [];
        list.push(issue);
        grouped.set(issue.currentStatusId, list);
      }
      const cols: Column[] = fullWf.statuses
        .slice()
        .sort((a, b) => a.order - b.order)
        .map(s => ({ status: s, issues: grouped.get(s.id) ?? [] }));
      this.columns.set(cols);
      this.loading.set(false);
    } catch (e) {
      this.loading.set(false);
    }
  }

  async onDrop(event: CdkDragDrop<Column>): Promise<void> {
    const targetCol = event.container.data;
    const sourceCol = event.previousContainer.data;
    const issue = event.item.data as IssueSummary;
    if (!issue) return;
    if (sourceCol.status.id === targetCol.status.id) return; // same column → ignore

    // Optimistic move
    const prevCols = this.columns();
    const updatedCols = prevCols.map(c => {
      if (c.status.id === sourceCol.status.id) {
        return { ...c, issues: c.issues.filter(i => i.id !== issue.id) };
      }
      if (c.status.id === targetCol.status.id) {
        const newIssue = { ...issue, currentStatusId: targetCol.status.id };
        return { ...c, issues: [...c.issues, newIssue] };
      }
      return c;
    });
    this.columns.set(updatedCols);

    // Resolve a transition from source.status → target.status via available transitions API.
    const userId = this.auth.user()?.id;
    if (!userId) {
      this.rollback(prevCols);
      return;
    }

    try {
      const projectId = this.project()?.id;
      if (!projectId) { this.rollback(prevCols); return; }

      const available: AvailableTransition[] = await firstValueFrom(
        this.wfApi.getAvailableTransitions(projectId, issue.issueTypeId, sourceCol.status.id, userId)
      );
      const matchingTransition = available.find(t => t.toStatusId === targetCol.status.id);
      if (!matchingTransition) {
        this.notif.error({
          messageKey: 'board.no_transition',
          traceId: '-',
          errors: null
        });
        this.rollback(prevCols);
        return;
      }

      await firstValueFrom(this.issueApi.transition(issue.id, {
        transitionId: matchingTransition.id,
        inputs: null,
        comment: null
      }));
      // Optimistic state already shows new column. Toast success comes from
      // BE's messageKey via apiResponseInterceptor.
    } catch {
      this.rollback(prevCols);
    }
  }

  private rollback(cols: Column[]): void {
    this.columns.set(cols);
  }

  initialsOf(userId: string): string {
    return userId.slice(0, 2).toUpperCase();
  }
}
