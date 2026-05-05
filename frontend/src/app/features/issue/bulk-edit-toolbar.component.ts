import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Output,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import {
  BulkUpdateOperationsDto,
  BulkUpdateRequest,
  BulkUpdateResultDto,
  IssueApiService,
} from '@core/api/issue.service';
import { UserPickerComponent } from '@shared/ui/user-picker.component';
import { NotificationService } from '@core/notification/notification.service';

interface DraftForm {
  assigneeId: string | null;
  clearAssignee: boolean;
  priority: number | null;
  addLabelsRaw: string;
  removeLabelsRaw: string;
  archive: 'yes' | 'no' | 'noop';
}

const initialDraft = (): DraftForm => ({
  assigneeId: null,
  clearAssignee: false,
  priority: null,
  addLabelsRaw: '',
  removeLabelsRaw: '',
  archive: 'noop',
});

/**
 * Sticky toolbar hiển thị khi user select ≥ 1 issue trong list. Mở dialog form
 * cho user nhập ops (assignee/priority/labels/archive), confirm rồi gọi BE.
 *
 * Output `applied` emit sau khi BE response — parent reload list + clear selection.
 */
@Component({
  selector: 'app-bulk-edit-toolbar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    DialogModule,
    SelectModule,
    CheckboxModule,
    ConfirmDialogModule,
    UserPickerComponent,
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (count() > 0) {
      <div class="toolbar">
        <span class="count">{{ 'issue.bulk.selected_count' | translate: { count: count() } }}</span>
        <button pButton size="small" (click)="dialogOpen.set(true)"
                [label]="'issue.bulk.edit' | translate"></button>
        <button pButton size="small" [text]="true" (click)="clear.emit()"
                [label]="'issue.bulk.clear_selection' | translate"></button>
      </div>
    }

    <p-dialog
      [(visible)]="dialogOpen"
      [header]="'issue.bulk.edit_title' | translate: { count: count() }"
      [modal]="true"
      [closable]="!saving()"
      [style]="{ width: '520px' }">
      <form (ngSubmit)="onSubmit()" class="form" novalidate>
        <label class="row">
          <span>{{ 'issue.assignee' | translate }}</span>
          <div class="assignee-row">
            <app-user-picker [(userId)]="draft.assigneeId" />
            <label class="check">
              <p-checkbox [(ngModel)]="draft.clearAssignee" name="clearAssignee" [binary]="true" inputId="clearAssignee" />
              <span>{{ 'issue.bulk.clear_assignee' | translate }}</span>
            </label>
          </div>
        </label>

        <label class="row">
          <span>{{ 'issue.priority' | translate }}</span>
          <p-select
            [(ngModel)]="draft.priority"
            name="priority"
            [options]="priorityOptions"
            optionLabel="label"
            optionValue="value"
            [showClear]="true"
            appendTo="body"
            styleClass="prio-select"
            [placeholder]="'issue.bulk.priority_placeholder' | translate" />
        </label>

        <label class="row">
          <span>{{ 'issue.bulk.add_labels' | translate }}</span>
          <input class="text" [(ngModel)]="draft.addLabelsRaw" name="addLabels"
                 [placeholder]="'issue.bulk.labels_placeholder' | translate" />
        </label>

        <label class="row">
          <span>{{ 'issue.bulk.remove_labels' | translate }}</span>
          <input class="text" [(ngModel)]="draft.removeLabelsRaw" name="removeLabels"
                 [placeholder]="'issue.bulk.labels_placeholder' | translate" />
        </label>

        <label class="row">
          <span>{{ 'issue.bulk.archive' | translate }}</span>
          <p-select
            [(ngModel)]="draft.archive"
            name="archive"
            [options]="archiveOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
            styleClass="prio-select" />
        </label>

        <div class="actions">
          <button pButton type="button" [text]="true" size="small"
                  (click)="dialogOpen.set(false)"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="submit" size="small"
                  [disabled]="!canSubmit() || saving()"
                  [loading]="saving()"
                  [label]="'issue.bulk.apply' | translate"></button>
        </div>
      </form>
    </p-dialog>

    <p-confirmDialog />
  `,
  styles: [`
    .toolbar {
      position: sticky; top: 56px; z-index: 5;
      display: flex; align-items: center; gap: 12px;
      padding: 8px 12px; margin-bottom: 12px;
      background: var(--c-surface-2); border: 1px solid var(--c-border);
      border-radius: var(--radius);
    }
    .count { font-size: 13px; font-weight: 600; }
    .form { display: flex; flex-direction: column; gap: 14px; }
    .row { display: flex; flex-direction: column; gap: 4px; }
    .row > span { font-size: 12px; color: var(--c-text-muted); font-weight: 500; }
    .assignee-row { display: flex; flex-direction: column; gap: 6px; }
    .check { display: inline-flex; gap: 6px; align-items: center; font-size: 13px; }
    .text {
      padding: 6px 10px; border: 1px solid var(--c-border); border-radius: var(--radius);
      background: var(--c-surface); font-size: 13px; color: var(--c-text);
    }
    :host ::ng-deep .prio-select { min-width: 220px; }
    .actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 4px; }
  `],
})
export class BulkEditToolbarComponent {
  /** ID issue được chọn — input từ parent (set qua [selectedIds]). */
  readonly selectedIds = input<string[]>([]);
  readonly count = computed(() => this.selectedIds().length);

  /** Emit khi muốn parent clear selection (sau apply hoặc bấm "Clear"). */
  @Output() readonly clear = new EventEmitter<void>();
  /** Emit sau khi BE apply xong, parent reload list. */
  @Output() readonly applied = new EventEmitter<BulkUpdateResultDto>();

  private readonly api = inject(IssueApiService);
  private readonly notify = inject(NotificationService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly dialogOpen = signal(false);
  readonly saving = signal(false);

  draft: DraftForm = initialDraft();

  readonly priorityOptions = [
    { value: 1, label: 'Lowest' },
    { value: 2, label: 'Low' },
    { value: 3, label: 'Medium' },
    { value: 4, label: 'High' },
    { value: 5, label: 'Highest' },
  ];

  readonly archiveOptions = [
    { value: 'noop' as const, label: '— No change —' },
    { value: 'yes' as const, label: 'Archive' },
    { value: 'no' as const, label: 'Unarchive' },
  ];

  canSubmit(): boolean {
    if (this.count() === 0) return false;
    return !!(
      this.draft.assigneeId ||
      this.draft.clearAssignee ||
      this.draft.priority ||
      this.draft.addLabelsRaw.trim() ||
      this.draft.removeLabelsRaw.trim() ||
      this.draft.archive !== 'noop'
    );
  }

  onSubmit(): void {
    if (!this.canSubmit()) return;
    this.confirm.confirm({
      message: this.translate.instant('issue.bulk.confirm', { count: this.count() }),
      accept: () => this.apply(),
    });
  }

  private apply(): void {
    const ops: BulkUpdateOperationsDto = {};
    if (this.draft.clearAssignee) ops.clearAssignee = true;
    else if (this.draft.assigneeId) ops.assigneeId = this.draft.assigneeId;
    if (this.draft.priority) ops.priority = this.draft.priority;
    const adds = this.parseLabels(this.draft.addLabelsRaw);
    const rems = this.parseLabels(this.draft.removeLabelsRaw);
    if (adds.length) ops.addLabels = adds;
    if (rems.length) ops.removeLabels = rems;
    if (this.draft.archive === 'yes') ops.archive = true;
    else if (this.draft.archive === 'no') ops.archive = false;

    const req: BulkUpdateRequest = {
      issueIds: [...this.selectedIds()],
      operations: ops,
    };

    this.saving.set(true);
    this.api.bulkUpdate(req).subscribe({
      next: result => {
        this.saving.set(false);
        this.dialogOpen.set(false);
        this.draft = initialDraft();
        if (result.failed.length > 0) {
          this.notify.info('issue.bulk.partial', { succeeded: result.succeeded.length, failed: result.failed.length });
        }
        this.applied.emit(result);
      },
      error: () => this.saving.set(false),
    });
  }

  private parseLabels(raw: string): string[] {
    return raw
      .split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0);
  }
}
