import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import { PagedList } from '@shared/models/api-response';

@Component({
  selector: 'app-issues-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, TableModule, InputTextModule,
    AppPageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'issue.title' | translate" />

    <div class="filters">
      <input pInputText
             [(ngModel)]="textFilter"
             (keyup.enter)="reload()"
             [placeholder]="'issue.search_placeholder' | translate" />
      <button pButton (click)="reload()" [label]="'common.search' | translate"></button>
    </div>

    <p-table [value]="page()?.items ?? []" [loading]="loading()" stripedRows>
      <ng-template pTemplate="header">
        <tr>
          <th class="w-key">{{ 'issue.key' | translate }}</th>
          <th>{{ 'issue.summary' | translate }}</th>
          <th class="w-status">{{ 'issue.status' | translate }}</th>
          <th class="w-pri">{{ 'issue.priority' | translate }}</th>
          <th class="w-date">{{ 'issue.created_at' | translate }}</th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-r>
        <tr>
          <td><a [routerLink]="['/issues', r.key]"><code>{{ r.key }}</code></a></td>
          <td>{{ r.summary }}</td>
          <td><span class="status-pill">{{ r.currentStatusId.slice(0, 8) }}…</span></td>
          <td><span class="pri pri-{{ r.priority }}">P{{ r.priority }}</span></td>
          <td>{{ r.createdAt | date:'short' }}</td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr><td colspan="5" class="empty">{{ 'issue.empty' | translate }}</td></tr>
      </ng-template>
    </p-table>
  `,
  styles: [`
    .filters { display: flex; gap: 8px; margin-bottom: 16px; }
    .filters input { flex: 0 1 360px; }
    .w-key { width: 100px; }
    .w-status, .w-pri { width: 110px; }
    .w-date { width: 140px; }
    code { font-size: 12px; color: var(--c-text); }
    .status-pill {
      display: inline-block; padding: 2px 8px; border-radius: 10px;
      font-size: 11px; background: var(--c-surface-3); color: var(--c-text-muted);
      font-family: monospace;
    }
    .pri {
      display: inline-block; width: 24px; height: 22px; line-height: 22px; text-align: center;
      border-radius: 3px; font-size: 11px; font-weight: 600;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .pri-4, .pri-5 { background: var(--c-accent-danger); color: white; }
    .empty { text-align: center; color: var(--c-text-muted); padding: 32px; }
  `]
})
export class IssuesPageComponent implements OnInit {
  private readonly api = inject(IssueApiService);

  readonly page = signal<PagedList<IssueSummary> | null>(null);
  readonly loading = signal(false);

  textFilter = '';

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.search({
      textSearch: this.textFilter || null,
      pageIndex: 1,
      pageSize: 50,
      sort: 'key'
    }).subscribe({
      next: (page) => { this.page.set(page); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
