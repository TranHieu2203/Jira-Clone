import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, SecurityContext, computed, inject, model, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonGroupModule } from 'primeng/buttongroup';
import { Issue, IssueApiService, UpdateIssueRequest } from '@core/api/issue.service';
import { IssueThreadRealtimePayload, WorkspaceHubService } from '@core/realtime/workspace-hub.service';
import { AvailableTransition, WorkflowApiService } from '@core/api/workflow.service';
import { AuthService } from '@core/auth/auth.service';
import { StatusCacheService } from '@core/api/status-cache.service';
import { IssueTypeCacheService } from '@core/api/issue-type-cache.service';
import { UserCacheService } from '@core/api/user-cache.service';
import { ActivityTimelineComponent } from './activity-timeline.component';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { CommentsThreadComponent } from './comments-thread.component';
import { AttachmentPanelComponent } from './attachment-panel.component';
import { LinkedIssuesPanelComponent } from './linked-issues-panel.component';
import { IssueCustomFieldsFormComponent } from './issue-custom-fields-form.component';
import { RichTextEditorComponent } from '@shared/ui/rich-text-editor.component';
import { IssueStatusBadgeComponent } from '@shared/ui/issue-status-badge.component';
import { IssuePriorityIconComponent } from '@shared/ui/issue-priority-icon.component';
import { IssueTypePillComponent } from '@shared/ui/issue-type-pill.component';
import { UserAvatarComponent } from '@shared/ui/user-avatar.component';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-issue-detail-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, ButtonGroupModule, InputTextModule, RichTextEditorComponent,
    CommentsThreadComponent,
    AttachmentPanelComponent,
    LinkedIssuesPanelComponent,
    ActivityTimelineComponent,
    UserPickerComponent,
    IssueCustomFieldsFormComponent,
    IssueStatusBadgeComponent,
    IssuePriorityIconComponent,
    IssueTypePillComponent,
    UserAvatarComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (issue(); as i) {
      <div class="head">
        <div class="breadcrumb">
          <a routerLink="/projects" class="bc-link">Projects</a>
          <span class="bc-sep">/</span>
          <a [routerLink]="['/projects', projectKeyOf(i.key)]" class="bc-link"><code>{{ projectKeyOf(i.key) }}</code></a>
          <span class="bc-sep">/</span>
          <span class="bc-key"><code>{{ i.key }}</code></span>
        </div>
        <div class="title-row">
          <app-issue-type-pill [typeId]="i.issueTypeId" size="lg" />
          <a [routerLink]="['/issues', i.key]" class="title-key"><code>{{ i.key }}</code></a>
        </div>
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

          <app-linked-issues-panel [issueId]="i.id" />
          <app-comments-thread [issueId]="i.id" />
          <app-attachment-panel [issueId]="i.id" />
          <app-activity-timeline [issueId]="i.id" />
        </main>

        <aside class="side">
          <div class="side-section">
            <div class="kv">
              <span>{{ 'issue.status' | translate }}</span>
              <app-issue-status-badge [statusId]="i.currentStatusId" size="lg" />
            </div>
            <div class="kv">
              <span>{{ 'issue.priority' | translate }}</span>
              <span class="pri-with-label">
                <app-issue-priority-icon [priority]="i.priority" />
                <span class="pri-label">{{ priorityLabel(i.priority) }}</span>
              </span>
            </div>
          </div>
          <div class="side-section">
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
            <div class="kv">
              <span>{{ 'issue.reporter' | translate }}</span>
              <span class="reporter-cell">
                <app-user-avatar [userId]="i.reporterId" size="sm" />
                <span class="reporter-name">{{ reporterDisplayName(i.reporterId) }}</span>
              </span>
            </div>
          </div>
          <div class="side-section">
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
          </div>
        </aside>
      </div>
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .head { margin-bottom: 24px; }
    .breadcrumb { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--c-text-muted); margin-bottom: 8px; }
    .bc-link { color: var(--c-text-muted); text-decoration: none; }
    .bc-link:hover { color: var(--c-text); text-decoration: underline; }
    .bc-key code { color: var(--c-text); font-weight: 500; }
    .bc-link code, .bc-key code { font-family: monospace; font-size: 12px; }
    .bc-sep { color: var(--c-text-subtle); }
    .title-row { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }
    .title-key { text-decoration: none; }
    .title-key code { font-family: monospace; font-size: 14px; color: var(--c-text-muted); font-weight: 500; }
    .title-key:hover code { color: var(--c-text); }
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
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); display: flex; flex-direction: column;
      align-self: start; position: sticky; top: 64px;
      overflow: hidden;
    }
    .side-section {
      padding: 14px 16px;
      display: flex; flex-direction: column; gap: 12px;
      border-bottom: 1px solid var(--c-border);
    }
    .side-section:last-child { border-bottom: none; }
    .kv { display: flex; gap: 8px; font-size: 13px; align-items: center; }
    .kv > span:first-child {
      flex: 0 0 90px; font-size: 11px; text-transform: uppercase;
      color: var(--c-text-muted); letter-spacing: 0.5px; font-weight: 600;
    }
    .kv.assignee-row { flex-direction: column; align-items: stretch; gap: 8px; }
    .kv.assignee-row > span:first-child { flex: none; }
    .assignee-edit { display: flex; flex-direction: column; gap: 8px; width: 100%; }
    .pri-with-label { display: inline-flex; align-items: center; gap: 6px; }
    .pri-label { font-size: 12px; color: var(--c-text); }
    .reporter-cell { display: inline-flex; align-items: center; gap: 6px; }
    .reporter-name { font-size: 12px; color: var(--c-text); }
    .labels { display: flex; flex-wrap: wrap; gap: 4px; }
    .lbl {
      font-size: 11px; padding: 1px 6px; border-radius: 3px;
      background: var(--c-surface-3); color: var(--c-text);
    }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class IssueDetailPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(IssueApiService);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly auth = inject(AuthService);
  private readonly statusCache = inject(StatusCacheService);
  private readonly typeCache = inject(IssueTypeCacheService);
  private readonly userCache = inject(UserCacheService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly hub = inject(WorkspaceHubService);
  private readonly cdr = inject(ChangeDetectorRef);

  /**
   * F12: handler được lưu ref để hub.removeIssueListener khớp khi destroy.
   * Listen các action từ phía issue khác user/tab gây ra:
   * - "status", "assignee", "updated" → reload toàn bộ issue
   * - "comment*", "attachment", "link" → bỏ qua ở đây (panel con tự reload)
   */
  private readonly issueRealtimeHandler = (payload: IssueThreadRealtimePayload): void => {
    const i = this.issue();
    if (!i) return;
    if (payload.action === 'status' || payload.action === 'assignee' || payload.action === 'updated') {
      this.load(i.key);
    }
  };

  private joinedIssueId: string | null = null;

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
    // F12: subscribe realtime — handler chỉ phản ứng cho issue hiện tại (filter trong handler).
    this.hub.addIssueListener(this.issueRealtimeHandler);
  }

  ngOnDestroy(): void {
    this.hub.removeIssueListener(this.issueRealtimeHandler);
    if (this.joinedIssueId) {
      void this.hub.leaveIssue(this.joinedIssueId);
      this.joinedIssueId = null;
    }
  }

  /** Extract project key prefix from issue key. e.g. "DEMO-12" → "DEMO". */
  projectKeyOf(issueKey: string): string {
    const idx = issueKey.lastIndexOf('-');
    return idx > 0 ? issueKey.substring(0, idx) : issueKey;
  }

  reporterDisplayName(userId: string): string {
    return this.userCache.displayNameOf(userId) ?? userId.slice(0, 8) + '…';
  }

  priorityLabel(p: number): string {
    switch (p) {
      case 1: return 'Lowest';
      case 2: return 'Low';
      case 3: return 'Medium';
      case 4: return 'High';
      case 5: return 'Highest';
      default: return 'Medium';
    }
  }

  private load(issueKey: string): void {
    this.api.getByKey(issueKey).subscribe(async (i) => {
      this.issue.set(i);
      this.assigneeDraft.set(i.assigneeId ?? null);
      this.descriptionDraft = i.description ?? '';
      this.editingDescription.set(false);
      // Warm caches THEN trigger CD so type pill / status badge / reporter
      // hiển thị tên thật ngay khi caches resolve (thay vì "?" / GUID slice).
      const userIds: string[] = [i.reporterId];
      if (i.assigneeId) userIds.push(i.assigneeId);
      await Promise.all([
        this.statusCache.ensureProjectLoaded(i.projectId),
        this.typeCache.ensureProjectLoaded(i.projectId),
        this.userCache.ensureLoaded(userIds)
      ]);
      this.cdr.markForCheck();
      this.loadTransitions(i);
      // F12: join hub group cho issue này (re-join idempotent nếu đã join).
      if (this.joinedIssueId !== i.id) {
        if (this.joinedIssueId) void this.hub.leaveIssue(this.joinedIssueId);
        this.joinedIssueId = i.id;
        void this.hub.joinIssue(i.id);
      }
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
