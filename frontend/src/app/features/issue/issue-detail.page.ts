import { ChangeDetectionStrategy, Component, OnInit, computed, inject, model, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonGroupModule } from 'primeng/buttongroup';
import { Issue, IssueApiService } from '@core/api/issue.service';
import { AvailableTransition, WorkflowApiService } from '@core/api/workflow.service';
import { AuthService } from '@core/auth/auth.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { ActivityTimelineComponent } from './activity-timeline.component';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { CommentsThreadComponent } from './comments-thread.component';
import { AttachmentPanelComponent } from './attachment-panel.component';

@Component({
  selector: 'app-issue-detail-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, ButtonGroupModule, InputTextModule,
    CommentsThreadComponent,
    AttachmentPanelComponent,
    ActivityTimelineComponent,
    UserPickerComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (issue(); as i) {
      <div class="head">
        <div class="key">{{ i.key }}</div>
        <h1>{{ i.summary }}</h1>
      </div>

      <div class="layout">
        <main class="main">
          <section>
            <h3>{{ 'issue.description' | translate }}</h3>
            <div class="desc">{{ i.description || '—' }}</div>
          </section>

          @if (transitions().length > 0) {
            <section>
              <h3>{{ 'issue.transition' | translate }}</h3>
              <div class="transitions">
                @for (t of transitions(); track t.id) {
                  <button pButton size="small" (click)="doTransition(t)"
                          [loading]="transitioning() === t.id"
                          [label]="t.name + ' → ' + t.toStatusName"></button>
                }
              </div>
            </section>
          }

          <app-comments-thread [issueId]="i.id" />
          <app-attachment-panel [issueId]="i.id" />
          <app-activity-timeline [issueId]="i.id" />
        </main>

        <aside class="side">
          <div class="kv"><span>{{ 'issue.status' | translate }}</span><strong>{{ statusName(i.currentStatusId) }}</strong></div>
          <div class="kv"><span>{{ 'issue.priority' | translate }}</span>P{{ i.priority }}</div>
          <div class="kv assignee-row">
            <span>{{ 'issue.assignee' | translate }}</span>
            <div class="assignee-edit">
              <app-user-picker [(userId)]="assigneeDraft" />
              <button pButton type="button" size="small" class="assignee-save"
                      [loading]="savingAssignee()"
                      [disabled]="!assigneeDirty() || savingAssignee()"
                      (click)="saveAssignee()"
                      [label]="'issue.assignee_save' | translate"></button>
            </div>
          </div>
          <div class="kv"><span>{{ 'issue.reporter' | translate }}</span>{{ i.reporterId.slice(0, 8) }}…</div>
          @if (i.dueDate) { <div class="kv"><span>{{ 'issue.due_date' | translate }}</span>{{ i.dueDate | date:'short' }}</div> }
          @if (i.storyPoints !== null && i.storyPoints !== undefined) {
            <div class="kv"><span>{{ 'issue.story_points' | translate }}</span>{{ i.storyPoints }}</div>
          }
          @if (i.labels && i.labels.length > 0) {
            <div class="kv">
              <span>{{ 'issue.labels' | translate }}</span>
              <div class="labels">
                @for (l of i.labels; track l) { <span class="lbl">{{ l }}</span> }
              </div>
            </div>
          }
          <div class="kv"><span>{{ 'issue.watchers' | translate }}</span>{{ i.watchers.length }}</div>
        </aside>
      </div>
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .head { margin-bottom: 24px; }
    .key { font-family: monospace; font-size: 13px; color: var(--c-text-muted); }
    h1 { margin: 4px 0 0; font-size: 22px; font-weight: 600; line-height: 1.3; }
    .layout { display: grid; grid-template-columns: 1fr 280px; gap: 24px; }
    @media (max-width: 768px) { .layout { grid-template-columns: 1fr; } }
    .main section { margin-bottom: 24px; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px;
    }
    .desc {
      padding: 12px; background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); white-space: pre-wrap; min-height: 60px;
      color: var(--c-text);
    }
    .transitions { display: flex; flex-wrap: wrap; gap: 8px; }
    .side {
      padding: 16px; background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); display: flex; flex-direction: column; gap: 10px;
      align-self: start; position: sticky; top: 64px;
    }
    .kv { display: flex; gap: 8px; font-size: 13px; align-items: baseline; }
    .kv > span:first-child {
      flex: 0 0 100px; font-size: 11px; text-transform: uppercase;
      color: var(--c-text-muted); letter-spacing: 0.5px;
    }
    .kv.assignee-row { flex-direction: column; align-items: stretch; gap: 8px; }
    .kv.assignee-row > span:first-child { flex: none; }
    .assignee-edit { display: flex; flex-direction: column; gap: 8px; width: 100%; }
    .labels { display: flex; flex-wrap: wrap; gap: 4px; }
    .lbl {
      font-size: 11px; padding: 1px 6px; border-radius: 3px;
      background: var(--c-surface-3); color: var(--c-text);
    }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class IssueDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(IssueApiService);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly auth = inject(AuthService);
  private readonly statusCache = inject(StatusCacheService);

  readonly issue = signal<Issue | null>(null);
  readonly transitions = signal<AvailableTransition[]>([]);
  readonly transitioning = signal<string | null>(null);
  readonly assigneeDraft = model<string | null>(null);
  readonly savingAssignee = signal(false);

  readonly assigneeDirty = computed(() => {
    const i = this.issue();
    if (!i) return false;
    return (i.assigneeId ?? null) !== (this.assigneeDraft() ?? null);
  });

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('issueKey');
    if (!key) return;
    this.load(key);
  }

  statusName(statusId: string): string {
    return this.statusCache.nameOf(statusId) ?? statusId.slice(0, 8) + '…';
  }

  private load(issueKey: string): void {
    this.api.getByKey(issueKey).subscribe((i) => {
      this.issue.set(i);
      this.assigneeDraft.set(i.assigneeId ?? null);
      this.statusCache.ensureProjectLoaded(i.projectId);
      this.loadTransitions(i);
    });
  }

  saveAssignee(): void {
    const i = this.issue();
    if (!i || !this.assigneeDirty()) return;
    this.savingAssignee.set(true);
    this.api.update(i.id, {
      summary: i.summary,
      description: i.description ?? null,
      priority: i.priority,
      assigneeId: this.assigneeDraft(),
      parentIssueId: i.parentIssueId ?? null,
      dueDate: i.dueDate ?? null,
      storyPoints: i.storyPoints ?? null,
      labels: i.labels ?? null,
      originalEstimateMinutes: i.originalEstimateMinutes ?? null,
      remainingEstimateMinutes: i.remainingEstimateMinutes ?? null,
      timeSpentMinutes: i.timeSpentMinutes ?? null
    }).subscribe({
      next: (updated) => {
        this.issue.set(updated);
        this.assigneeDraft.set(updated.assigneeId ?? null);
        this.savingAssignee.set(false);
      },
      error: () => this.savingAssignee.set(false)
    });
  }

  private loadTransitions(i: Issue): void {
    const userId = this.auth.user()?.id;
    if (!userId) return;
    this.wfApi.getAvailableTransitions(i.projectId, i.issueTypeId, i.currentStatusId, userId)
      .subscribe((list) => this.transitions.set(list));
  }

  doTransition(t: AvailableTransition): void {
    const i = this.issue();
    if (!i) return;
    this.transitioning.set(t.id);
    this.api.transition(i.id, { transitionId: t.id, inputs: null, comment: null }).subscribe({
      next: (updated) => {
        this.issue.set(updated);
        this.transitioning.set(null);
        this.loadTransitions(updated);
      },
      error: () => this.transitioning.set(null)
    });
  }
}
