import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { MetadataService } from './metadata.service';
import {
  CreateMetadataRequest,
  METADATA_GROUPS,
  METADATA_TYPE_OPTIONS,
  MetadataDto,
  MetadataType,
  UpdateMetadataRequest
} from './metadata.model';

interface MetadataDraft {
  value: string;
  label: string;
  type: MetadataType;
  description: string;
  validationJson: string;
}

@Component({
  selector: 'app-form-mgmt-metadata-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-head">
      <h1>{{ 'form_mgmt.metadata.title' | translate }}</h1>
      <p class="hint">{{ 'form_mgmt.metadata.intro' | translate }}</p>
    </div>

    <div class="toolbar">
      <input pInputText type="text" [(ngModel)]="keyword" (keyup.enter)="reload()"
             [placeholder]="'form_mgmt.metadata.search_placeholder' | translate" />
      <p-select [(ngModel)]="group" (onChange)="reload()"
                [options]="groupOptions()" optionLabel="label" optionValue="value"
                [showClear]="true"
                [placeholder]="'form_mgmt.metadata.all_groups' | translate"
                styleClass="group-select"></p-select>
      <button pButton type="button" size="small" [outlined]="true"
              (click)="reload()"
              [label]="'common.search' | translate"></button>
      <span class="spacer"></span>
      <button pButton type="button" size="small"
              (click)="openCreate()"
              [label]="'form_mgmt.metadata.new' | translate"></button>
    </div>

    @if (loading()) {
      <p class="hint">{{ 'common.loading' | translate }}</p>
    } @else if (items().length === 0) {
      <p class="hint">{{ 'form_mgmt.metadata.empty' | translate }}</p>
    } @else {
      <table class="tbl">
        <thead>
          <tr>
            <th>{{ 'form_mgmt.metadata.col_value' | translate }}</th>
            <th>{{ 'form_mgmt.metadata.col_label' | translate }}</th>
            <th>{{ 'form_mgmt.metadata.col_type' | translate }}</th>
            <th>{{ 'form_mgmt.metadata.col_group' | translate }}</th>
            <th>{{ 'form_mgmt.metadata.col_desc' | translate }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (m of items(); track m.id) {
            <tr>
              <td class="mono">{{ m.value }}</td>
              <td>{{ m.label }}</td>
              <td>{{ typeLabelKey(m.type) | translate }}</td>
              <td class="mono">{{ m.fieldGroup ?? '—' }}</td>
              <td class="desc">{{ m.description ?? '—' }}</td>
              <td class="actions">
                <button pButton type="button" [text]="true" size="small"
                        (click)="openEdit(m)"
                        [label]="'common.edit' | translate"></button>
                <button pButton type="button" [text]="true" size="small"
                        severity="danger"
                        (click)="confirmDelete(m)"
                        [label]="'common.delete' | translate"></button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }

    <p-dialog
      [(visible)]="dialogVisible"
      [modal]="true"
      [style]="{ width: 'min(640px, 96vw)' }"
      [header]="dialogTitleKey | translate"
      (onHide)="onDialogHide()">
      @if (draftModel) {
        <div class="form-grid">
          <label>
            <span>{{ 'form_mgmt.metadata.col_value' | translate }}</span>
            <input pInputText type="text" [(ngModel)]="draftModel.value"
                   [readonly]="editingId !== null" class="mono" />
            @if (!editingId) {
              <small class="muted">{{ 'form_mgmt.metadata.value_hint' | translate }}</small>
            }
          </label>
          <label>
            <span>{{ 'form_mgmt.metadata.col_label' | translate }}</span>
            <input pInputText type="text" [(ngModel)]="draftModel.label" />
          </label>
          <label>
            <span>{{ 'form_mgmt.metadata.col_type' | translate }}</span>
            <p-select [(ngModel)]="draftModel.type"
                      [options]="typeOptions()" optionLabel="label" optionValue="value"
                      appendTo="body"></p-select>
          </label>
          <label class="full">
            <span>{{ 'form_mgmt.metadata.col_desc' | translate }}</span>
            <textarea pTextarea [(ngModel)]="draftModel.description" rows="2"></textarea>
          </label>
          <label class="full">
            <span>{{ 'form_mgmt.metadata.validation_json' | translate }}</span>
            <textarea pTextarea [(ngModel)]="draftModel.validationJson" rows="3"
                      class="mono" placeholder='{"min": 0, "max": 999999999}'></textarea>
            <small class="muted">{{ 'form_mgmt.metadata.validation_hint' | translate }}</small>
          </label>
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [outlined]="true" (click)="dialogVisible = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="save()" [disabled]="saving() || !canSave()"
                  [label]="'common.save' | translate"></button>
        </div>
      }
    </p-dialog>

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [`
    :host { display: block; padding: 16px 20px; }
    .page-head h1 { margin: 0 0 4px; font-size: 18px; font-weight: 600; }
    .hint { color: var(--c-text-muted); font-size: 13px; margin: 0 0 12px; }
    .toolbar { display: flex; gap: 8px; align-items: center; margin-bottom: 12px; flex-wrap: wrap; }
    .toolbar .spacer { flex: 1; }
    .toolbar input[pInputText] { width: 240px; }
    .toolbar :host ::ng-deep .group-select { min-width: 220px; }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; }
    .tbl th, .tbl td { border: 1px solid var(--c-border); padding: 8px 10px; text-align: left; }
    .tbl th { background: var(--c-surface-2); font-weight: 600; }
    .mono { font-family: ui-monospace, monospace; }
    .desc { max-width: 320px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .actions { white-space: nowrap; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-grid label { display: flex; flex-direction: column; gap: 6px; font-size: 12px; color: var(--c-text-muted); }
    .form-grid label.full { grid-column: 1 / -1; }
    .form-grid label > span { font-weight: 600; color: var(--c-text); }
    .form-grid label small.muted { font-size: 11px; color: var(--c-text-muted); }
    .form-grid input, .form-grid textarea { width: 100%; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
  `]
})
export class MetadataListPageComponent implements OnInit {
  private readonly api = inject(MetadataService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly items = signal<MetadataDto[]>([]);

  keyword = '';
  group: string | null = null;

  dialogVisible = false;
  dialogTitleKey = 'form_mgmt.metadata.new';
  editingId: string | null = null;
  draftModel: MetadataDraft | null = null;

  readonly typeOptionsRaw = METADATA_TYPE_OPTIONS;
  readonly groupOptionsRaw = METADATA_GROUPS;

  // Pre-translated options — populated từ translate.get() (PATTERNS §2.5: instant() bị tree-shake ở prod).
  readonly typeOptions = signal<Array<{ value: MetadataType; label: string }>>([]);
  readonly groupOptions = signal<Array<{ value: string; label: string }>>([]);

  ngOnInit(): void {
    this.loadOptions();
    this.reload();
  }

  private loadOptions(): void {
    const keys = [
      ...this.typeOptionsRaw.map((o) => o.i18nKey),
      ...this.groupOptionsRaw.map((o) => o.i18nKey)
    ];
    this.translate.get(keys).subscribe((t) => {
      this.typeOptions.set(this.typeOptionsRaw.map((o) => ({ value: o.value, label: t[o.i18nKey] })));
      this.groupOptions.set(
        this.groupOptionsRaw.map((o) => ({ value: o.value, label: `${o.value} — ${t[o.i18nKey]}` }))
      );
      this.cdr.markForCheck();
    });
  }

  reload(): void {
    this.loading.set(true);
    this.cdr.markForCheck();
    this.api.search(this.keyword, this.group ?? undefined).subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  openCreate(): void {
    this.editingId = null;
    this.dialogTitleKey = 'form_mgmt.metadata.new';
    this.draftModel = { value: '', label: '', type: MetadataType.Text, description: '', validationJson: '' };
    this.dialogVisible = true;
    this.cdr.markForCheck();
  }

  openEdit(m: MetadataDto): void {
    this.editingId = m.id;
    this.dialogTitleKey = 'form_mgmt.metadata.edit';
    this.draftModel = {
      value: m.value,
      label: m.label,
      type: m.type,
      description: m.description ?? '',
      validationJson: m.validationJson ?? ''
    };
    this.dialogVisible = true;
    this.cdr.markForCheck();
  }

  onDialogHide(): void {
    this.draftModel = null;
    this.editingId = null;
  }

  canSave(): boolean {
    const d = this.draftModel;
    if (!d) return false;
    if (!d.label.trim()) return false;
    if (!this.editingId && !d.value.trim()) return false;
    return true;
  }

  save(): void {
    if (!this.draftModel) return;
    const d = this.draftModel;
    const desc = d.description?.trim() ? d.description.trim() : null;
    const vjson = d.validationJson?.trim() ? d.validationJson.trim() : null;

    this.saving.set(true);
    this.cdr.markForCheck();

    const done = () => {
      this.saving.set(false);
      this.dialogVisible = false;
      this.reload();
    };
    const fail = () => {
      this.saving.set(false);
      this.cdr.markForCheck();
    };

    if (this.editingId) {
      const body: UpdateMetadataRequest = { label: d.label.trim(), type: d.type, description: desc, validationJson: vjson };
      this.api.update(this.editingId, body).subscribe({ next: done, error: fail });
    } else {
      const body: CreateMetadataRequest = {
        value: d.value.trim().toUpperCase(),
        label: d.label.trim(),
        type: d.type,
        description: desc,
        validationJson: vjson
      };
      this.api.create(body).subscribe({ next: done, error: fail });
    }
  }

  confirmDelete(m: MetadataDto): void {
    this.translate
      .get(['form_mgmt.metadata.delete_confirm_title', 'form_mgmt.metadata.delete_confirm_detail',
            'common.delete', 'common.cancel'], { value: m.value })
      .subscribe((t) => {
        this.confirm.confirm({
          header: t['form_mgmt.metadata.delete_confirm_title'],
          message: t['form_mgmt.metadata.delete_confirm_detail'],
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: t['common.delete'],
          rejectLabel: t['common.cancel'],
          acceptButtonStyleClass: 'p-button-danger',
          accept: () => {
            this.api.remove(m.id).subscribe({ next: () => this.reload() });
          }
        });
      });
  }

  typeLabelKey(type: MetadataType): string {
    return this.typeOptionsRaw.find((o) => o.value === type)?.i18nKey ?? 'form_mgmt.metadata.type_opt.text';
  }
}
