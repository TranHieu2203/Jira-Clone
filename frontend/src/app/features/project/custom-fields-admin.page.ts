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
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthService } from '@core/auth/auth.service';
import { ProjectDetail, ProjectRole } from '@core/api/project.service';
import {
  AddContextRequest,
  CreateCustomFieldRequest,
  CustomField,
  CustomFieldApiService,
  CustomFieldContext,
  UpdateCustomFieldRequest
} from '@core/api/custom-field.service';

function projectDetailFromRoute(route: ActivatedRoute): ProjectDetail {
  let r: ActivatedRoute | null = route;
  while (r) {
    const d = r.snapshot.data['projectDetail'];
    if (d) {
      return d as ProjectDetail;
    }
    r = r.parent;
  }
  throw new Error('projectDetail resolver missing');
}

/** Mirrors backend CustomFieldType ordering used in seed/API. */
const FIELD_TYPE_OPTIONS: { value: number; labelKey: string }[] = [
  { value: 1, labelKey: 'field_type.text' },
  { value: 2, labelKey: 'field_type.text_area' },
  { value: 3, labelKey: 'field_type.number' },
  { value: 4, labelKey: 'field_type.decimal' },
  { value: 5, labelKey: 'field_type.date' },
  { value: 6, labelKey: 'field_type.date_time' },
  { value: 10, labelKey: 'field_type.select' },
  { value: 11, labelKey: 'field_type.multi_select' },
  { value: 12, labelKey: 'field_type.cascading' },
  { value: 20, labelKey: 'field_type.user' },
  { value: 21, labelKey: 'field_type.user_multi' },
  { value: 30, labelKey: 'field_type.checkbox' },
  { value: 31, labelKey: 'field_type.url' },
  { value: 32, labelKey: 'field_type.label' },
  { value: 99, labelKey: 'field_type.read_only' }
];

@Component({
  selector: 'app-custom-fields-admin-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
    SelectModule,
    MultiSelectModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!isProjectAdmin()) {
      <p class="hint">{{ 'project.admin_only' | translate }}</p>
    } @else {
      @if (loading()) {
        <p class="hint">{{ 'common.loading' | translate }}</p>
      } @else {
        <div class="toolbar">
          <button pButton type="button" size="small"
                  (click)="openCreate()"
                  [label]="'field_admin.create_field' | translate"></button>
          <button pButton type="button" size="small" [outlined]="true"
                  (click)="bindDemoContexts()"
                  [label]="'field_admin.bind_demo_contexts' | translate"></button>
        </div>

        <table class="tbl">
          <thead>
            <tr>
              <th>{{ 'field_admin.col_key' | translate }}</th>
              <th>{{ 'field_admin.col_name' | translate }}</th>
              <th>{{ 'field_admin.col_type' | translate }}</th>
              <th>{{ 'field_admin.col_system' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (f of fields(); track f.id) {
              <tr>
                <td class="mono">{{ f.key }}</td>
                <td>{{ f.name }}</td>
                <td>{{ typeLabelKey(f.type) | translate }}</td>
                <td>{{ f.isSystem ? ('common.yes' | translate) : ('common.no' | translate) }}</td>
                <td class="actions">
                  <button pButton type="button" [text]="true" size="small"
                          (click)="openEdit(f)"
                          [label]="'common.edit' | translate"></button>
                  @if (!f.isSystem) {
                    <button pButton type="button" [text]="true" size="small" class="danger"
                            (click)="confirmDelete(f)"
                            [label]="'common.delete' | translate"></button>
                  }
                </td>
              </tr>
              <tr class="ctx-row">
                <td colspan="5">
                  <div class="ctx-block">
                    <strong>{{ 'field_admin.contexts_for_project' | translate }}</strong>
                    <ul class="ctx-list">
                      @for (cx of contextsForProject(f); track cx.id) {
                        <li>
                          <span>{{ cx.name }}</span>
                          @if (cx.isGlobal) {
                            <span class="tag">{{ 'field_admin.context_global' | translate }}</span>
                          }
                          @if (cx.isRequired) {
                            <span class="tag">{{ 'field_admin.required' | translate }}</span>
                          }
                          <span class="muted">#{{ cx.displayOrder }}</span>
                          <button pButton type="button" [text]="true" size="small"
                                  (click)="confirmRemoveContext(f, cx)"
                                  [label]="'field_admin.remove_context' | translate"></button>
                        </li>
                      } @empty {
                        <li class="muted">{{ 'field_admin.no_contexts' | translate }}</li>
                      }
                    </ul>
                    <button pButton type="button" size="small"
                            (click)="openAddContext(f)"
                            [label]="'field_admin.add_context' | translate"></button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }

      <p-dialog [(visible)]="createOpen" [modal]="true"
                [header]="'field_admin.create_field' | translate"
                [style]="{ width: '480px' }">
        <div class="field">
          <label>{{ 'field_admin.field_key' | translate }}</label>
          <input pInputText [(ngModel)]="createKey" name="ck" class="w-full mono" />
        </div>
        <div class="field">
          <label>{{ 'field_admin.field_name' | translate }}</label>
          <input pInputText [(ngModel)]="createName" name="cn" class="w-full" />
        </div>
        <div class="field">
          <label>{{ 'field_admin.field_type' | translate }}</label>
          <p-select [options]="typeOptions()"
                    [(ngModel)]="createType"
                    optionLabel="label"
                    optionValue="value"
                    [style]="{ width: '100%' }" />
        </div>
        <div class="field">
          <label>{{ 'issue.description' | translate }}</label>
          <input pInputText [(ngModel)]="createDesc" name="cd" class="w-full" />
        </div>
        <div class="field row">
          <p-checkbox [(ngModel)]="createSearchable" name="cs" [binary]="true" inputId="cs" />
          <label for="cs">{{ 'field_admin.searchable' | translate }}</label>
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="createOpen = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitCreate()"
                  [label]="'common.create' | translate"></button>
        </div>
      </p-dialog>

      <p-dialog [(visible)]="editOpen" [modal]="true"
                [header]="'field_admin.edit_field' | translate"
                [style]="{ width: '480px' }">
        @if (editing(); as ef) {
          <div class="field">
            <label>{{ 'field_admin.field_name' | translate }}</label>
            <input pInputText [(ngModel)]="editName" name="en" class="w-full" />
          </div>
          <div class="field">
            <label>{{ 'issue.description' | translate }}</label>
            <input pInputText [(ngModel)]="editDesc" name="ed" class="w-full" />
          </div>
          <div class="field row">
            <p-checkbox [(ngModel)]="editSearchable" name="es" [binary]="true" inputId="es" />
            <label for="es">{{ 'field_admin.searchable' | translate }}</label>
          </div>
          <div class="dlg-actions">
            <button pButton type="button" [text]="true" (click)="editOpen = false"
                    [label]="'common.cancel' | translate"></button>
            <button pButton type="button" (click)="submitEdit(ef)"
                    [label]="'common.save' | translate"></button>
          </div>
        }
      </p-dialog>

      <p-dialog [(visible)]="ctxOpen" [modal]="true"
                [header]="'field_admin.add_context' | translate"
                [style]="{ width: '520px' }">
        <div class="field">
          <label>{{ 'field_admin.context_name' | translate }}</label>
          <input pInputText [(ngModel)]="ctxName" name="cxn" class="w-full" />
        </div>
        <div class="field row">
          <p-checkbox [(ngModel)]="ctxGlobal" name="cxg" [binary]="true" inputId="cxg"
                      (ngModelChange)="onCtxGlobalChange($event)" />
          <label for="cxg">{{ 'field_admin.context_global' | translate }}</label>
        </div>
        <div class="field row">
          <p-checkbox [(ngModel)]="ctxRequired" name="cxr" [binary]="true" inputId="cxr" />
          <label for="cxr">{{ 'field_admin.required' | translate }}</label>
        </div>
        <div class="field">
          <label>{{ 'field_admin.display_order' | translate }}</label>
          <p-inputNumber [(ngModel)]="ctxOrder" name="cxo" [min]="0" [showButtons]="true" />
        </div>
        @if (!ctxGlobal) {
          <div class="field">
            <label>{{ 'project.issue_types' | translate }}</label>
            <p-multiSelect [options]="issueTypeOptions()"
                           [(ngModel)]="ctxIssueTypeIds"
                           optionLabel="label"
                           optionValue="value"
                           [filter]="true"
                           display="chip"
                           [placeholder]="'common.select' | translate"
                           [style]="{ width: '100%' }"
                           appendTo="body" />
          </div>
        }
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="ctxOpen = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitContext()"
                  [label]="'common.create' | translate"></button>
        </div>
      </p-dialog>

      <p-confirmDialog />
    }
  `,
  styles: [`
    .hint { font-size: 13px; color: var(--c-text-muted); max-width: 560px; line-height: 1.45; }
    .toolbar { margin-bottom: 16px; }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; }
    .tbl th, .tbl td { text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--c-border); }
    .tbl th { color: var(--c-text-muted); font-weight: 600; }
    .ctx-row td { background: var(--c-surface); border-bottom: 1px solid var(--c-border); vertical-align: top; }
    .ctx-block { padding: 8px 0 16px 12px; }
    .ctx-list { list-style: none; margin: 8px 0; padding: 0; }
    .ctx-list li { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin-bottom: 6px; }
    .tag { font-size: 10px; padding: 2px 6px; border-radius: 3px; background: var(--c-surface-3); }
    .muted { font-size: 12px; color: var(--c-text-muted); }
    .mono { font-family: monospace; font-size: 12px; }
    .actions { text-align: right; }
    .danger { color: var(--c-accent-danger, #dc2626); }
    .field { margin-bottom: 12px; }
    .field label { display: block; font-size: 12px; margin-bottom: 4px; color: var(--c-text-muted); }
    .field.row { display: flex; align-items: center; gap: 8px; }
    .field.row label { margin: 0; }
    .w-full { width: 100%; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
  `]
})
export class CustomFieldsAdminPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly cfApi = inject(CustomFieldApiService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  private projectDetail!: ProjectDetail;

  readonly fields = signal<CustomField[]>([]);
  readonly loading = signal(true);
  readonly typeOptions = signal<{ value: number; label: string }[]>([]);

  createOpen = false;
  createKey = '';
  createName = '';
  createType = 1;
  createDesc = '';
  createSearchable = true;

  editOpen = false;
  editing = signal<CustomField | null>(null);
  editName = '';
  editDesc = '';
  editSearchable = true;

  ctxOpen = false;
  ctxField: CustomField | null = null;
  ctxName = '';
  ctxGlobal = false;
  ctxRequired = false;
  ctxOrder = 0;
  ctxIssueTypeIds: string[] = [];

  ngOnInit(): void {
    this.projectDetail = projectDetailFromRoute(this.route);
    forkJoin(FIELD_TYPE_OPTIONS.map((o) => this.translate.get(o.labelKey))).subscribe((labels) => {
      this.typeOptions.set(
        FIELD_TYPE_OPTIONS.map((o, i) => ({ value: o.value, label: labels[i] ?? '' }))
      );
      this.cdr.markForCheck();
    });
    this.reload();
  }

  isProjectAdmin(): boolean {
    const uid = this.auth.user()?.id;
    if (!uid) return false;
    const m = this.projectDetail.members.find((x) => x.userId === uid);
    return m !== undefined && m.role === (1 as ProjectRole);
  }

  issueTypeOptions(): { label: string; value: string }[] {
    return this.projectDetail.issueTypes.map((t) => ({ label: t.name, value: t.id }));
  }

  typeLabelKey(type: number): string {
    return FIELD_TYPE_OPTIONS.find((o) => o.value === type)?.labelKey ?? 'field_type.text';
  }

  contextsForProject(f: CustomField): CustomFieldContext[] {
    const pid = this.projectDetail.id;
    return f.contexts.filter((c) => c.isGlobal || c.projectIds.includes(pid));
  }

  reload(): void {
    this.loading.set(true);
    this.cfApi.list().subscribe({
      next: (list) => {
        this.fields.set(list);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.fields.set([]);
        this.loading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  bindDemoContexts(): void {
    const projectId = this.projectDetail.id;
    this.cfApi.bindDemoContextsToProject(projectId).subscribe({
      next: () => this.reload(),
      error: () => {}
    });
  }

  openCreate(): void {
    this.createKey = '';
    this.createName = '';
    this.createType = 1;
    this.createDesc = '';
    this.createSearchable = true;
    this.createOpen = true;
  }

  submitCreate(): void {
    const key = this.createKey.trim().toLowerCase();
    const name = this.createName.trim();
    if (!key || !name) return;
    const body: CreateCustomFieldRequest = {
      key,
      name,
      type: this.createType,
      description: this.createDesc.trim() || null,
      isSearchable: this.createSearchable,
      configJson: null
    };
    this.cfApi.create(body).subscribe({
      next: () => {
        this.createOpen = false;
        this.reload();
      },
      error: () => {}
    });
  }

  openEdit(f: CustomField): void {
    this.editing.set(f);
    this.editName = f.name;
    this.editDesc = f.description ?? '';
    this.editSearchable = f.isSearchable;
    this.editOpen = true;
  }

  submitEdit(f: CustomField): void {
    const body: UpdateCustomFieldRequest = {
      name: this.editName.trim(),
      description: this.editDesc.trim() || null,
      isSearchable: this.editSearchable,
      configJson: null
    };
    if (!body.name) return;
    this.cfApi.update(f.id, body).subscribe({
      next: () => {
        this.editOpen = false;
        this.editing.set(null);
        this.reload();
      },
      error: () => {}
    });
  }

  confirmDelete(f: CustomField): void {
    forkJoin({
      header: this.translate.get('field_admin.delete_title'),
      message: this.translate.get('field_admin.delete_detail', { name: f.name }),
      accept: this.translate.get('common.delete'),
      reject: this.translate.get('common.cancel')
    }).subscribe((t) => {
      this.confirm.confirm({
        header: t.header,
        message: t.message,
        acceptLabel: t.accept,
        rejectLabel: t.reject,
        accept: () => {
          this.cfApi.delete(f.id).subscribe({
            next: () => this.reload(),
            error: () => {}
          });
        }
      });
    });
  }

  openAddContext(f: CustomField): void {
    this.ctxField = f;
    this.ctxName = f.name + ' — ' + this.projectDetail.key;
    this.ctxGlobal = false;
    this.ctxRequired = false;
    this.ctxOrder = 0;
    this.ctxIssueTypeIds = this.projectDetail.issueTypes.map((t) => t.id);
    this.ctxOpen = true;
  }

  onCtxGlobalChange(isGlobal: boolean): void {
    if (isGlobal) {
      this.ctxIssueTypeIds = [];
    }
  }

  submitContext(): void {
    const f = this.ctxField;
    if (!f || !this.ctxName.trim()) return;
    const pid = this.projectDetail.id;
    const body: AddContextRequest = {
      name: this.ctxName.trim(),
      isGlobal: this.ctxGlobal,
      isRequired: this.ctxRequired,
      defaultValueJson: null,
      projectIds: this.ctxGlobal ? null : [pid],
      issueTypeIds: this.ctxGlobal ? null : this.ctxIssueTypeIds.length > 0 ? this.ctxIssueTypeIds : null,
      displayOrder: this.ctxOrder
    };
    this.cfApi.addContext(f.id, body).subscribe({
      next: () => {
        this.ctxOpen = false;
        this.ctxField = null;
        this.reload();
      },
      error: () => {}
    });
  }

  confirmRemoveContext(f: CustomField, cx: CustomFieldContext): void {
    forkJoin({
      header: this.translate.get('field_admin.remove_context_title'),
      message: this.translate.get('field_admin.remove_context_detail', { name: cx.name }),
      accept: this.translate.get('common.delete'),
      reject: this.translate.get('common.cancel')
    }).subscribe((t) => {
      this.confirm.confirm({
        header: t.header,
        message: t.message,
        acceptLabel: t.accept,
        rejectLabel: t.reject,
        accept: () => {
          this.cfApi.removeContext(f.id, cx.id).subscribe({
            next: () => this.reload(),
            error: () => {}
          });
        }
      });
    });
  }
}
