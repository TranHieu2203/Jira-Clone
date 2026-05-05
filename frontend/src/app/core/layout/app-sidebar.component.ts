import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '@core/auth/auth.service';
import { WorkspaceContextService } from './workspace-context.service';

interface NavItem {
  label: string;
  i18nKey: string;
  icon: string;
  link: string;
  exact?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="sidebar" [class.collapsed]="collapsed()">
      @if (project()) {
        <!-- Project-level nav khi đã vào trong project -->
        <div class="section-title">
          <span class="key">{{ project()!.key }}</span>
          <span class="name" [title]="project()!.name">{{ project()!.name }}</span>
        </div>
        @for (item of projectNav(); track item.link) {
          <a [routerLink]="item.link" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
             class="nav-item" [title]="item.i18nKey | translate">
            <span class="icon">{{ item.icon }}</span>
            <span class="label">{{ item.i18nKey | translate }}</span>
          </a>
        }
      } @else {
        <!-- Workspace-level nav -->
        <div class="section-title">
          <span class="name">{{ 'nav.workspace' | translate }}</span>
        </div>
        @for (item of workspaceNav(); track item.link) {
          <a [routerLink]="item.link" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
             class="nav-item" [title]="item.i18nKey | translate">
            <span class="icon">{{ item.icon }}</span>
            <span class="label">{{ item.i18nKey | translate }}</span>
          </a>
        }
        @if (isSystemAdmin()) {
          <div class="section-title admin-section">
            <span class="name">{{ 'nav.admin_section' | translate }}</span>
          </div>
          @for (item of adminNav(); track item.link) {
            <a [routerLink]="item.link" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
               class="nav-item" [title]="item.i18nKey | translate">
              <span class="icon">{{ item.icon }}</span>
              <span class="label">{{ item.i18nKey | translate }}</span>
            </a>
          }
        }
      }
    </aside>
  `,
  styles: [`
    .sidebar {
      width: 240px;
      flex-shrink: 0;
      background: var(--c-surface);
      border-right: 1px solid var(--c-border);
      padding: 12px 8px;
      transition: width 0.15s ease;
      overflow-y: auto;
    }
    .sidebar.collapsed { width: 56px; }
    .sidebar.collapsed .label,
    .sidebar.collapsed .section-title .name,
    .sidebar.collapsed .section-title .key { display: none; }
    .section-title {
      display: flex; align-items: center; gap: 6px;
      padding: 6px 10px 10px;
      font-size: 11px; font-weight: 600; text-transform: uppercase;
      color: var(--c-text-subtle); letter-spacing: 0.5px;
    }
    .section-title .key {
      font-size: 10px; padding: 1px 5px; border-radius: 3px;
      background: var(--c-surface-3); color: var(--c-text-muted);
    }
    .section-title .name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .section-title.admin-section { margin-top: 12px; padding-top: 12px; border-top: 1px solid var(--c-border); }
    .nav-item {
      display: flex; align-items: center; gap: 10px;
      padding: 7px 10px; border-radius: var(--radius);
      color: var(--c-text-muted); text-decoration: none;
      font-size: 13px; font-weight: 500;
      border-left: 2px solid transparent;
      margin-bottom: 1px;
    }
    .nav-item:hover { background: var(--c-surface-2); color: var(--c-text); text-decoration: none; }
    .nav-item.active {
      background: var(--c-surface-2); color: var(--c-text);
      border-left-color: var(--c-text); font-weight: 600;
    }
    .nav-item .icon { font-size: 15px; flex-shrink: 0; width: 18px; text-align: center; }
    .nav-item .label { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    @media (max-width: 767px) {
      .sidebar:not(.collapsed) {
        position: fixed; top: 48px; left: 0; bottom: 0; z-index: 90;
        box-shadow: var(--shadow-md);
      }
    }
  `]
})
export class AppSidebarComponent {
  private readonly ctx = inject(WorkspaceContextService);
  private readonly auth = inject(AuthService);

  readonly collapsed = input(false);

  readonly workspace = this.ctx.workspace;
  readonly project = this.ctx.project;

  readonly workspaceNav = computed<NavItem[]>(() => [
    { label: 'Home', i18nKey: 'nav.home', icon: '🏠', link: '/', exact: true },
    { label: 'Workspaces', i18nKey: 'nav.workspaces', icon: '📁', link: '/workspaces' },
    { label: 'Projects', i18nKey: 'nav.projects', icon: '📋', link: '/projects' },
    { label: 'My Issues', i18nKey: 'nav.my_issues', icon: '✦', link: '/issues' }
  ]);

  readonly isSystemAdmin = computed(() => (this.auth.user()?.roles ?? []).includes('Admin'));

  readonly adminNav = computed<NavItem[]>(() => [
    { label: 'Email templates', i18nKey: 'nav.admin_email_templates', icon: '✉', link: '/admin/email-templates' },
    { label: 'Email logs', i18nKey: 'nav.admin_email_logs', icon: '☰', link: '/admin/email-logs' }
  ]);

  readonly projectNav = computed<NavItem[]>(() => {
    const p = this.project();
    if (!p) return [];
    const base = `/projects/${p.key}`;
    return [
      { label: 'Overview', i18nKey: 'nav.overview', icon: '⌂', link: base, exact: true },
      { label: 'Backlog', i18nKey: 'nav.backlog', icon: '☰', link: `${base}/backlog` },
      { label: 'Board', i18nKey: 'nav.board', icon: '◫', link: `${base}/board` },
      { label: 'Issues', i18nKey: 'nav.issues', icon: '✦', link: `${base}/issues` },
      { label: 'Reports', i18nKey: 'nav.reports', icon: '⊟', link: `${base}/reports` },
      { label: 'Settings', i18nKey: 'nav.settings', icon: '⚙', link: `${base}/settings` }
    ];
  });
}
