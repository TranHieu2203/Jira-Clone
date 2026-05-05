import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import {
  IssueLinkApiService,
  IssueLinkDto,
  IssueLinkType,
  IssueLinksForIssueDto,
} from '@core/api/issue-link.service';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';

interface LinkTypeOption {
  value: IssueLinkType;
  /** i18n key (forward direction). */
  labelKey: string;
}

interface LinkRow {
  link: IssueLinkDto;
  /** Issue ở phía bên kia (target nếu outgoing, source nếu incoming). */
  otherIssue: IssueSummary | null;
  /** i18n key cho chiều hiển thị (forward khi outgoing, inverse khi incoming). */
  labelKey: string;
  isOutgoing: boolean;
}

@Component({
  selector: 'app-linked-issues-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    ButtonModule,
    SelectModule,
    AutoCompleteModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="linked">
      <h3>{{ 'issue_link.title' | translate }} <span class="count">{{ rows().length }}</span></h3>

      @if (loading()) {
        <div class="muted">{{ 'common.loading' | translate }}</div>
      } @else if (rows().length === 0) {
        <div class="muted">{{ 'issue_link.empty' | translate }}</div>
      } @else {
        <ul class="list">
          @for (r of rows(); track r.link.id) {
            <li class="row">
              <span class="label">{{ r.labelKey | translate }}</span>
              @if (r.otherIssue; as o) {
                <a class="other" [routerLink]="['/issues', o.key]">
                  <span class="key">{{ o.key }}</span>
                  <span class="summary" [title]="o.summary">{{ o.summary }}</span>
                </a>
              } @else {
                <span class="other muted">—</span>
              }
              @if (r.isOutgoing) {
                <button pButton type="button" size="small" class="p-button-text danger"
                        [label]="'common.delete' | translate"
                        [loading]="deletingId() === r.link.id"
                        (click)="requestDelete(r.link)"></button>
              }
            </li>
          }
        </ul>
      }

      <!-- Add link form -->
      <form class="add" (ngSubmit)="submit()" #f="ngForm" novalidate>
        <p-select
          name="linkType"
          [(ngModel)]="formType"
          [options]="linkTypeOptions"
          optionLabel="labelKey"
          optionValue="value"
          [placeholder]="'issue_link.type_label' | translate"
          appendTo="body"
          styleClass="type-select">
          <ng-template let-opt pTemplate="item">{{ opt.labelKey | translate }}</ng-template>
          <ng-template let-opt pTemplate="selectedItem">{{ opt?.labelKey | translate }}</ng-template>
        </p-select>
        <p-autoComplete
          name="targetIssue"
          [(ngModel)]="targetSelection"
          [suggestions]="suggestions()"
          (completeMethod)="onSearch($event)"
          [forceSelection]="true"
          [delay]="200"
          [minLength]="2"
          appendTo="body"
          styleClass="target-search"
          [placeholder]="'issue_link.search_placeholder' | translate">
          <ng-template let-i pTemplate="item">
            <span class="suggest">
              <span class="key">{{ i.key }}</span>
              <span class="summary">{{ i.summary }}</span>
            </span>
          </ng-template>
        </p-autoComplete>
        <button pButton type="submit" size="small"
                [disabled]="!canSubmit() || saving()"
                [loading]="saving()"
                [label]="'issue_link.add' | translate"></button>
      </form>

      <p-confirmDialog />
    </section>
  `,
  styles: [`
    .linked { margin-bottom: 24px; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px;
      display: flex; align-items: baseline; gap: 6px;
    }
    .count { font-size: 11px; color: var(--c-text-muted); }
    .muted { font-size: 13px; color: var(--c-text-muted); }
    .list { list-style: none; margin: 0 0 12px; padding: 0; display: flex; flex-direction: column; gap: 6px; }
    .row {
      display: flex; align-items: center; gap: 10px;
      padding: 8px 10px; border: 1px solid var(--c-border); border-radius: var(--radius);
      background: var(--c-surface);
    }
    .label {
      font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px;
      color: var(--c-text-muted); min-width: 100px;
    }
    .other { display: inline-flex; align-items: center; gap: 8px; flex: 1; min-width: 0; text-decoration: none; color: inherit; }
    .other:hover .summary { text-decoration: underline; }
    .other .key { font-family: monospace; font-weight: 600; font-size: 12px; flex-shrink: 0; }
    .other .summary { font-size: 13px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .add { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .add :host ::ng-deep .type-select { min-width: 140px; }
    .add :host ::ng-deep .target-search { flex: 1; min-width: 240px; }
    .suggest { display: inline-flex; gap: 8px; align-items: center; }
    .suggest .key { font-family: monospace; font-weight: 600; }
    .danger { color: var(--c-accent-danger); }
  `],
})
export class LinkedIssuesPanelComponent {
  // Public input
  readonly issueId = input.required<string>();

  private readonly api = inject(IssueLinkApiService);
  private readonly issueApi = inject(IssueApiService);
  private readonly confirm = inject(ConfirmationService);

  // ─── State signals ──────────────────────────────────────────────
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly rows = signal<LinkRow[]>([]);
  readonly suggestions = signal<IssueSummary[]>([]);

  // ─── Form bindings ──────────────────────────────────────────────
  formType: IssueLinkType = IssueLinkType.RelatesTo;
  targetSelection: IssueSummary | null = null;

  readonly linkTypeOptions: LinkTypeOption[] = [
    { value: IssueLinkType.RelatesTo, labelKey: 'issue_link.type.relates_to' },
    { value: IssueLinkType.Blocks, labelKey: 'issue_link.type.blocks' },
    { value: IssueLinkType.Duplicates, labelKey: 'issue_link.type.duplicates' },
    { value: IssueLinkType.Clones, labelKey: 'issue_link.type.clones' },
    { value: IssueLinkType.Causes, labelKey: 'issue_link.type.causes' },
  ];

  constructor() {
    // Auto-load mỗi lần issueId đổi.
    effect(() => {
      const id = this.issueId();
      if (id) this.load(id);
    });
  }

  canSubmit(): boolean {
    return !!this.targetSelection
      && !!this.formType
      && this.targetSelection.id !== this.issueId();
  }

  onSearch(event: AutoCompleteCompleteEvent): void {
    const q = (event.query ?? '').trim();
    if (q.length < 2) {
      this.suggestions.set([]);
      return;
    }
    this.issueApi.search({ pageIndex: 1, pageSize: 10, jql: '', textSearch: q }).subscribe({
      next: page => this.suggestions.set(page.items.filter(i => i.id !== this.issueId())),
      error: () => this.suggestions.set([]),
    });
  }

  submit(): void {
    if (!this.canSubmit() || !this.targetSelection) return;
    this.saving.set(true);
    this.api.create({
      sourceIssueId: this.issueId(),
      targetIssueId: this.targetSelection.id,
      linkType: this.formType,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.targetSelection = null;
        this.suggestions.set([]);
        this.load(this.issueId());
      },
      error: () => this.saving.set(false),
    });
  }

  requestDelete(link: IssueLinkDto): void {
    this.confirm.confirm({
      message: 'issue_link.confirm_delete',
      accept: () => this.doDelete(link),
    });
  }

  private doDelete(link: IssueLinkDto): void {
    this.deletingId.set(link.id);
    this.api.delete(link.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.load(this.issueId());
      },
      error: () => this.deletingId.set(null),
    });
  }

  private load(issueId: string): void {
    this.loading.set(true);
    this.api.listByIssue(issueId).subscribe({
      next: data => this.buildRows(data),
      error: () => this.loading.set(false),
    });
  }

  private buildRows(data: IssueLinksForIssueDto): void {
    const allLinks = [
      ...data.outgoing.map(l => ({ link: l, isOutgoing: true })),
      ...data.incoming.map(l => ({ link: l, isOutgoing: false })),
    ];

    // Lấy issue keys cho bên kia của mỗi link để hiển thị.
    const otherIds = new Set<string>();
    for (const r of allLinks) {
      otherIds.add(r.isOutgoing ? r.link.targetIssueId : r.link.sourceIssueId);
    }

    if (otherIds.size === 0) {
      this.rows.set([]);
      this.loading.set(false);
      return;
    }

    // BE search hỗ trợ filter `issueIds` (xem `IssueSearchCriteria.RestrictToIssueIds`).
    this.issueApi.search({ pageIndex: 1, pageSize: 100, jql: '', issueIds: Array.from(otherIds) }).subscribe({
      next: page => {
        const byId = new Map(page.items.map(i => [i.id, i]));
        const rows: LinkRow[] = allLinks.map(r => ({
          link: r.link,
          isOutgoing: r.isOutgoing,
          otherIssue: byId.get(r.isOutgoing ? r.link.targetIssueId : r.link.sourceIssueId) ?? null,
          labelKey: this.labelKey(r.link.linkType, r.isOutgoing),
        }));
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        // Fallback: show without resolved summary.
        const rows: LinkRow[] = allLinks.map(r => ({
          link: r.link,
          isOutgoing: r.isOutgoing,
          otherIssue: null,
          labelKey: this.labelKey(r.link.linkType, r.isOutgoing),
        }));
        this.rows.set(rows);
        this.loading.set(false);
      },
    });
  }

  /**
   * Forward label khi issue hiện tại là source ("blocks", "duplicates"…),
   * inverse label khi là target ("blocked by", "duplicated by"…).
   * RelatesTo (đối xứng) → cùng key cả 2 chiều.
   */
  private labelKey(type: IssueLinkType, isOutgoing: boolean): string {
    if (isOutgoing) {
      switch (type) {
        case IssueLinkType.RelatesTo: return 'issue_link.type.relates_to';
        case IssueLinkType.Blocks: return 'issue_link.type.blocks';
        case IssueLinkType.Duplicates: return 'issue_link.type.duplicates';
        case IssueLinkType.Clones: return 'issue_link.type.clones';
        case IssueLinkType.Causes: return 'issue_link.type.causes';
      }
    } else {
      switch (type) {
        case IssueLinkType.RelatesTo: return 'issue_link.type.relates_to';
        case IssueLinkType.Blocks: return 'issue_link.type.blocked_by';
        case IssueLinkType.Duplicates: return 'issue_link.type.duplicated_by';
        case IssueLinkType.Clones: return 'issue_link.type.cloned_by';
        case IssueLinkType.Causes: return 'issue_link.type.caused_by';
      }
    }
    return 'issue_link.type.relates_to';
  }
}
