import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { ProjectApiService, ProjectSummary } from '@core/api/project.service';

@Component({
  selector: 'app-projects-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'project.title' | translate" />

    @if (loading()) {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    } @else if (rows().length === 0) {
      <div class="empty">{{ 'project.empty' | translate }}</div>
    } @else {
      <div class="grid">
        @for (p of rows(); track p.id) {
          <a class="card" [routerLink]="['/projects', p.key]">
            <div class="key">{{ p.key }}</div>
            <div class="name">{{ p.name }}</div>
            @if (p.description) { <div class="desc">{{ p.description }}</div> }
            <div class="meta">
              <span>{{ p.type === 1 ? 'Scrum' : 'Kanban' }}</span>
              <span class="dot">•</span>
              <span>{{ p.memberCount }} {{ 'project.members' | translate }}</span>
              @if (p.isArchived) { <span class="dot">•</span><span class="arch">archived</span> }
            </div>
          </a>
        }
      </div>
    }
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 14px; }
    .card {
      display: flex; flex-direction: column; gap: 4px; padding: 14px;
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); text-decoration: none; color: var(--c-text);
    }
    .card:hover { border-color: var(--c-border-strong); text-decoration: none; }
    .key { font-family: monospace; font-size: 11px; color: var(--c-text-muted); }
    .name { font-weight: 600; font-size: 15px; }
    .desc { font-size: 13px; color: var(--c-text-muted); margin-top: 4px; }
    .meta { display: flex; gap: 6px; font-size: 11px; color: var(--c-text-subtle); margin-top: 8px; }
    .dot { color: var(--c-border-strong); }
    .arch { color: var(--c-accent-danger); text-transform: uppercase; }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectsPageComponent implements OnInit {
  private readonly api = inject(ProjectApiService);

  readonly rows = signal<ProjectSummary[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.listMine().subscribe({
      next: (list) => { this.rows.set(list); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
