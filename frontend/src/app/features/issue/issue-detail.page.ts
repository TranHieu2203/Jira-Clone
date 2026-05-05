import { ChangeDetectionStrategy, Component, OnInit, SecurityContext, computed, inject, model, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonGroupModule } from 'primeng/buttongroup';
import { Issue, IssueApiService, UpdateIssueRequest } from '@core/api/issue.service';
import { AvailableTransition, WorkflowApiService } from '@core/api/workflow.service';
import { AuthService } from '@core/auth/auth.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { ActivityTimelineComponent } from './activity-timeline.component';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { CommentsThreadComponent } from './comments-thread.component';
import { AttachmentPanelComponent } from './attachment-panel.component';
import { IssueCustomFieldsFormComponent } from './issue-custom-fields-form.component';
import { RichTextEditorComponent } from '@shared/ui/rich-text-editor.component';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-issue-detail-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, ButtonGroupModule, InputTextModule, RichTextEditorComponent,
    CommentsThreadComponent,
    AttachmentPanelComponent,
    ActivityTimelineComponent,
    UserPickerComponent,
    IssueCustomFieldsFormComponent
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
          <section class="desc-section">
            <div class="desc-head">
              <h3>{{ 'issue.description' | translate }}</h3>
              @if (!editingDescription()) {
                <button type="button" class="link-edit" (click)="startEditDescription()">
                  {{ 'issue.desc_edit' | translate }}
                </button>
              }
            </div>
            @if (editingDescription()) {
              <app-rich-text-editor
                [(ngModel)]="descriptionDraft"
                name="issueDescription"
                [placeholderText]="'issue.description_editor_placeholder' | translate"
              />
              <div class="desc-actions">
                <button pButton type="button" size="small" [text]="true"
                        (click)="cancelEditDescription()"
                        [label]="'common.cancel' | translate"></button>
                <button pButton type="button" size="small"
                        [loading]="savingDescription()"
                        [disabled]="savingDescription()"
                        (click)="saveDescription()"
                        [label]="'common.save' | translate"></button>
              </div>
            } @else if (!i.description?.trim()) {
              <div class="desc">—</div>
            } @else if (isRichHtml(i.description!)) {
              <div class="desc html" [innerHTML]="safeDescription(i.description!)"></div>
            } @else {
              <div class="desc">{{ i.description }}</div>
            }
          </section>

          <app-issue-custom-fields-form
            [projectId]="i.projectId"
            [issueTypeId]="i.issueTypeId"
            [issueId]="i.id"
            [showSaveButton]="true"
          />

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
          <div class="kv">
            <span>{{ 'issue.status' | translate }}</span>
            <span class="status-pill" [attr.data-cat]="statusCat(i.currentStatusId)">
              {{ statusName(i.currentStatusId) }}
            </span>
          </div>
          <div class="kv">
            <span>{{ 'issue.priority' | translate }}</span>
            <span class="pri pri-{{ i.priority }}">P{{ i.priority }}</span>
          </div>
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
    .desc-head {
      display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 8px;
    }
    .desc-head h3 { margin: 0; }
    .link-edit {
      background: transparent; border: none; color: var(--c-text-muted);
      cursor: pointer; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px;
    }
    .link-edit:hover { color: var(--c-text); text-decoration: underline; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px;
    }
    .desc-section h3 { margin: 0; }
    .desc {
      padding: 12px; background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); white-space: pre-wrap; min-height: 60px;
      color: var(--c-text);
    }
    .desc.html { white-space: normal; }
    .desc.html ::ng-deep p { margin: 0 0 0.5em; }
    .desc.html ::ng-deep p:last-child { margin-bottom: 0; }
    .desc-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
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
  private readonly sanitizer = inject(DomSanitizer);

  readonly issue = signal<Issue | null>(null);
  readonly transitions = signal<AvailableTransition[]>([]);
  readonly transitioning = signal<string | null>(null);
  readonly assigneeDraft = model<string | null>(null);
  readonly savingAssignee = signal(false);
  readonly editingDescription = signal(false);
  readonly savingDescription = signal(false);
  descriptionDraft = '';

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

  statusCat(statusId: string): number {
    return this.statusCache.categoryOf(statusId) ?? 1;
  }

  private load(issueKey: string): void {
    this.api.getByKey(issueKey).subscribe((i) => {
      this.issue.set(i);
      this.assigneeDraft.set(i.assigneeId ?? null);
      this.descriptionDraft = i.description ?? '';
      this.editingDescription.set(false);
      this.statusCache.ensureProjectLoaded(i.projectId);
      this.loadTransitions(i);
    });
  }

  isRichHtml(s: string): boolean {
    const t = s?.trim() ?? '';
    return t.startsWith('<') && t.includes('>');
  }

  safeDescription(html: string): string {
    return this.sanitizer.sanitize(SecurityContext.HTML, html) ?? '';
  }

  startEditDescription(): void {
    const i = this.issue();
    if (!i) return;
    this.descriptionDraft = i.description ?? '';
    this.editingDescription.set(true);
  }

  cancelEditDescription(): void {
    const i = this.issue();
    this.descriptionDraft = i?.description ?? '';
    this.editingDescription.set(false);
  }

  saveDescription(): void {
    const i = this.issue();
    if (!i) return;
    this.savingDescription.set(true);
    this.api.update(i.id, {
      summary: i.summary,
      description: this.descriptionDraft.trim() ? this.descriptionDraft : null,
      priority: i.priority,
      assigneeId: i.assigneeId ?? null,
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
        this.descriptionDraft = updated.description ?? '';
        this.editingDescription.set(false);
        this.savingDescription.set(false);
      },
      error: () => this.savingDescription.set(false)
    });
  }

  saveAssignee(): void {
    const i = this.issue();
    if (!i || !this.assigneeDirty()) return;
    const patch = this.buildUpdateRequest(this.assigneeDraft());
    if (!patch) return;
    this.savingAssignee.set(true);
    this.api.update(i.id, patch).subscribe({
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

  /** Full PATCH body from current issue row (assignee from draft when caller passes it). */
  private buildUpdateRequest(assigneeId: string | null): UpdateIssueRequest | null {
    const i = this.issue();
    if (!i) return null;
    return {
      summary: i.summary,
      description: i.description ?? null,
      priority: i.priority,
      assigneeId,
      parentIssueId: i.parentIssueId ?? null,
      dueDate: i.dueDate ?? null,
      storyPoints: i.storyPoints ?? null,
      labels: i.labels ?? null,
      originalEstimateMinutes: i.originalEstimateMinutes ?? null,
      remainingEstimateMinutes: i.remainingEstimateMinutes ?? null,
      timeSpentMinutes: i.timeSpentMinutes ?? null
    };
  }

  doTransition(t: AvailableTransition): void {
    const i = this.issue();
    if (!i) return;
    const transitionBody = { transitionId: t.id, inputs: null, comment: null };
    const finish = (updated: Issue) => {
      this.issue.set(updated);
      this.assigneeDraft.set(updated.assigneeId ?? null);
      this.transitioning.set(null);
      this.loadTransitions(updated);
    };

    this.transitioning.set(t.id);

    if (this.assigneeDirty()) {
      const patch = this.buildUpdateRequest(this.assigneeDraft());
      if (!patch) {
        this.transitioning.set(null);
        return;
      }
      this.api
        .update(i.id, patch)
        .pipe(switchMap((updated) => this.api.transition(updated.id, transitionBody)))
        .subscribe({
          next: finish,
          error: () => this.transitioning.set(null)
        });
      return;
    }

    this.api.transition(i.id, transitionBody).subscribe({
      next: finish,
      error: () => this.transitioning.set(null)
    });
  }
}
