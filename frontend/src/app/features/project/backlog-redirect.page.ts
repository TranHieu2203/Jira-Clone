import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { ProjectApiService, ProjectSummary } from '@core/api/project.service';

const LAST_PROJECT_KEY = 'jira-clone:last-project-key';

/**
 * Top-level `/backlog` shortcut.
 *
 * - Nếu user đã chọn project trước đó (localStorage) → redirect tới `/projects/<key>/backlog`.
 * - Nếu chưa, gọi `projects/mine`:
 *   - 0 project → message + link `/projects`.
 *   - 1 project → auto redirect.
 *   - 2+ project → list để user pick.
 */
@Component({
  selector: 'app-backlog-redirect',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <div class="page-loading">{{ 'common.loading' | translate }}</div>
    } @else if (projects().length === 0) {
      <div class="empty-state">
        <h2>{{ 'backlog.no_project_title' | translate }}</h2>
        <p>{{ 'backlog.no_project_hint' | translate }}</p>
        <a routerLink="/projects" class="btn-primary">{{ 'nav.projects' | translate }}</a>
      </div>
    } @else {
      <div class="picker">
        <h2>{{ 'backlog.pick_project_title' | translate }}</h2>
        <p class="muted">{{ 'backlog.pick_project_hint' | translate }}</p>
        <ul class="proj-list">
          @for (p of projects(); track p.id) {
            <li>
              <a [routerLink]="['/projects', p.key, 'backlog']" (click)="rememberProject(p.key)">
                <code class="key">{{ p.key }}</code>
                <span class="name">{{ p.name }}</span>
                <span class="arrow">→</span>
              </a>
            </li>
          }
        </ul>
      </div>
    }
  `,
  styles: [`
    .page-loading { padding: 40px; text-align: center; color: var(--c-text-muted); }
    .empty-state, .picker { max-width: 560px; margin: 40px auto; padding: 24px; text-align: center; }
    .empty-state h2, .picker h2 { margin: 0 0 8px; font-size: 18px; font-weight: 600; }
    .muted { color: var(--c-text-muted); font-size: 13px; margin: 0 0 16px; }
    .btn-primary {
      display: inline-block; padding: 8px 16px; border-radius: var(--radius);
      background: var(--c-text); color: var(--c-on-primary); text-decoration: none;
      font-size: 13px; font-weight: 500;
    }
    .btn-primary:hover { opacity: 0.9; }
    .proj-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 8px; }
    .proj-list li a {
      display: flex; align-items: center; gap: 12px;
      padding: 10px 14px;
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      text-decoration: none; color: var(--c-text);
      transition: background 0.1s, border-color 0.1s;
    }
    .proj-list li a:hover { background: var(--c-surface-2); border-color: var(--c-text); }
    .proj-list .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); flex: 0 0 80px; text-align: left; }
    .proj-list .name { flex: 1; text-align: left; font-weight: 500; }
    .proj-list .arrow { color: var(--c-text-subtle); font-size: 16px; }
  `]
})
export class BacklogRedirectPageComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly projApi = inject(ProjectApiService);

  readonly loading = signal(true);
  readonly projects = signal<ProjectSummary[]>([]);

  async ngOnInit(): Promise<void> {
    // 1. Try last-used project from localStorage
    const cachedKey = this.readCached();
    if (cachedKey) {
      void this.router.navigate(['/projects', cachedKey, 'backlog'], { replaceUrl: true });
      return;
    }
    // 2. Fetch user's projects
    try {
      const list = await firstValueFrom(this.projApi.listMine());
      const visible = list.filter((p) => !p.isArchived);
      if (visible.length === 1) {
        const only = visible[0];
        this.rememberProject(only.key);
        void this.router.navigate(['/projects', only.key, 'backlog'], { replaceUrl: true });
        return;
      }
      this.projects.set(visible);
    } finally {
      this.loading.set(false);
    }
  }

  rememberProject(key: string): void {
    try { localStorage.setItem(LAST_PROJECT_KEY, key); } catch { /* ignore quota */ }
  }

  private readCached(): string | null {
    try { return localStorage.getItem(LAST_PROJECT_KEY); } catch { return null; }
  }
}
