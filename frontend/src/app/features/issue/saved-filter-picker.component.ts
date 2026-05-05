import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  OnInit,
  Output,
  inject,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import {
  CreateSavedFilterRequest,
  SavedFilterApiService,
  SavedFilterDto,
} from '@core/api/saved-filter.service';
import { AuthService } from '@core/auth/auth.service';

interface DraftForm {
  name: string;
  description: string;
  isShared: boolean;
}

/**
 * Picker + manager cho saved filter. Wrap quanh JQL input của trang issue search.
 *
 * Output `applied` emit JQL string để parent gắn vào ô search + reload.
 * Input `currentJql` là JQL đang gõ — dùng cho nút "Save current" và highlight match.
 */
@Component({
  selector: 'app-saved-filter-picker',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    SelectModule,
    DialogModule,
    InputTextModule,
    CheckboxModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="filter-bar">
      <p-select
        [(ngModel)]="selectedId"
        [options]="filterOptions()"
        optionLabel="label"
        optionValue="value"
        [placeholder]="'saved_filter.pick' | translate"
        appendTo="body"
        styleClass="picker"
        (onChange)="onPick($event.value)">
        <ng-template let-opt pTemplate="item">
          <span class="opt">
            <span class="name">{{ opt.label }}</span>
            @if (opt.shared) { <span class="badge">{{ 'saved_filter.shared' | translate }}</span> }
          </span>
        </ng-template>
      </p-select>

      <button pButton type="button" size="small" [text]="true"
              [disabled]="!currentJql() || saving()"
              [loading]="saving()"
              (click)="openCreate()"
              [label]="'saved_filter.save_current' | translate"></button>

      @if (selectedFilter(); as sf) {
        @if (sf.ownerUserId === currentUserId()) {
          <button pButton type="button" size="small" [text]="true"
                  (click)="openEdit(sf)"
                  [label]="'common.edit' | translate"></button>
          <button pButton type="button" size="small" [text]="true" class="danger"
                  [loading]="deleting() === sf.id"
                  (click)="requestDelete(sf)"
                  [label]="'common.delete' | translate"></button>
        }
      }
    </div>

    <p-dialog
      [(visible)]="dialogOpen"
      [header]="(editingId() ? 'saved_filter.edit_title' : 'saved_filter.save_title') | translate"
      [modal]="true"
      [closable]="!saving()"
      [style]="{ width: '480px' }">
      <form (ngSubmit)="submit()" #f="ngForm" class="form" novalidate>
        <label class="row">
          <span>{{ 'saved_filter.name_label' | translate }}</span>
          <input pInputText required [(ngModel)]="draft.name" name="name" maxlength="120" />
        </label>
        <label class="row">
          <span>{{ 'saved_filter.description_label' | translate }}</span>
          <input pInputText [(ngModel)]="draft.description" name="description" maxlength="1000" />
        </label>
        <label class="row jql-preview">
          <span>{{ 'saved_filter.jql_label' | translate }}</span>
          <code>{{ currentJql() || '—' }}</code>
        </label>
        <label class="row check">
          <p-checkbox [(ngModel)]="draft.isShared" name="isShared" [binary]="true" inputId="isShared" />
          <label for="isShared">{{ 'saved_filter.is_shared' | translate }}</label>
        </label>
        <div class="actions">
          <button pButton type="button" [text]="true" size="small"
                  (click)="dialogOpen.set(false)"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="submit" size="small"
                  [loading]="saving()"
                  [disabled]="!canSubmit() || saving()"
                  [label]="'common.save' | translate"></button>
        </div>
      </form>
    </p-dialog>

    <p-confirmDialog />
  `,
  styles: [`
    .filter-bar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; margin-bottom: 8px; }
    :host ::ng-deep .picker { min-width: 220px; max-width: 320px; }
    .opt { display: inline-flex; gap: 8px; align-items: center; }
    .opt .badge {
      font-size: 10px; padding: 1px 6px; border-radius: 8px;
      background: var(--c-surface-3); color: var(--c-text-muted);
      text-transform: uppercase; letter-spacing: 0.5px;
    }
    .danger :host ::ng-deep .p-button-label { color: var(--c-accent-danger); }
    .form { display: flex; flex-direction: column; gap: 12px; }
    .row { display: flex; flex-direction: column; gap: 4px; }
    .row > span { font-size: 12px; color: var(--c-text-muted); font-weight: 500; }
    .row.check { flex-direction: row; align-items: center; gap: 8px; }
    .row.check label { font-size: 13px; cursor: pointer; }
    .jql-preview code {
      display: block; font-size: 12px; padding: 8px; border: 1px solid var(--c-border);
      border-radius: var(--radius); background: var(--c-surface-2);
      overflow-x: auto; white-space: pre-wrap; word-break: break-all;
    }
    .actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
  `],
})
export class SavedFilterPickerComponent implements OnInit {
  /** JQL đang gõ ở parent — dùng cho nút "Save current" và preview trong dialog. */
  readonly currentJql = input<string>('');

  /** Emit khi user chọn 1 filter saved → parent set JQL + reload. */
  @Output() readonly applied = new EventEmitter<string>();

  private readonly api = inject(SavedFilterApiService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly filters = signal<SavedFilterDto[]>([]);
  readonly saving = signal(false);
  readonly deleting = signal<string | null>(null);
  readonly dialogOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  selectedId: string | null = null;

  draft: DraftForm = { name: '', description: '', isShared: false };

  ngOnInit(): void { this.reload(); }

  currentUserId(): string | null {
    return this.auth.user()?.id ?? null;
  }

  selectedFilter(): SavedFilterDto | null {
    return this.filters().find(f => f.id === this.selectedId) ?? null;
  }

  filterOptions(): { value: string; label: string; shared: boolean }[] {
    return this.filters().map(f => ({ value: f.id, label: f.name, shared: f.isShared }));
  }

  onPick(id: string): void {
    const f = this.filters().find(x => x.id === id);
    if (f) this.applied.emit(f.jql);
  }

  openCreate(): void {
    if (!this.currentJql().trim()) return;
    this.editingId.set(null);
    this.draft = { name: '', description: '', isShared: false };
    this.dialogOpen.set(true);
  }

  openEdit(f: SavedFilterDto): void {
    this.editingId.set(f.id);
    this.draft = {
      name: f.name,
      description: f.description ?? '',
      isShared: f.isShared,
    };
    this.dialogOpen.set(true);
  }

  canSubmit(): boolean {
    return !!this.draft.name?.trim() && !!this.currentJql().trim();
  }

  submit(): void {
    if (!this.canSubmit()) return;
    const payload: CreateSavedFilterRequest = {
      name: this.draft.name.trim(),
      jql: this.currentJql().trim(),
      description: this.draft.description?.trim() || null,
      isShared: this.draft.isShared,
    };
    this.saving.set(true);
    const editing = this.editingId();
    const obs = editing
      ? this.api.update(editing, payload)
      : this.api.create(payload);
    obs.subscribe({
      next: result => {
        this.saving.set(false);
        this.dialogOpen.set(false);
        this.editingId.set(null);
        this.selectedId = result.id;
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  requestDelete(f: SavedFilterDto): void {
    this.confirm.confirm({
      message: this.translate.instant('saved_filter.confirm_delete'),
      accept: () => this.doDelete(f),
    });
  }

  private doDelete(f: SavedFilterDto): void {
    this.deleting.set(f.id);
    this.api.delete(f.id).subscribe({
      next: () => {
        this.deleting.set(null);
        if (this.selectedId === f.id) this.selectedId = null;
        this.reload();
      },
      error: () => this.deleting.set(null),
    });
  }

  private reload(): void {
    this.api.listMine().subscribe({
      next: list => this.filters.set(list),
      error: () => this.filters.set([]),
    });
  }
}
