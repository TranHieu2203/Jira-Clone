import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
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
import { firstValueFrom } from 'rxjs';

const SprintPlanned = 0;
const SprintActive = 1;
const SprintCompleted = 2;

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
    AppPageHeaderComponent,
    CreateIssueDialogComponent
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
      </div>

      <div class="split" cdkDropListGroup>
        <section class="panel">
          <h2 class="panel-title">{{ 'backlog.product_backlog' | translate }}</h2>
          <div
            id="product-backlog-list"
            class="drop-list"
            cdkDropList
            cdkDropListConnectedTo="sprint-issues-list"
            [cdkDropListData]="backlogItems()"
            (cdkDropListDropped)="onDrop($event)">
            @for (issue of backlogItems(); track issue.id) {
              <div class="issue-card" cdkDrag [cdkDragData]="issue">
                <div class="row">
                  <a [routerLink]="['/issues', issue.key]"><code>{{ issue.key }}</code></a>
                  <div class="meta">
                    <span class="status-pill" [attr.data-cat]="statusCat(issue.currentStatusId)">
                      {{ statusName(issue.currentStatusId) }}
                    </span>
                    <span class="pri pri-{{ issue.priority }}">P{{ issue.priority }}</span>
                  </div>
                </div>
                <div class="sum">{{ issue.summary }}</div>
              </div>
            }
            @if (backlogItems().length === 0) {
              <p class="empty">{{ 'backlog.empty_backlog' | translate }}</p>
            }
          </div>
        </section>

        <section class="panel">
          <h2 class="panel-title">{{ 'backlog.sprint_issues' | translate }}</h2>
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
              @for (issue of sprintItems(); track issue.id) {
                <div class="issue-card" cdkDrag [cdkDragData]="issue">
                  <div class="row">
                    <a [routerLink]="['/issues', issue.key]"><code>{{ issue.key }}</code></a>
                    <div class="meta">
                      <span class="status-pill" [attr.data-cat]="statusCat(issue.currentStatusId)">
                        {{ statusName(issue.currentStatusId) }}
                      </span>
                      <span class="pri pri-{{ issue.priority }}">P{{ issue.priority }}</span>
                    </div>
                  </div>
                  <div class="sum">{{ issue.summary }}</div>
                </div>
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
    .toolbar { display: flex; flex-wrap: wrap; gap: 12px; align-items: flex-end; margin-bottom: 16px; }
    .field { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--c-text-muted); }
    ::ng-deep .sprint-select { min-width: 240px; }
    .split {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      align-items: start;
    }
    @media (max-width: 900px) { .split { grid-template-columns: 1fr; } }
    .panel {
      background: var(--c-surface-2);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 12px;
      min-height: 320px;
    }
    .panel-title { font-size: 13px; font-weight: 600; margin: 0 0 12px; color: var(--c-text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
    .drop-list { min-height: 120px; display: flex; flex-direction: column; gap: 8px; }
    .issue-card {
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 10px;
      cursor: grab;
    }
    .issue-card:active { cursor: grabbing; }
    .row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
    .meta { display: inline-flex; align-items: center; gap: 6px; }
    code { font-size: 12px; }
    .sum { font-size: 13px; color: var(--c-text); line-height: 1.35; }
    .status-pill {
      display: inline-block; padding: 2px 8px; border-radius: 10px;
      font-size: 11px; font-weight: 600;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .status-pill[data-cat="1"] { background: var(--c-surface-3); color: var(--c-text-muted); }
    .status-pill[data-cat="2"] { background: #dbeafe; color: #1e40af; }
    .status-pill[data-cat="3"] { background: #d1fae5; color: #065f46; }
    .pri { font-size: 10px; font-weight: 700; padding: 2px 6px; border-radius: 3px; background: var(--c-surface-3); color: var(--c-text-muted); }
    .pri-4, .pri-5 { background: var(--c-accent-danger); color: white; }
    .empty, .hint { color: var(--c-text-muted); font-size: 13px; padding: 12px; text-align: center; }
    .page-loading { padding: 40px; text-align: center; color: var(--c-text-muted); }
    .form-grid { display: grid; grid-template-columns: 100px 1fr; gap: 10px; align-items: center; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
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
  private readonly cdr = inject(ChangeDetectorRef);

  readonly project = signal<ProjectDetail | null>(null);
  readonly sprints = signal<SprintDto[]>([]);
  readonly selectedSprintId = signal<string | null>(null);
  readonly backlogItems = signal<IssueSummary[]>([]);
  readonly sprintItems = signal<IssueSummary[]>([]);
  readonly loading = signal(false);
  readonly dialogVisible = signal(false);
  readonly createSprintVisible = model(false);

  /** Bound to p-select (template-driven). */
  selectedSprintIdModel: string | null = null;

  newSprintName = '';
  newSprintStart = '';
  newSprintEnd = '';

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

  private async bootstrap(projectKey: string): Promise<void> {
    this.loading.set(true);
    try {
      const detail = await firstValueFrom(this.projApi.getDetailForMemberByKey(projectKey));
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));
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
    await this.warmStatus(page.items);
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
    await this.warmStatus(page.items);
    const map = new Map(page.items.map((i) => [i.id, i]));
    const ordered: IssueSummary[] = [];
    for (const id of sp.orderedIssueIds) {
      const row = map.get(id);
      if (row) ordered.push(row);
    }
    this.sprintItems.set(ordered);
  }

  private async warmStatus(items: IssueSummary[]): Promise<void> {
    const p = this.project();
    if (!p) return;
    await this.statusCache.ensureProjectLoaded(p.id);
    this.cdr.markForCheck();
  }

  private statusLabel(st: number): string {
    if (st === SprintActive) return 'Active';
    if (st === SprintCompleted) return 'Done';
    return 'Planned';
  }

  statusName(statusId: string): string {
    return this.statusCache.nameOf(statusId) ?? statusId.slice(0, 8) + '…';
  }

  statusCat(statusId: string): number {
    return this.statusCache.categoryOf(statusId) ?? 1;
  }
}
