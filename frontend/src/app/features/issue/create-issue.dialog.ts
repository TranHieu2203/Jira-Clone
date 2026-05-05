import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ViewChild,
  effect,
  inject,
  input,
  model,
  output,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { Issue, IssueApiService, IssuePriority, IssueSummary } from '@core/api/issue.service';
import { IssueType, ProjectApiService, ProjectDetail, ProjectSummary } from '@core/api/project.service';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { RichTextEditorComponent } from '@shared/ui/rich-text-editor.component';
import { IssueCustomFieldsFormComponent } from './issue-custom-fields-form.component';

@Component({
  selector: 'app-create-issue-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslateModule,
    ButtonModule, DialogModule, InputTextModule, TextareaModule, SelectModule, AutoCompleteModule,
    UserPickerComponent, RichTextEditorComponent, IssueCustomFieldsFormComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog [visible]="visible()" (visibleChange)="visible.set($event)"
              [modal]="true" [style]="{ width: '620px' }"
              [header]="'issue.create' | translate">
      <form (ngSubmit)="save()" class="form" #f="ngForm">
        @if (!fixedProjectId()) {
          <label class="field">
            <span>{{ 'project.title' | translate }} *</span>
            <p-select [(ngModel)]="selectedProjectId" name="project"
                      [options]="projects()" optionLabel="name" optionValue="id"
                      [filter]="true" filterBy="name,key"
                      [placeholder]="'common.select' | translate"
                      (onChange)="onProjectChange()"
                      appendTo="body" />
          </label>
        }
        <label class="field">
          <span>{{ 'issue.type' | translate }} *</span>
          <p-select [(ngModel)]="selectedTypeId" name="type"
                    [options]="issueTypes()" optionLabel="name" optionValue="id"
                    [disabled]="issueTypes().length === 0"
                    appendTo="body" />
        </label>
        <label class="field">
          <span>{{ 'issue.parent' | translate }}</span>
          <p-autoComplete
            name="parent"
            [(ngModel)]="parentSelection"
            [suggestions]="parentSuggestions()"
            (completeMethod)="onParentSearch($event)"
            [forceSelection]="true"
            [delay]="200"
            [minLength]="1"
            appendTo="body"
            [placeholder]="'issue.parent_search_placeholder' | translate"
            [disabled]="!selectedProjectId">
            <ng-template let-i pTemplate="item">
              <span class="parent-suggest">
                <span class="key">{{ i.key }}</span>
                <span class="summary">{{ i.summary }}</span>
              </span>
            </ng-template>
            <ng-template let-i pTemplate="selectedItem">
              <span class="parent-suggest">
                <span class="key">{{ i.key }}</span>
                <span class="summary">{{ i.summary }}</span>
              </span>
            </ng-template>
          </p-autoComplete>
          @if (parentSelection) {
            <button pButton type="button" size="small" [text]="true" class="clear-parent"
                    (click)="clearParent()"
                    [label]="'common.clear' | translate"></button>
          }
        </label>
        <label class="field">
          <span>{{ 'issue.summary' | translate }} *</span>
          <input pInputText [(ngModel)]="model.summary" name="summary"
                 required maxlength="500" />
        </label>
        <label class="field">
          <span>{{ 'issue.description' | translate }}</span>
          <app-rich-text-editor
            [(ngModel)]="model.description"
            name="description"
            [placeholderText]="'issue.description_editor_placeholder' | translate"
          />
        </label>
        <label class="field">
          <span>{{ 'issue.priority' | translate }}</span>
          <p-select [(ngModel)]="model.priority" name="priority"
                    [options]="priorityOptions" optionLabel="label" optionValue="value"
                    appendTo="body" />
        </label>
        <label class="field">
          <span>{{ 'issue.assignee' | translate }}</span>
          <app-user-picker [(userId)]="assigneeUserId" />
        </label>
        <app-issue-custom-fields-form
          [projectId]="selectedProjectId"
          [issueTypeId]="selectedTypeId"
          [issueId]="null"
          [showSaveButton]="false"
        />
        <div class="actions">
          <button pButton type="button" [text]="true"
                  (click)="visible.set(false)"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="submit"
                  [loading]="saving()"
                  [disabled]="!canSubmitForm() || saving()"
                  [label]="'common.save' | translate"></button>
        </div>
      </form>
    </p-dialog>
  `,
  styles: [`
    .form { display: flex; flex-direction: column; gap: 12px; }
    .field { display: flex; flex-direction: column; gap: 6px; font-size: 13px; color: var(--c-text-muted); }
    textarea { resize: vertical; }
    .actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 8px; }
    .parent-suggest { display: inline-flex; gap: 8px; align-items: center; }
    .parent-suggest .key { font-family: monospace; font-weight: 600; font-size: 12px; color: var(--c-text-muted); }
    .parent-suggest .summary { font-size: 13px; }
    .clear-parent { align-self: flex-start; padding: 0; color: var(--c-text-muted); }
  `]
})
export class CreateIssueDialogComponent {
  private readonly issueApi = inject(IssueApiService);
  private readonly projectApi = inject(ProjectApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild(IssueCustomFieldsFormComponent) private cfForm?: IssueCustomFieldsFormComponent;

  /** Pre-fix project (vd. khi mở từ project detail). null = cho phép user chọn. */
  readonly fixedProjectId = input<string | null>(null);
  /** Pre-fill parent issue (e.g. mở dialog từ Epic detail). User vẫn có thể clear. */
  readonly initialParentIssueId = input<string | null>(null);
  readonly visible = model<boolean>(false);
  readonly assigneeUserId = model<string | null>(null);
  readonly created = output<Issue>();

  readonly projects = signal<ProjectSummary[]>([]);
  readonly issueTypes = signal<IssueType[]>([]);
  readonly parentSuggestions = signal<IssueSummary[]>([]);
  readonly saving = signal(false);

  readonly priorityOptions = [
    { label: 'Lowest', value: 1 as IssuePriority },
    { label: 'Low', value: 2 as IssuePriority },
    { label: 'Medium', value: 3 as IssuePriority },
    { label: 'High', value: 4 as IssuePriority },
    { label: 'Highest', value: 5 as IssuePriority }
  ];

  selectedProjectId: string | null = null;
  selectedTypeId: string | null = null;
  parentSelection: IssueSummary | null = null;
  model: { summary: string; description: string; priority: IssuePriority } = {
    summary: '', description: '', priority: 3
  };

  /** Không dùng computed(signal-only): selectedTypeId là field gán async — method + markForCheck. */
  canSubmitForm(): boolean {
    return !!this.selectedProjectId && !!this.selectedTypeId && this.model.summary.trim().length > 0;
  }

  constructor() {
    effect(
      () => {
        const open = this.visible();
        const pid = this.fixedProjectId();
        const parentId = this.initialParentIssueId();
        if (!open) return;
        if (pid) {
          this.selectedProjectId = pid;
          this.loadIssueTypes(pid);
        } else {
          this.selectedProjectId = null;
          this.selectedTypeId = null;
          this.issueTypes.set([]);
          this.projectApi.listMine().subscribe((list) => this.projects.set(list));
        }
        // Pre-fill parent nếu caller cung cấp initialParentIssueId.
        if (parentId) {
          this.issueApi.search({
            issueIds: [parentId], pageIndex: 1, pageSize: 1, sort: 'key', includeArchived: false
          }).subscribe(page => {
            const found = page.items[0] ?? null;
            if (found) {
              this.parentSelection = found;
              this.cdr.markForCheck();
            }
          });
        } else {
          this.parentSelection = null;
        }
      },
      { allowSignalWrites: true }
    );
  }

  onProjectChange(): void {
    this.selectedTypeId = null;
    this.parentSelection = null;
    this.parentSuggestions.set([]);
    this.issueTypes.set([]);
    if (this.selectedProjectId) this.loadIssueTypes(this.selectedProjectId);
  }

  /**
   * Search issues trong project hiện tại để chọn parent. Trả về tối đa 10 kết quả,
   * loại bỏ sub-task (không thể là parent của issue khác).
   *
   * BE chưa expose filter "non-subtask" qua search → filter tại FE bằng issueTypeId
   * lookup. Đủ dùng MVP; nếu list project có nhiều subtask, server-side filter sau.
   */
  onParentSearch(event: AutoCompleteCompleteEvent): void {
    const q = (event.query ?? '').trim();
    const pid = this.selectedProjectId;
    if (!pid) {
      this.parentSuggestions.set([]);
      return;
    }
    const subtaskTypeIds = new Set(
      this.issueTypes().filter(t => t.isSubtask).map(t => t.id)
    );
    this.issueApi.search({
      projectId: pid,
      pageIndex: 1,
      pageSize: 20,
      sort: 'key',
      textSearch: q.length >= 1 ? q : null,
      includeArchived: false
    }).subscribe({
      next: page => {
        const filtered = page.items
          .filter(i => !subtaskTypeIds.has(i.issueTypeId))
          .slice(0, 10);
        this.parentSuggestions.set(filtered);
      },
      error: () => this.parentSuggestions.set([])
    });
  }

  clearParent(): void {
    this.parentSelection = null;
    this.parentSuggestions.set([]);
  }

  private loadIssueTypes(projectId: string): void {
    this.projectApi.getById(projectId).subscribe((p: ProjectDetail) => {
      const nonSubtask = p.issueTypes.filter(t => !t.isSubtask);
      this.issueTypes.set(nonSubtask);
      // Default: Story nếu có, không thì cái đầu tiên.
      const def = nonSubtask.find(t => t.key === 'STORY') ?? nonSubtask[0];
      if (def) this.selectedTypeId = def.id;
      this.cdr.markForCheck();
    });
  }

  save(): void {
    if (!this.canSubmitForm()) return;
    this.saving.set(true);
    const cfPayload = this.cfForm?.getPayloadForCreate() ?? null;
    this.issueApi.create({
      projectId: this.selectedProjectId!,
      issueTypeId: this.selectedTypeId!,
      summary: this.model.summary,
      description: this.model.description || null,
      priority: this.model.priority,
      assigneeId: this.assigneeUserId(),
      parentIssueId: this.parentSelection?.id ?? null,
      dueDate: null,
      storyPoints: null,
      labels: null,
      customFieldValues: cfPayload
    }).subscribe({
      next: (issue) => {
        this.saving.set(false);
        this.visible.set(false);
        this.created.emit(issue);
        this.reset();
      },
      error: () => this.saving.set(false)
    });
  }

  private reset(): void {
    if (!this.fixedProjectId()) this.selectedProjectId = null;
    this.selectedTypeId = null;
    this.parentSelection = null;
    this.parentSuggestions.set([]);
    this.model = { summary: '', description: '', priority: 3 };
    this.assigneeUserId.set(null);
  }
}
