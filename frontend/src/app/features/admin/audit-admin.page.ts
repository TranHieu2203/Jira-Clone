import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { AuditApiService, AuditEntryDto } from '@core/api/audit-api.service';
import { PagedList } from '@shared/models/api-response';

@Component({
  selector: 'app-audit-admin-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule, ButtonModule, InputTextModule, SelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-head">
      <h1>{{ 'admin.audit.title' | translate }}</h1>
      <p class="hint">{{ 'admin.audit.intro' | translate }}</p>
    </div>

    <div class="toolbar">
      <a pButton [routerLink]="['/admin/email-templates']" [outlined]="true" size="small"
         [label]="'nav.admin_email_templates' | translate"></a>
      <a pButton [routerLink]="['/admin/email-logs']" [outlined]="true" size="small"
         [label]="'nav.admin_email_logs' | translate"></a>

      <p-select
        [(ngModel)]="scope"
        name="scope"
        [options]="scopeOptions"
        optionLabel="label"
        optionValue="value"
        styleClass="sel"
        [placeholder]="'admin.audit.scope_all' | translate" />
      <input pInputText type="text" [(ngModel)]="action" [placeholder]="'admin.audit.action_filter' | translate" />
      <input pInputText type="text" [(ngModel)]="scopeId" [placeholder]="'admin.audit.scope_id_filter' | translate" />
      <button pButton size="small" (click)="reload()" [label]="'common.search' | translate"></button>
    </div>

    @if (loading()) {
      <p class="hint">{{ 'common.loading' | translate }}</p>
    } @else if ((items()?.items?.length ?? 0) === 0) {
      <p class="hint">{{ 'admin.audit.empty' | translate }}</p>
    } @else {
      <table class="tbl">
        <thead>
          <tr>
            <th>{{ 'admin.audit.col_time' | translate }}</th>
            <th>{{ 'admin.audit.col_actor' | translate }}</th>
            <th>{{ 'admin.audit.col_action' | translate }}</th>
            <th>{{ 'admin.audit.col_scope' | translate }}</th>
            <th>{{ 'admin.audit.col_scope_id' | translate }}</th>
            <th>{{ 'admin.audit.col_payload' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (e of items()?.items ?? []; track e.id) {
            <tr>
              <td class="mono">{{ e.occurredAt | date: 'short' }}</td>
              <td class="mono">{{ shortId(e.actorUserId) }}</td>
              <td><code>{{ e.action }}</code></td>
              <td>{{ e.scope }}</td>
              <td class="mono">{{ shortId(e.scopeId) }}</td>
              <td class="payload" [title]="e.payloadJson ?? ''">{{ e.payloadJson }}</td>
            </tr>
          }
        </tbody>
      </table>

      <div class="pager">
        <button pButton size="small" [text]="true"
                [disabled]="(items()?.pageIndex ?? 1) <= 1"
                (click)="prev()" [label]="'common.prev' | translate"></button>
        <span class="muted">{{ items()?.pageIndex }} / {{ totalPages() }}</span>
        <button pButton size="small" [text]="true"
                [disabled]="(items()?.pageIndex ?? 1) >= totalPages()"
                (click)="next()" [label]="'common.next' | translate"></button>
      </div>
    }
  `,
  styles: [`
    .page-head { margin-bottom: 16px; }
    .page-head h1 { font-size: 18px; margin: 0; }
    .hint { color: var(--c-text-muted); font-size: 13px; }
    .toolbar {
      display: flex; gap: 8px; flex-wrap: wrap; align-items: center;
      margin-bottom: 16px;
    }
    .toolbar input { min-width: 180px; }
    :host ::ng-deep .sel { min-width: 160px; }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; }
    .tbl th, .tbl td {
      padding: 8px 10px; border-bottom: 1px solid var(--c-border);
      text-align: left; vertical-align: top;
    }
    .tbl th { font-weight: 600; color: var(--c-text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; }
    .mono { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    code { font-family: monospace; font-size: 12px; background: var(--c-surface-2); padding: 1px 6px; border-radius: 3px; }
    .payload {
      max-width: 360px; overflow: hidden; text-overflow: ellipsis;
      white-space: nowrap; font-family: monospace; font-size: 11px; color: var(--c-text-muted);
    }
    .pager { display: flex; gap: 8px; align-items: center; margin-top: 12px; }
    .muted { color: var(--c-text-muted); font-size: 12px; }
  `],
})
export class AuditAdminPageComponent implements OnInit {
  private readonly api = inject(AuditApiService);

  readonly items = signal<PagedList<AuditEntryDto> | null>(null);
  readonly loading = signal(false);

  scope: string | null = null;
  action = '';
  scopeId = '';

  readonly scopeOptions = [
    { value: null, label: '— All scopes —' },
    { value: 'org', label: 'org' },
    { value: 'project', label: 'project' },
    { value: 'workflow', label: 'workflow' },
  ];

  ngOnInit(): void { this.reload(); }

  totalPages(): number {
    const p = this.items();
    if (!p || p.pageSize === 0) return 1;
    return Math.max(1, Math.ceil(p.totalCount / p.pageSize));
  }

  shortId(id: string | null): string {
    return id ? id.slice(0, 8) + '…' : '—';
  }

  prev(): void {
    const p = this.items();
    if (!p || p.pageIndex <= 1) return;
    this.fetch(p.pageIndex - 1);
  }

  next(): void {
    const p = this.items();
    if (!p) return;
    if (p.pageIndex >= this.totalPages()) return;
    this.fetch(p.pageIndex + 1);
  }

  reload(): void {
    this.fetch(1);
  }

  private fetch(pageIndex: number): void {
    this.loading.set(true);
    this.api
      .search({
        scope: this.scope,
        action: this.action.trim() || null,
        scopeId: this.scopeId.trim() || null,
        pageIndex,
        pageSize: 50,
      })
      .subscribe({
        next: (p) => {
          this.items.set(p);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
