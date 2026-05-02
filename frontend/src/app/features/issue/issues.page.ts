import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { IssueApiService, IssueSummary } from '@core/api/issue.service';
import { ProjectApiService } from '@core/api/project.service';
import { PagedList } from '@shared/models/api-response';
import { CreateIssueDialogComponent } from './create-issue.dialog';
import { StatusCacheService } from '@core/api/status-cache.service';

@Component({
  selector: 'app-issues-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, TableModule, InputTextModule,
    AppPageHeaderComponent, CreateIssueDialogComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'issue.title' | translate">
      <button pButton (click)="dialogVisible.set(true)" [label]="'issue.create' | translate"></button>
    </app-page-header>

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
          <td>
            <span class="status-pill" [attr.data-cat]="statusCat(r.currentStatusId)">
              {{ statusName(r.currentStatusId) }}
            </span>
          </td>
          <td><span class="pri pri-{{ r.priority }}">P{{ r.priority }}</span></td>
          <td>{{ r.createdAt | date:'short' }}</td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr><td colspan="5" class="empty">{{ 'issue.empty' | translate }}</td></tr>
      </ng-template>
    </p-table>

    <app-create-issue-dialog
      [fixedProjectId]="fixedProjectId()"
      [(visible)]="dialogVisible"
      (created)="onIssueCreated()" />
  `,
  styles: [`
    .filters { display: flex; gap: 8px; margin-bottom: 16px; }
    .filters input { flex: 0 1 360px; }
    .w-key { width: 100px; }
    .w-status { width: 130px; }
    .w-pri { width: 70px; }
    .w-date { width: 140px; }
    code { font-size: 12px; color: var(--c-text); }
    .status-pill {
      display: inline-block; padding: 2px 8px; border-radius: 10px;
      font-size: 11px; font-weight: 600;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .status-pill[data-cat="1"] { background: var(--c-surface-3); color: var(--c-text-muted); }
    .status-pill[data-cat="2"] { background: #dbeafe; color: #1e40af; }
    .status-pill[data-cat="3"] { background: #d1fae5; color: #065f46; }
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
  private readonly projApi = inject(ProjectApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly statusCache = inject(StatusCacheService);

  readonly page = signal<PagedList<IssueSummary> | null>(null);
  readonly loading = signal(false);
  readonly dialogVisible = signal(false);
  readonly fixedProjectId = signal<string | null>(null);

  textFilter = '';

  ngOnInit(): void {
    // Nếu route /projects/:projectKey/issues, resolve projectKey → projectId.
    const projectKey = this.route.snapshot.paramMap.get('projectKey');
    if (projectKey) {
      this.projApi.listMine().subscribe(list => {
        const found = list.find(p => p.key === projectKey.toUpperCase());
        if (found) this.fixedProjectId.set(found.id);
        this.reload();
      });
    } else {
      this.reload();
    }
  }

  reload(): void {
    this.loading.set(true);
    this.api.search({
      projectId: this.fixedProjectId(),
      textSearch: this.textFilter || null,
      pageIndex: 1,
      pageSize: 50,
      sort: 'key'
    }).subscribe({
      next: (page) => {
        this.page.set(page);
        this.loading.set(false);
        this.warmStatusCacheFor(page.items);
      },
      error: () => this.loading.set(false)
    });
  }

  onIssueCreated(): void {
    this.reload();
  }

  /** Tải workflows cho các project có trong list để map status name. */
  private warmStatusCacheFor(items: readonly IssueSummary[]): void {
    // Search response không có projectId trong summary → tạm dùng fixedProjectId hoặc skip.
    if (this.fixedProjectId()) {
      this.statusCache.ensureProjectLoaded(this.fixedProjectId()!);
    }
  }

  statusName(statusId: string): string {
    return this.statusCache.nameOf(statusId) ?? statusId.slice(0, 8) + '…';
  }

  statusCat(statusId: string): number {
    return this.statusCache.categoryOf(statusId) ?? 1;
  }
}
