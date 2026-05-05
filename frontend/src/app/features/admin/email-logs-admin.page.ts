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
import { InputTextModule } from 'primeng/inputtext';
import { EmailAdminApiService, EmailLogRow } from '@core/api/email-admin-api.service';

@Component({
  selector: 'app-email-logs-admin-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule, ButtonModule, InputTextModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-head">
      <h1>{{ 'admin.email.logs_title' | translate }}</h1>
      <p class="hint">{{ 'admin.email.logs_intro' | translate }}</p>
    </div>

    <div class="toolbar">
      <a pButton [routerLink]="['/admin/email-templates']" [outlined]="true" size="small"
         [label]="'nav.admin_email_templates' | translate"></a>
      <input pInputText type="text" [(ngModel)]="filterTemplateKey" [placeholder]="'admin.email.filter_template' | translate" />
      <input pInputText type="text" [(ngModel)]="filterTo" [placeholder]="'admin.email.filter_to' | translate" />
      <select class="sel" [(ngModel)]="filterStatus">
        <option [ngValue]="null">{{ 'admin.email.log_status_all' | translate }}</option>
        <option [ngValue]="'Pending'">{{ 'admin.email.log_status_pending' | translate }}</option>
        <option [ngValue]="'Sent'">{{ 'admin.email.log_status_sent' | translate }}</option>
        <option [ngValue]="'Failed'">{{ 'admin.email.log_status_failed' | translate }}</option>
        <option [ngValue]="'Skipped'">{{ 'admin.email.log_status_skipped' | translate }}</option>
      </select>
      <button pButton type="button" size="small" (click)="reload()"
              [label]="'common.search' | translate"></button>
    </div>

    @if (loading()) {
      <p class="hint">{{ 'common.loading' | translate }}</p>
    } @else {
      <table class="tbl">
        <thead>
          <tr>
            <th>{{ 'admin.email.col_created' | translate }}</th>
            <th>{{ 'admin.email.col_template' | translate }}</th>
            <th>{{ 'admin.email.col_to' | translate }}</th>
            <th>{{ 'admin.email.col_status' | translate }}</th>
            <th>{{ 'admin.email.col_error' | translate }}</th>
            <th class="w-action">{{ 'admin.email.col_action' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of items(); track r.id) {
            <tr>
              <td class="mono">{{ r.createdAt | date : 'short' }}</td>
              <td class="mono">{{ r.templateKey }}</td>
              <td>{{ r.toEmail }}</td>
              <td>{{ statusLabelKey(r.status) | translate }}</td>
              <td class="err">{{ r.error ?? '—' }}</td>
              <td>
                @if (r.status === 2) {
                  <button pButton type="button" size="small" [text]="true"
                          [loading]="retryingId() === r.id"
                          (click)="retry(r)"
                          [label]="'admin.email.retry' | translate"></button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: [`
    .page-head h1 { margin: 0 0 8px; font-size: 1.25rem; }
    .hint { margin: 0 0 8px; color: var(--c-text-muted); font-size: 13px; }
    .toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 16px; }
    .toolbar input { min-width: 140px; }
    .sel {
      min-width: 160px; padding: 8px 10px; border-radius: var(--radius);
      border: 1px solid var(--c-border); background: var(--c-surface); color: var(--c-text);
      font-size: 13px;
    }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; }
    .tbl th, .tbl td { border: 1px solid var(--c-border); padding: 8px 10px; text-align: left; vertical-align: top; }
    .tbl th { background: var(--c-surface-2); font-weight: 600; }
    .mono { font-family: ui-monospace, monospace; font-size: 12px; }
    .err { max-width: 280px; word-break: break-word; color: var(--c-text-muted); }
  `]
})
export class EmailLogsAdminPageComponent implements OnInit {
  private readonly api = inject(EmailAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly loading = signal(false);
  readonly items = signal<EmailLogRow[]>([]);
  readonly retryingId = signal<string | null>(null);

  filterTemplateKey = '';
  filterTo = '';
  filterStatus: string | null = null;

  ngOnInit(): void {
    this.reload();
  }

  statusLabelKey(status: number): string {
    switch (status) {
      case 0:
        return 'admin.email.log_status_pending';
      case 1:
        return 'admin.email.log_status_sent';
      case 2:
        return 'admin.email.log_status_failed';
      case 3:
        return 'admin.email.log_status_skipped';
      default:
        return 'admin.email.log_status_unknown';
    }
  }

  reload(): void {
    this.loading.set(true);
    this.cdr.markForCheck();
    const st = this.filterStatus ?? undefined;
    this.api.listLogs(1, 100, this.filterTemplateKey, this.filterTo, st).subscribe({
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

  /** R6 DLQ: retry log Failed → BE re-render từ ArgsJson + insert log mới. Reload list để thấy result. */
  retry(row: EmailLogRow): void {
    this.retryingId.set(row.id);
    this.api.retry(row.id).subscribe({
      next: () => {
        this.retryingId.set(null);
        this.reload();
      },
      error: () => {
        this.retryingId.set(null);
        this.cdr.markForCheck();
      }
    });
  }
}
