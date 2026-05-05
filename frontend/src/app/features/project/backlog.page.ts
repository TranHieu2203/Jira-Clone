import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  model,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import {
  CreateSprintRequest,
  SprintApiService,
  SprintDto
} from '@core/api/sprint-api.service';
import {
  ProjectApiService,
  ProjectDetail,
  projectDetailToSummary
} from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { CreateIssueDialogComponent } from '@features/issue/create-issue.dialog';
import { StatusCacheService } from '@core/api/status-cache.service';
import { IssueTypeCacheService } from '@core/api/issue-type-cache.service';
import { UserCacheService } from '@core/api/user-cache.service';
import { IssueStatusBadgeComponent } from '@shared/ui/issue-status-badge.component';
import { IssuePriorityIconComponent } from '@shared/ui/issue-priority-icon.component';
import { IssueTypePillComponent } from '@shared/ui/issue-type-pill.component';
import { UserAvatarComponent } from '@shared/ui/user-avatar.component';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

const SprintPlanned = 0;
const SprintActive = 1;
const SprintCompleted = 2;

interface IssueGroup {
  /** null = "No epic / orphan items". */
  readonly epic: IssueSummary | null;
  readonly items: readonly IssueSummary[];
}

@Component({
  selector: 'app-backlog-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    CdkDropListGroup,
    CdkDropList,
    CdkDrag,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TooltipModule,
    AppPageHeaderComponent,
    CreateIssueDialogComponent,
    IssueStatusBadgeComponent,
    IssuePriorityIconComponent,
    IssueTypePillComponent,
    UserAvatarComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="'nav.backlog' | translate">
        <button pButton (click)="dialogVisible.set(true)" [label]="'issue.create' | translate"></button>
        <button pButton (click)="openCreateSprint()" [label]="'sprint.create' | translate" class="p-button-secondary"></button>
      </app-page-header>

      <div class="toolbar">
        <label class="field">
          <span>{{ 'sprint.select' | translate }}</span>
          <p-select
            [options]="sprintOptions()"
            optionLabel="label"
            optionValue="value"
            [(ngModel)]="selectedSprintIdModel"
            (ngModelChange)="onSprintSelect($event)"
            [placeholder]="'sprint.none' | translate"
            appendTo="body"
            styleClass="sprint-select" />
        </label>
        @if (selectedSprint(); as sp) {
          @if (sp.status === SprintPlanned) {
            <button pButton (click)="startSprint(sp)" [label]="'sprint.start_button' | translate"></button>
          }
          @if (sp.status === SprintActive) {
            <button pButton (click)="completeSprint(sp)" [label]="'sprint.complete' | translate" class="p-button-secondary"></button>
          }
        }
        <div class="grow"></div>
        <label class="field toggle-field">
          <span>{{ 'backlog.group_by' | translate }}</span>
          <button
            type="button"
            class="toggle-btn"
            [class.active]="groupByEpic()"
            (click)="groupByEpic.set(!groupByEpic())"
            [attr.aria-pressed]="groupByEpic()">
            <i class="pi" [class.pi-sitemap]="groupByEpic()" [class.pi-list]="!groupByEpic()"></i>
            {{ (groupByEpic() ? 'backlog.group_epic' : 'backlog.group_flat') | translate }}
          </button>
        </label>
      </div>

      <div class="split" cdkDropListGroup>
        <section class="panel">
          <header class="panel-head">
            <h2 class="panel-title">{{ 'backlog.product_backlog' | translate }}</h2>
            <span class="panel-stats">
              {{ 'backlog.count_issues' | translate: { count: backlogItems().length } }}
              @if (backlogPoints() > 0) { · {{ backlogPoints() }} SP }
            </span>
          </header>
          <div
            id="product-backlog-list"
            class="drop-list"
            cdkDropList
            cdkDropListConnectedTo="sprint-issues-list"
            [cdkDropListData]="backlogItems()"
            (cdkDropListDropped)="onDrop($event)">
            @for (g of backlogGroups(); track g.epic?.id ?? '__none__') {
              @if (groupByEpic() && (g.epic || g.items.length > 0)) {
                <div class="group-header" [attr.data-empty]="g.items.length === 0 ? '1' : null">
                  @if (g.epic; as ep) {
                    <app-issue-type-pill [typeId]="ep.issueTypeId" />
                    <a class="group-key" [routerLink]="['/issues', ep.key]"><code>{{ ep.key }}</code></a>
                    <span class="group-summary">{{ ep.summary }}</span>
                    <app-issue-status-badge [statusId]="ep.currentStatusId" />
                  } @else {
                    <span class="group-summary muted">{{ 'backlog.group_no_epic' | translate }}</span>
                  }
                  <span class="group-count">{{ g.items.length }}</span>
                </div>
              }
              @for (issue of g.items; track issue.id) {
                <div class="issue-row" cdkDrag [cdkDragData]="issue">
                  <span class="drag-handle" cdkDragHandle><i class="pi pi-bars"></i></span>
                  <app-issue-type-pill [typeId]="issue.issueTypeId" />
                  <a class="row-key" [routerLink]="['/issues', issue.key]"><code>{{ issue.key }}</code></a>
                  <span class="row-summary" [title]="issue.summary">{{ issue.summary }}</span>
                  @if (issue.labels && issue.labels.length > 0) {
                    <span class="row-labels">
                      @for (l of issue.labels.slice(0, 2); track l) {
                        <span class="label-chip">{{ l }}</span>
                      }
                      @if (issue.labels.length > 2) {
                        <span class="label-chip more">+{{ issue.labels.length - 2 }}</span>
                      }
                    </span>
                  }
                  @if (issue.storyPoints != null) {
                    <span class="sp-pill" [pTooltip]="'backlog.story_points' | translate">{{ issue.storyPoints }}</span>
                  }
                  <app-issue-status-badge [statusId]="issue.currentStatusId" />
                  <app-issue-priority-icon [priority]="issue.priority" />
                  <app-user-avatar [userId]="issue.assigneeId" [emptyTooltip]="unassignedTooltip()" />
                </div>
              }
            }
            @if (backlogItems().length === 0) {
              <p class="empty">{{ 'backlog.empty_backlog' | translate }}</p>
            }
          </div>
        </section>

        <section class="panel">
          <header class="panel-head">
            <h2 class="panel-title">{{ 'backlog.sprint_issues' | translate }}</h2>
            @if (selectedSprint()) {
              <span class="panel-stats">
                {{ 'backlog.count_issues' | translate: { count: sprintItems().length } }}
                @if (sprintPoints() > 0) { · {{ sprintPoints() }} SP }
              </span>
            }
          </header>
          @if (!selectedSprint()) {
            <p class="hint">{{ 'backlog.pick_sprint' | translate }}</p>
          } @else {
            <div
              id="sprint-issues-list"
              class="drop-list"
              cdkDropList
              cdkDropListConnectedTo="product-backlog-list"
              [cdkDropListData]="sprintItems()"
              (cdkDropListDropped)="onDrop($event)">
              @for (g of sprintGroups(); track g.epic?.id ?? '__none__') {
                @if (groupByEpic() && (g.epic || g.items.length > 0)) {
                  <div class="group-header" [attr.data-empty]="g.items.length === 0 ? '1' : null">
                    @if (g.epic; as ep) {
                      <app-issue-type-pill [typeId]="ep.issueTypeId" />
                      <a class="group-key" [routerLink]="['/issues', ep.key]"><code>{{ ep.key }}</code></a>
                      <span class="group-summary">{{ ep.summary }}</span>
                      <app-issue-status-badge [statusId]="ep.currentStatusId" />
                    } @else {
                      <span class="group-summary muted">{{ 'backlog.group_no_epic' | translate }}</span>
                    }
                    <span class="group-count">{{ g.items.length }}</span>
                  </div>
                }
                @for (issue of g.items; track issue.id) {
                  <div class="issue-row" cdkDrag [cdkDragData]="issue">
                    <span class="drag-handle" cdkDragHandle><i class="pi pi-bars"></i></span>
                    <app-issue-type-pill [typeId]="issue.issueTypeId" />
                    <a class="row-key" [routerLink]="['/issues', issue.key]"><code>{{ issue.key }}</code></a>
                    <span class="row-summary" [title]="issue.summary">{{ issue.summary }}</span>
                    @if (issue.labels && issue.labels.length > 0) {
                      <span class="row-labels">
                        @for (l of issue.labels.slice(0, 2); track l) {
                          <span class="label-chip">{{ l }}</span>
                        }
                        @if (issue.labels.length > 2) {
                          <span class="label-chip more">+{{ issue.labels.length - 2 }}</span>
                        }
                      </span>
                    }
                    @if (issue.storyPoints != null) {
                      <span class="sp-pill" [pTooltip]="'backlog.story_points' | translate">{{ issue.storyPoints }}</span>
                    }
                    <app-issue-status-badge [statusId]="issue.currentStatusId" />
                    <app-issue-priority-icon [priority]="issue.priority" />
                    <app-user-avatar [userId]="issue.assigneeId" [emptyTooltip]="unassignedTooltip()" />
                  </div>
                }
              }
              @if (sprintItems().length === 0) {
                <p class="empty">{{ 'backlog.empty_sprint' | translate }}</p>
              }
            </div>
          }
        </section>
      </div>

      <app-create-issue-dialog
        [fixedProjectId]="p.id"
        [(visible)]="dialogVisible"
        (created)="onIssueCreated()" />

      <p-dialog [(visible)]="createSprintVisible" [header]="'sprint.create' | translate" [modal]="true" [style]="{ width: '400px' }">
        <div class="form-grid">
          <label>{{ 'sprint.name' | translate }}</label>
          <input pInputText [(ngModel)]="newSprintName" />
          <label>{{ 'sprint.field_start' | translate }}</label>
          <input type="date" [(ngModel)]="newSprintStart" />
          <label>{{ 'sprint.field_end' | translate }}</label>
          <input type="date" [(ngModel)]="newSprintEnd" />
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="createSprintVisible.set(false)" [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitCreateSprint()" [label]="'common.save' | translate"></button>
        </div>
      </p-dialog>
    } @else {
      <div class="page-loading">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    /* ──────── Toolbar ──────── */
    .toolbar { display: flex; flex-wrap: wrap; gap: 12px; align-items: flex-end; margin-bottom: 16px; }
    .grow { flex: 1; }
    .field { display: flex; flex-direction: column; gap: 4px; font-size: 11px; color: var(--c-text-muted); text-transform: uppercase; letter-spacing: 0.4px; }
    .toggle-field { align-items: stretch; }
    .toggle-btn {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 6px 12px; border-radius: var(--radius);
      border: 1px solid var(--c-border); background: var(--c-surface); color: var(--c-text);
      font-size: 12px; font-weight: 500; cursor: pointer; transition: background 0.1s;
    }
    .toggle-btn:hover { background: var(--c-surface-2); }
    .toggle-btn.active { background: var(--c-text); color: var(--c-on-primary); border-color: var(--c-text); }
    ::ng-deep .sprint-select { min-width: 240px; }

    /* ──────── Layout ──────── */
    .split { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; align-items: start; }
    @media (max-width: 1100px) { .split { grid-template-columns: 1fr; } }
    .panel {
      background: var(--c-surface-2);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 12px;
      min-height: 320px;
    }
    .panel-head {
      display: flex; align-items: baseline; justify-content: space-between;
      gap: 8px; margin: 0 4px 12px;
    }
    .panel-title { font-size: 13px; font-weight: 600; margin: 0; color: var(--c-text); text-transform: uppercase; letter-spacing: 0.5px; }
    .panel-stats { font-size: 11px; color: var(--c-text-muted); }
    .drop-list { min-height: 120px; display: flex; flex-direction: column; gap: 2px; }

    /* ──────── Group header (Epic) ──────── */
    .group-header {
      display: flex; align-items: center; gap: 8px;
      padding: 8px 10px;
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      margin-top: 10px;
      font-size: 12px;
    }
    .group-header[data-empty="1"] { opacity: 0.6; }
    .group-header:first-child { margin-top: 0; }
    .group-key code { font-family: monospace; font-size: 11px; color: var(--c-text-muted); }
    .group-key { text-decoration: none; }
    .group-key:hover code { color: var(--c-text); text-decoration: underline; }
    .group-summary { font-weight: 600; color: var(--c-text); flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .group-summary.muted { font-weight: 500; color: var(--c-text-muted); font-style: italic; }
    .group-count {
      background: var(--c-surface-3); color: var(--c-text-muted);
      padding: 2px 8px; border-radius: 10px;
      font-size: 10px; font-weight: 600;
    }

    /* ──────── Issue row (Jira-like compact) ──────── */
    .issue-row {
      display: flex; align-items: center; gap: 10px;
      padding: 6px 10px;
      background: var(--c-surface);
      border: 1px solid transparent;
      border-radius: var(--radius);
      min-height: 36px;
      cursor: default;
      transition: background 0.08s, border-color 0.08s;
    }
    .issue-row:hover { background: var(--c-surface-3); border-color: var(--c-border); }
    .drag-handle {
      display: inline-flex; align-items: center; justify-content: center;
      width: 16px; height: 16px; color: var(--c-text-subtle); cursor: grab;
      opacity: 0; transition: opacity 0.1s;
    }
    .issue-row:hover .drag-handle { opacity: 1; }
    .drag-handle:active { cursor: grabbing; }
    .drag-handle .pi { font-size: 12px; }

    .row-key { text-decoration: none; flex: 0 0 auto; }
    .row-key code {
      font-family: monospace; font-size: 12px; color: var(--c-text-muted);
    }
    .row-key:hover code { color: var(--c-text); text-decoration: underline; }

    .row-summary {
      flex: 1; min-width: 0; font-size: 13px; color: var(--c-text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }

    .row-labels { display: inline-flex; gap: 4px; flex: 0 0 auto; }
    .label-chip {
      background: var(--c-surface-3); color: var(--c-text-muted);
      padding: 1px 6px; border-radius: 8px;
      font-size: 10px; font-weight: 500;
      max-width: 80px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .label-chip.more { background: transparent; color: var(--c-text-subtle); }

    .sp-pill {
      display: inline-flex; align-items: center; justify-content: center;
      flex: 0 0 22px; min-width: 22px; height: 22px;
      padding: 0 6px;
      border-radius: 11px; background: var(--c-surface-3); color: var(--c-text);
      font-size: 11px; font-weight: 600;
    }

    /* ──────── Misc ──────── */
    .empty, .hint { color: var(--c-text-muted); font-size: 13px; padding: 24px; text-align: center; }
    .page-loading { padding: 40px; text-align: center; color: var(--c-text-muted); }
    .form-grid { display: grid; grid-template-columns: 100px 1fr; gap: 10px; align-items: center; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }

    /* CDK drag visuals */
    .cdk-drag-preview {
      box-shadow: var(--shadow-md);
      border-radius: var(--radius);
      background: var(--c-surface);
    }
    .cdk-drag-placeholder { opacity: 0.3; }
    .cdk-drag-animating { transition: transform 200ms cubic-bezier(0, 0, 0.2, 1); }
    .drop-list.cdk-drop-list-dragging .issue-row:not(.cdk-drag-placeholder) {
      transition: transform 200ms cubic-bezier(0, 0, 0.2, 1);
    }
  `]
})
export class BacklogPageComponent implements OnInit, OnDestroy {
  readonly SprintPlanned = SprintPlanned;
  readonly SprintActive = SprintActive;
  readonly SprintCompleted = SprintCompleted;

  private readonly route = inject(ActivatedRoute);
  private readonly projApi = inject(ProjectApiService);
  private readonly issueApi = inject(IssueApiService);
  private readonly sprintApi = inject(SprintApiService);
  private readonly ctx = inject(WorkspaceContextService);
  private readonly statusCache = inject(StatusCacheService);
  private readonly typeCache = inject(IssueTypeCacheService);
  private readonly userCache = inject(UserCacheService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly project = signal<ProjectDetail | null>(null);
  readonly sprints = signal<SprintDto[]>([]);
  readonly selectedSprintId = signal<string | null>(null);
  readonly backlogItems = signal<IssueSummary[]>([]);
  readonly sprintItems = signal<IssueSummary[]>([]);
  readonly loading = signal(false);
  readonly dialogVisible = signal(false);
  readonly createSprintVisible = model(false);
  readonly groupByEpic = signal(true);

  /** Bound to p-select (template-driven). */
  selectedSprintIdModel: string | null = null;

  newSprintName = '';
  newSprintStart = '';
  newSprintEnd = '';

  // ─── Derived: sums + grouped lists ───────────────────────────────────────
  readonly backlogPoints = computed(() => sumPoints(this.backlogItems()));
  readonly sprintPoints = computed(() => sumPoints(this.sprintItems()));
  readonly backlogGroups = computed<readonly IssueGroup[]>(() => buildGroups(this.backlogItems()));
  readonly sprintGroups = computed<readonly IssueGroup[]>(() => buildGroups(this.sprintItems()));

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    void this.bootstrap(key);
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }

  readonly sprintOptions = (): { label: string; value: string }[] =>
    this.sprints().map((s) => ({
      label: `${s.name} (${this.statusLabel(s.status)})`,
      value: s.id
    }));

  selectedSprint(): SprintDto | null {
    const id = this.selectedSprintId();
    if (!id) return null;
    return this.sprints().find((x) => x.id === id) ?? null;
  }

  openCreateSprint(): void {
    this.newSprintName = '';
    const t = new Date();
    const end = new Date(t);
    end.setDate(end.getDate() + 14);
    this.newSprintStart = t.toISOString().slice(0, 10);
    this.newSprintEnd = end.toISOString().slice(0, 10);
    this.createSprintVisible.set(true);
  }

  async submitCreateSprint(): Promise<void> {
    const p = this.project();
    if (!p || !this.newSprintName.trim() || !this.newSprintStart || !this.newSprintEnd) return;
    const body: CreateSprintRequest = {
      name: this.newSprintName.trim(),
      startDate: new Date(this.newSprintStart + 'T12:00:00Z').toISOString(),
      endDate: new Date(this.newSprintEnd + 'T12:00:00Z').toISOString(),
      goal: null
    };
    const created = await firstValueFrom(this.sprintApi.create(p.id, body));
    this.createSprintVisible.set(false);
    await this.reloadSprints();
    this.selectedSprintId.set(created.id);
    this.selectedSprintIdModel = created.id;
    await this.reloadAll();
  }

  onSprintSelect(id: string | null): void {
    this.selectedSprintId.set(id);
    void this.reloadSprintColumn();
  }

  async startSprint(sp: SprintDto): Promise<void> {
    const p = this.project();
    if (!p) return;
    await firstValueFrom(this.sprintApi.start(p.id, sp.id));
    await this.reloadSprints();
    await this.reloadAll();
  }

  async completeSprint(sp: SprintDto): Promise<void> {
    const p = this.project();
    if (!p) return;
    await firstValueFrom(this.sprintApi.complete(p.id, sp.id));
    await this.reloadSprints();
    await this.reloadAll();
  }

  onIssueCreated(): void {
    void this.reloadAll();
  }

  async onDrop(ev: CdkDragDrop<IssueSummary[]>): Promise<void> {
    const p = this.project();
    const sp = this.selectedSprint();
    if (!p || !sp) return;

    const issue = ev.item.data as IssueSummary;
    if (!issue) return;

    if (ev.previousContainer === ev.container) {
      if (sp.status === SprintCompleted) return;
      if (ev.container.id !== 'sprint-issues-list') return; // chỉ reorder trong sprint
      const list = [...this.sprintItems()];
      const prevIdx = ev.previousIndex;
      const newIdx = ev.currentIndex;
      if (prevIdx === newIdx) return;
      const moved = list.splice(prevIdx, 1)[0];
      list.splice(newIdx, 0, moved);
      this.sprintItems.set(list);
      await firstValueFrom(
        this.sprintApi.reorderIssues(p.id, sp.id, { issueIds: list.map((i) => i.id) })
      );
      await this.reloadSprints();
      return;
    }

    const toSprint = ev.container.id === 'sprint-issues-list';
    if (toSprint) {
      if (sp.status === SprintCompleted) return;
      await firstValueFrom(this.sprintApi.addIssue(p.id, sp.id, issue.id));
    } else {
      await firstValueFrom(this.sprintApi.removeIssue(p.id, sp.id, issue.id));
    }
    await this.reloadSprints();
    await this.reloadAll();
  }

  /** Tooltip cho avatar khi assignee = null. Đọc từ TranslateService (sync). */
  unassignedTooltip(): string {
    return this.translate.instant('issue.unassigned');
  }

  // ─── Bootstrap & reload ───────────────────────────────────────────────────
  private async bootstrap(projectKey: string): Promise<void> {
    this.loading.set(true);
    try {
      const detail = await firstValueFrom(this.projApi.getDetailForMemberByKey(projectKey));
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));
      this.typeCache.putMany(detail.issueTypes);
      // Persist last-visited project for `/backlog` shortcut redirect.
      try { localStorage.setItem('jira-clone:last-project-key', detail.key); } catch { /* ignore quota */ }
      await this.reloadSprints();
      await this.reloadAll();
    } finally {
      this.loading.set(false);
      this.cdr.markForCheck();
    }
  }

  private async reloadSprints(): Promise<void> {
    const p = this.project();
    if (!p) return;
    const list = await firstValueFrom(this.sprintApi.list(p.id));
    this.sprints.set(list);

    let sel = this.selectedSprintId();
    if (!sel || !list.some((x) => x.id === sel)) {
      const active = list.find((x) => x.status === SprintActive);
      const planned = list.find((x) => x.status === SprintPlanned);
      sel = active?.id ?? planned?.id ?? null;
      this.selectedSprintId.set(sel);
      this.selectedSprintIdModel = sel;
    }

    await this.reloadSprintColumn();
    this.cdr.markForCheck();
  }

  private async reloadAll(): Promise<void> {
    await this.reloadBacklog();
    await this.reloadSprintColumn();
    await this.warmAuxCaches();
    this.cdr.markForCheck();
  }

  private async reloadBacklog(): Promise<void> {
    const p = this.project();
    if (!p) return;
    const exclude = this.collectOpenSprintIssueIds();
    const page = await firstValueFrom(
      this.issueApi.search({
        projectId: p.id,
        pageIndex: 1,
        pageSize: 200,
        sort: 'key',
        includeArchived: false,
        excludeIssueIds: exclude.length ? exclude : null
      })
    );
    this.backlogItems.set(page.items);
  }

  private collectOpenSprintIssueIds(): string[] {
    const set = new Set<string>();
    for (const s of this.sprints()) {
      if (s.status === SprintPlanned || s.status === SprintActive) {
        for (const id of s.orderedIssueIds) set.add(id);
      }
    }
    return [...set];
  }

  private async reloadSprintColumn(): Promise<void> {
    const p = this.project();
    const sp = this.selectedSprint();
    if (!p || !sp || sp.orderedIssueIds.length === 0) {
      this.sprintItems.set([]);
      return;
    }
    const page = await firstValueFrom(
      this.issueApi.search({
        projectId: p.id,
        issueIds: [...sp.orderedIssueIds],
        pageIndex: 1,
        pageSize: 200,
        sort: 'key',
        includeArchived: false
      })
    );
    const map = new Map(page.items.map((i) => [i.id, i]));
    const ordered: IssueSummary[] = [];
    for (const id of sp.orderedIssueIds) {
      const row = map.get(id);
      if (row) ordered.push(row);
    }
    this.sprintItems.set(ordered);
  }

  /** Pre-fetch user names + statuses cho cả 2 list. */
  private async warmAuxCaches(): Promise<void> {
    const p = this.project();
    if (!p) return;
    await this.statusCache.ensureProjectLoaded(p.id);
    const all = [...this.backlogItems(), ...this.sprintItems()];
    const userIds = new Set<string>();
    for (const i of all) {
      if (i.assigneeId) userIds.add(i.assigneeId);
    }
    await this.userCache.ensureLoaded([...userIds]);
    this.cdr.markForCheck();
  }

  private statusLabel(st: number): string {
    if (st === SprintActive) return 'Active';
    if (st === SprintCompleted) return 'Done';
    return 'Planned';
  }
}

// ─── Pure helpers ──────────────────────────────────────────────────────────────

function sumPoints(items: readonly IssueSummary[]): number {
  let n = 0;
  for (const i of items) {
    if (typeof i.storyPoints === 'number') n += i.storyPoints;
  }
  return Math.round(n * 10) / 10;
}

/**
 * Group issues by parentIssueId. Items whose parent is also in the list trở thành
 * children của epic group; items có parent ngoài list hoặc không có parent → group "no epic".
 *
 * Epic-level items (xuất hiện như parent của ai đó) được TÁCH ra khỏi danh sách item
 * thường — chúng chỉ render dưới dạng group header. Như vậy không bị duplicate.
 */
function buildGroups(items: readonly IssueSummary[]): readonly IssueGroup[] {
  const byId = new Map<string, IssueSummary>();
  for (const i of items) byId.set(i.id, i);

  const childrenByParent = new Map<string, IssueSummary[]>();
  const orphans: IssueSummary[] = [];
  const usedAsParent = new Set<string>();

  for (const i of items) {
    if (i.parentIssueId && byId.has(i.parentIssueId)) {
      usedAsParent.add(i.parentIssueId);
      const arr = childrenByParent.get(i.parentIssueId) ?? [];
      arr.push(i);
      childrenByParent.set(i.parentIssueId, arr);
    } else {
      orphans.push(i);
    }
  }

  // Items used as parent (Epics) đều render-as-header; loại khỏi orphans nếu có.
  const filteredOrphans = orphans.filter((i) => !usedAsParent.has(i.id));

  const groups: IssueGroup[] = [];
  // Epics có children — sort theo key
  const epicIds = [...childrenByParent.keys()].sort((a, b) => {
    const ka = byId.get(a)?.key ?? '';
    const kb = byId.get(b)?.key ?? '';
    return ka.localeCompare(kb);
  });
  for (const eid of epicIds) {
    groups.push({ epic: byId.get(eid) ?? null, items: childrenByParent.get(eid) ?? [] });
  }
  // Group cuối: orphans không thuộc epic nào
  if (filteredOrphans.length > 0) {
    groups.push({ epic: null, items: filteredOrphans });
  }
  return groups;
}
