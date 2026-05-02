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
import { Issue, IssueApiService, IssuePriority } from '@core/api/issue.service';
import { IssueType, ProjectApiService, ProjectDetail, ProjectSummary } from '@core/api/project.service';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { IssueCustomFieldsFormComponent } from './issue-custom-fields-form.component';

@Component({
  selector: 'app-create-issue-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslateModule,
    ButtonModule, DialogModule, InputTextModule, TextareaModule, SelectModule,
    UserPickerComponent, IssueCustomFieldsFormComponent
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
          <span>{{ 'issue.summary' | translate }} *</span>
          <input pInputText [(ngModel)]="model.summary" name="summary"
                 required maxlength="500" />
        </label>
        <label class="field">
          <span>{{ 'issue.description' | translate }}</span>
          <textarea pTextarea rows="4" [(ngModel)]="model.description" name="description"></textarea>
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
  `]
})
export class CreateIssueDialogComponent {
  private readonly issueApi = inject(IssueApiService);
  private readonly projectApi = inject(ProjectApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild(IssueCustomFieldsFormComponent) private cfForm?: IssueCustomFieldsFormComponent;

  /** Pre-fix project (vd. khi mở từ project detail). null = cho phép user chọn. */
  readonly fixedProjectId = input<string | null>(null);
  readonly visible = model<boolean>(false);
  readonly assigneeUserId = model<string | null>(null);
  readonly created = output<Issue>();

  readonly projects = signal<ProjectSummary[]>([]);
  readonly issueTypes = signal<IssueType[]>([]);
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
      },
      { allowSignalWrites: true }
    );
  }

  onProjectChange(): void {
    this.selectedTypeId = null;
    this.issueTypes.set([]);
    if (this.selectedProjectId) this.loadIssueTypes(this.selectedProjectId);
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
      parentIssueId: null,
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
    this.model = { summary: '', description: '', priority: 3 };
    this.assigneeUserId.set(null);
  }
}
