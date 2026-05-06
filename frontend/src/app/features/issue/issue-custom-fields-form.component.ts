import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  inject,
  input,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';
import {
  CustomField,
  CustomFieldApiService,
  CustomFieldContext,
  CustomFieldType,
  IssueFieldValue
} from '@core/api/custom-field.service';

const CF_TEXT: CustomFieldType = 1;
const CF_NUMBER: CustomFieldType = 3;
const CF_DECIMAL: CustomFieldType = 4;
const CF_DATE: CustomFieldType = 5;
const CF_SELECT: CustomFieldType = 10;
const CF_MULTI: CustomFieldType = 11;

type DraftCell = string | number | string[] | null;

@Component({
  selector: 'app-issue-custom-fields-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    MultiSelectModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!projectId() || !issueTypeId()) {
      <div class="muted"></div>
    } @else if (loading()) {
      <div class="muted">{{ 'common.loading' | translate }}</div>
    } @else if (fields().length === 0) {
      <div class="muted">{{ 'issue.no_custom_fields' | translate }}</div>
    } @else {
      <fieldset class="cf-fieldset">
        <legend>{{ 'issue.custom_fields' | translate }}</legend>
        @for (f of fields(); track f.id) {
          <label class="field">
            <span>{{ f.name }} @if (isRequired(f)) { <span class="req">*</span> }</span>
            @switch (f.type) {
              @case (cfText) {
                <input
                  pInputText
                  [ngModel]="strDraft(f)"
                  (ngModelChange)="setStrDraft(f, $event)"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                />
              }
              @case (cfNumber) {
                <p-inputNumber
                  [ngModel]="numDraft(f)"
                  (ngModelChange)="setNumDraft(f, $event)"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                  [showButtons]="true"
                  [min]="0"
                  [useGrouping]="false"
                  styleClass="cf-input-number"
                />
              }
              @case (cfDecimal) {
                <p-inputNumber
                  [ngModel]="numDraft(f)"
                  (ngModelChange)="setNumDraft(f, $event)"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                  [showButtons]="true"
                  [min]="0"
                  [useGrouping]="false"
                  [minFractionDigits]="0"
                  [maxFractionDigits]="2"
                  styleClass="cf-input-number"
                />
              }
              @case (cfDate) {
                <input
                  type="date"
                  class="p-inputtext p-component cf-date"
                  [ngModel]="strDraft(f)"
                  (ngModelChange)="setStrDraft(f, $event)"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                />
              }
              @case (cfSelect) {
                <p-select
                  [ngModel]="strDraft(f)"
                  (ngModelChange)="setStrDraft(f, $event)"
                  [options]="selectOptions(f)"
                  optionLabel="label"
                  optionValue="value"
                  [placeholder]="'common.select' | translate"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                  appendTo="body"
                />
              }
              @case (cfMulti) {
                <p-multiSelect
                  [ngModel]="multiDraft(f)"
                  (ngModelChange)="setMultiDraft(f, $event)"
                  [options]="selectOptions(f)"
                  optionLabel="label"
                  optionValue="value"
                  [placeholder]="'common.select' | translate"
                  display="chip"
                  [name]="'cf-' + f.key"
                  [attr.data-testid]="'cf-' + f.key"
                  appendTo="body"
                />
              }
              @default {
                <span class="muted">{{ 'issue.custom_field_unsupported' | translate }}</span>
              }
            }
          </label>
        }
        @if (showSaveButton()) {
          <div class="cf-actions">
            <button
              pButton
              type="button"
              [loading]="savingCf()"
              [disabled]="savingCf()"
              (click)="saveCustomFields()"
              [label]="'issue.custom_fields_save' | translate"
            ></button>
          </div>
        }
      </fieldset>
    }
  `,
  styles: [`
    .cf-fieldset {
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 12px 14px;
      margin: 0;
    }
    legend {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--c-text-muted);
      padding: 0 6px;
    }
    .field {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 13px;
      color: var(--c-text-muted);
      margin-bottom: 12px;
    }
    .field:last-of-type { margin-bottom: 0; }
    .req { color: var(--c-accent-danger, #dc2626); }
    .muted { font-size: 12px; color: var(--c-text-muted); }
    .cf-actions { margin-top: 12px; display: flex; justify-content: flex-end; }
    .cf-date { width: 100%; max-width: 280px; padding: 8px 10px; }
    :host ::ng-deep .cf-input-number { width: 100%; max-width: 200px; }
  `]
})
export class IssueCustomFieldsFormComponent {
  private readonly cfApi = inject(CustomFieldApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly projectId = input<string | null>(null);
  readonly issueTypeId = input<string | null>(null);
  readonly issueId = input<string | null>(null);
  readonly showSaveButton = input(false);

  readonly loading = signal(false);
  readonly savingCf = signal(false);
  readonly fields = signal<CustomField[]>([]);

  draft: Record<string, DraftCell> = {};

  readonly cfText = CF_TEXT;
  readonly cfNumber = CF_NUMBER;
  readonly cfDecimal = CF_DECIMAL;
  readonly cfDate = CF_DATE;
  readonly cfSelect = CF_SELECT;
  readonly cfMulti = CF_MULTI;

  constructor() {
    effect(
      () => {
        const pid = this.projectId();
        const tid = this.issueTypeId();
        const iid = this.issueId();
        if (!pid || !tid) {
          this.fields.set([]);
          this.draft = {};
          return;
        }
        this.loadResolved(pid, tid, iid);
      },
      { allowSignalWrites: true }
    );
  }

  strDraft(f: CustomField): string {
    const v = this.draft[f.id];
    return typeof v === 'string' ? v : '';
  }

  setStrDraft(f: CustomField, v: string): void {
    this.draft[f.id] = v;
    this.cdr.markForCheck();
  }

  numDraft(f: CustomField): number | null {
    const v = this.draft[f.id];
    return typeof v === 'number' && !Number.isNaN(v) ? v : null;
  }

  setNumDraft(f: CustomField, n: number | null): void {
    this.draft[f.id] = n;
    this.cdr.markForCheck();
  }

  /**
   * CRITICAL: Phải trả về reference STABLE — nếu mỗi call sinh array mới (như
   * `v.map(String)` cũ), `<p-multiSelect>` so sánh reference qua ngModel thấy "đổi"
   * → emit ngModelChange → re-render → cycle infinite → renderer OOM crash.
   *
   * Strategy: nếu `draft[f.id]` không phải string[], CHUẨN HÓA về string[] đúng
   * 1 lần (in-place store), sau đó luôn trả cùng reference.
   */
  multiDraft(f: CustomField): string[] {
    const v = this.draft[f.id];
    if (Array.isArray(v) && v.every((x) => typeof x === 'string')) {
      return v as string[];
    }
    const norm = Array.isArray(v) ? v.map(String) : [];
    this.draft[f.id] = norm;
    return norm;
  }

  setMultiDraft(f: CustomField, v: string[]): void {
    this.draft[f.id] = v ?? [];
    this.cdr.markForCheck();
  }

  selectOptions(f: CustomField): { label: string; value: string }[] {
    return f.options.map((o) => ({ label: o.label, value: o.value }));
  }

  isRequired(f: CustomField): boolean {
    const pid = this.projectId();
    const tid = this.issueTypeId();
    if (!pid || !tid) return false;
    return this.matchingContext(f, pid, tid)?.isRequired ?? false;
  }

  getPayloadForCreate(): Record<string, unknown> | null {
    const out: Record<string, unknown> = {};
    for (const f of this.fields()) {
      const v = this.buildApiValue(f);
      if (v !== undefined) out[f.id] = v;
    }
    return Object.keys(out).length > 0 ? out : null;
  }

  saveCustomFields(): void {
    const iid = this.issueId();
    const pid = this.projectId();
    const tid = this.issueTypeId();
    if (!iid || !pid || !tid) return;

    const values: { customFieldId: string; value: unknown }[] = [];
    for (const f of this.fields()) {
      const v = this.buildApiValue(f);
      if (v !== undefined) values.push({ customFieldId: f.id, value: v });
    }

    this.savingCf.set(true);
    this.cfApi.setValues(iid, pid, tid, values).subscribe({
      next: () => {
        this.savingCf.set(false);
        this.cdr.markForCheck();
      },
      error: () => this.savingCf.set(false)
    });
  }

  private loadResolved(projectId: string, issueTypeId: string, issueId: string | null): void {
    this.loading.set(true);
    const resolve$ = this.cfApi.resolve(projectId, issueTypeId);
    const values$ = issueId ? this.cfApi.listValues(issueId) : of<IssueFieldValue[]>([]);

    forkJoin({ resolved: resolve$, existing: values$ }).subscribe({
      next: ({ resolved, existing }) => {
        this.fields.set(resolved);
        this.draft = {};
        const byField = new Map(existing.map((v) => [v.customFieldId, v]));
        for (const f of resolved) {
          const row = byField.get(f.id);
          this.draft[f.id] = row ? this.coerceLoaded(f.type, row.value) : this.emptyDraft(f.type);
        }
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.fields.set([]);
        this.draft = {};
        this.loading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  private emptyDraft(type: CustomFieldType): DraftCell {
    switch (type) {
      case CF_MULTI:
        return [];
      case CF_NUMBER:
      case CF_DECIMAL:
        return null;
      default:
        return null;
    }
  }

  private coerceLoaded(type: CustomFieldType, value: unknown): DraftCell {
    const u = this.unwrapApiValue(value);
    if (u === null || u === undefined) return this.emptyDraft(type);

    switch (type) {
      case CF_NUMBER: {
        if (typeof u === 'number' && !Number.isNaN(u)) return Math.trunc(u);
        if (typeof u === 'string') {
          const n = parseInt(u, 10);
          return Number.isNaN(n) ? null : n;
        }
        return null;
      }
      case CF_DECIMAL: {
        if (typeof u === 'number' && !Number.isNaN(u)) return u;
        if (typeof u === 'string') {
          const n = parseFloat(u);
          return Number.isNaN(n) ? null : n;
        }
        return null;
      }
      case CF_MULTI:
        if (Array.isArray(u)) return u.map(String);
        if (typeof u === 'string') return [u];
        return [];
      case CF_DATE: {
        const s = typeof u === 'string' ? u : String(u);
        return s.length >= 10 ? s.slice(0, 10) : s;
      }
      case CF_TEXT:
      case CF_SELECT:
        return typeof u === 'string' ? u : String(u);
      default:
        return typeof u === 'string' ? u : String(u);
    }
  }

  private unwrapApiValue(value: unknown): unknown {
    if (value === null || value === undefined) return null;
    if (typeof value === 'object' && value !== null && 'v' in value) {
      return (value as { v: unknown }).v;
    }
    return value;
  }

  private buildApiValue(f: CustomField): unknown | undefined {
    const raw = this.draft[f.id];
    switch (f.type) {
      case CF_TEXT:
      case CF_SELECT:
      case CF_DATE: {
        if (raw === null || raw === undefined) return undefined;
        const s = typeof raw === 'string' ? raw.trim() : String(raw).trim();
        return s.length === 0 ? undefined : s;
      }
      case CF_NUMBER: {
        if (raw === null || raw === undefined) return undefined;
        if (typeof raw !== 'number' || Number.isNaN(raw)) return undefined;
        return Math.trunc(raw);
      }
      case CF_DECIMAL: {
        if (raw === null || raw === undefined) return undefined;
        if (typeof raw !== 'number' || Number.isNaN(raw)) return undefined;
        return raw;
      }
      case CF_MULTI: {
        if (!Array.isArray(raw) || raw.length === 0) return undefined;
        return raw.map(String);
      }
      default:
        return undefined;
    }
  }

  private matchingContext(f: CustomField, projectId: string, issueTypeId: string): CustomFieldContext | null {
    const applies = (c: CustomFieldContext) =>
      (c.isGlobal || c.projectIds.includes(projectId)) &&
      (c.issueTypeIds.length === 0 || c.issueTypeIds.includes(issueTypeId));
    return (
      f.contexts.find((c) => !c.isGlobal && applies(c)) ??
      f.contexts.find((c) => c.isGlobal && applies(c)) ??
      null
    );
  }
}
