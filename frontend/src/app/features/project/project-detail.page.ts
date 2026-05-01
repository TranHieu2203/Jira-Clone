import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { ProjectApiService, ProjectDetail } from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';

@Component({
  selector: 'app-project-detail-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="p.name">
        <span class="key">{{ p.key }}</span>
      </app-page-header>

      <div class="info">
        @if (p.description) { <p>{{ p.description }}</p> }
        <div class="meta">
          <span>{{ p.type === 1 ? 'Scrum' : 'Kanban' }}</span>
          <span class="dot">•</span>
          <span>{{ p.members.length }} members</span>
          <span class="dot">•</span>
          <span>{{ p.issueTypes.length }} issue types</span>
        </div>
      </div>

      <h3 class="section-title">{{ 'project.issue_types' | translate }}</h3>
      <div class="types">
        @for (t of p.issueTypes; track t.id) {
          <div class="type" [style.borderLeftColor]="t.color || 'var(--c-border-strong)'">
            <strong>{{ t.name }}</strong>
            <span class="key">{{ t.key }}</span>
            @if (t.isSystem) { <span class="badge">system</span> }
            @if (t.isSubtask) { <span class="badge">subtask</span> }
          </div>
        }
      </div>
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    .info { color: var(--c-text-muted); margin-bottom: 16px; }
    .meta { display: flex; gap: 6px; font-size: 12px; margin-top: 8px; }
    .dot { color: var(--c-border-strong); }
    .section-title { font-size: 14px; font-weight: 600; margin: 24px 0 12px; color: var(--c-text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
    .types { display: flex; flex-direction: column; gap: 6px; }
    .type {
      display: flex; align-items: center; gap: 10px; padding: 10px 12px;
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-left: 3px solid var(--c-border-strong);
      border-radius: var(--radius);
    }
    .type .key { font-size: 11px; }
    .badge {
      font-size: 10px; padding: 2px 6px; border-radius: 3px;
      background: var(--c-surface-3); color: var(--c-text-muted);
      text-transform: uppercase;
    }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectDetailPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProjectApiService);
  private readonly ctx = inject(WorkspaceContextService);

  readonly project = signal<ProjectDetail | null>(null);

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    // Tạm: cần workspaceId — lấy từ context hoặc query. MVP sẽ load tất cả
    // project và filter; long-term có endpoint by-key tách riêng.
    // Hiện chỉ có ProjectApiService.getByKey(workspaceId, key) — cần workspaceId.
    // Workaround: list mine + filter bằng key (đủ tốt cho MVP).
    this.api.listMine().subscribe((list) => {
      const summary = list.find(p => p.key === key.toUpperCase());
      if (!summary) return;
      this.api.getById(summary.id).subscribe((detail) => {
        this.project.set(detail);
        this.ctx.setProject(summary);
      });
    });
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }
}
