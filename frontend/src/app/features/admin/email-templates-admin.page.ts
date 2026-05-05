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
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import {
  EmailAdminApiService,
  EmailTemplateRow,
  UpsertEmailTemplateBody
} from '@core/api/email-admin-api.service';

@Component({
  selector: 'app-email-templates-admin-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    ButtonModule,
    CheckboxModule,
    DialogModule,
    InputTextModule,
    TextareaModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-head">
      <h1>{{ 'admin.email.templates_title' | translate }}</h1>
      <p class="hint">{{ 'admin.email.templates_intro' | translate }}</p>
      <p class="hint subtle">{{ 'admin.email.placeholders_hint' | translate }}</p>
    </div>

    <div class="toolbar">
      <button pButton type="button" size="small"
              (click)="openCreate()"
              [label]="'admin.email.new_template' | translate"></button>
      <a pButton [routerLink]="['/admin/email-logs']" [outlined]="true" size="small"
         [label]="'nav.admin_email_logs' | translate"></a>
    </div>

    @if (loading()) {
      <p class="hint">{{ 'common.loading' | translate }}</p>
    } @else {
      <table class="tbl">
        <thead>
          <tr>
            <th>{{ 'admin.email.col_key' | translate }}</th>
            <th>{{ 'admin.email.col_name' | translate }}</th>
            <th>{{ 'admin.email.col_enabled' | translate }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (t of items(); track t.id) {
            <tr>
              <td class="mono">{{ t.key }}</td>
              <td>{{ t.name }}</td>
              <td>{{ t.isEnabled ? ('common.yes' | translate) : ('common.no' | translate) }}</td>
              <td class="actions">
                <button pButton type="button" [text]="true" size="small"
                        (click)="openEdit(t)"
                        [label]="'common.edit' | translate"></button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }

    <p-dialog
      [(visible)]="dialogVisible"
      [modal]="true"
      [style]="{ width: 'min(720px, 96vw)' }"
      [header]="dialogTitleKey | translate"
      (onHide)="onDialogHide()">
      @if (draftModel) {
        <div class="form-grid">
          <label>
            <span>{{ 'admin.email.col_key' | translate }}</span>
            <input pInputText type="text" [(ngModel)]="draftModel.key" [readonly]="editingKey !== null" />
          </label>
          <label>
            <span>{{ 'admin.email.col_name' | translate }}</span>
            <input pInputText type="text" [(ngModel)]="draftModel.name" />
          </label>
          <label class="full">
            <span>{{ 'admin.email.subject' | translate }}</span>
            <input pInputText type="text" [(ngModel)]="draftModel.subjectTemplate" />
          </label>
          <label class="full">
            <span>{{ 'admin.email.html_body' | translate }}</span>
            <textarea pTextarea [(ngModel)]="draftModel.htmlBodyTemplate" rows="8" class="w-full"></textarea>
          </label>
          <label class="full">
            <span>{{ 'admin.email.text_body' | translate }}</span>
            <textarea pTextarea [(ngModel)]="draftModel.textBodyTemplate" rows="4" class="w-full"></textarea>
          </label>
          <label class="inline">
            <p-checkbox [(ngModel)]="draftModel.isEnabled" [binary]="true" inputId="tpl-enabled" />
            <span for="tpl-enabled">{{ 'admin.email.col_enabled' | translate }}</span>
          </label>
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [outlined]="true" (click)="dialogVisible = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="save()" [disabled]="saving()"
                  [label]="'common.save' | translate"></button>
        </div>
      }
    </p-dialog>
  `,
  styles: [`
    .page-head h1 { margin: 0 0 8px; font-size: 1.25rem; }
    .hint { margin: 0 0 8px; color: var(--c-text-muted); font-size: 13px; }
    .hint.subtle { font-size: 12px; }
    .toolbar { display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 16px; }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; }
    .tbl th, .tbl td { border: 1px solid var(--c-border); padding: 8px 10px; text-align: left; }
    .tbl th { background: var(--c-surface-2); font-weight: 600; }
    .mono { font-family: ui-monospace, monospace; }
    .actions { white-space: nowrap; }
    .form-grid { display: grid; gap: 12px; }
    .form-grid label { display: flex; flex-direction: column; gap: 6px; font-size: 12px; color: var(--c-text-muted); }
    .form-grid label.full { grid-column: 1 / -1; }
    .form-grid label.inline { flex-direction: row; align-items: center; gap: 8px; }
    .w-full { width: 100%; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
  `]
})
export class EmailTemplatesAdminPageComponent implements OnInit {
  private readonly api = inject(EmailAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly items = signal<EmailTemplateRow[]>([]);

  dialogVisible = false;
  dialogTitleKey = 'admin.email.edit_template';
  editingKey: string | null = null;
  draftModel: Draft | null = null;

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.cdr.markForCheck();
    this.api.listTemplates(1, 100).subscribe({
      next: (page) => {
        this.items.set(page.items);
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
    this.editingKey = null;
    this.dialogTitleKey = 'admin.email.new_template';
    this.draftModel = {
      key: '',
      name: '',
      subjectTemplate: '{{issueKey}}',
      htmlBodyTemplate: '<p>{{preview}}</p>',
      textBodyTemplate: '',
      isEnabled: true
    };
    this.dialogVisible = true;
    this.cdr.markForCheck();
  }

  openEdit(t: EmailTemplateRow): void {
    this.editingKey = t.key;
    this.dialogTitleKey = 'admin.email.edit_template';
    this.draftModel = {
      key: t.key,
      name: t.name,
      subjectTemplate: t.subjectTemplate,
      htmlBodyTemplate: t.htmlBodyTemplate,
      textBodyTemplate: t.textBodyTemplate ?? '',
      isEnabled: t.isEnabled
    };
    this.dialogVisible = true;
    this.cdr.markForCheck();
  }

  onDialogHide(): void {
    this.draftModel = null;
    this.editingKey = null;
  }

  save(): void {
    if (!this.draftModel) return;
    const d = this.draftModel;
    const body: UpsertEmailTemplateBody = {
      key: d.key.trim(),
      name: d.name.trim(),
      subjectTemplate: d.subjectTemplate.trim(),
      htmlBodyTemplate: d.htmlBodyTemplate,
      textBodyTemplate: d.textBodyTemplate?.trim() ? d.textBodyTemplate.trim() : null,
      isEnabled: d.isEnabled
    };
    if (!body.key || !body.name) return;
    this.saving.set(true);
    this.cdr.markForCheck();
    this.api.upsertTemplate(body).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible = false;
        this.reload();
      },
      error: () => {
        this.saving.set(false);
        this.cdr.markForCheck();
      }
    });
  }
}

interface Draft {
  key: string;
  name: string;
  subjectTemplate: string;
  htmlBodyTemplate: string;
  textBodyTemplate: string;
  isEnabled: boolean;
}
