import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { AuthService } from '@core/auth/auth.service';
import { ProjectDetail, ProjectRole, projectDetailToSummary } from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';

@Component({
  selector: 'app-project-settings-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="'project.settings_title' | translate">
        <span class="key">{{ p.key }}</span>
      </app-page-header>

      <div class="links-bar">
        <a [routerLink]="['/projects', p.key]" class="nav-link">{{ 'nav.overview' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'backlog']" class="nav-link">{{ 'nav.backlog' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'board']" class="nav-link">{{ 'nav.board' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'issues']" class="nav-link">{{ 'nav.issues' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'reports']" class="nav-link">{{ 'nav.reports' | translate }}</a>
      </div>

      <nav class="settings-subnav" aria-label="Project settings sections">
        <a routerLink="." routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"
           class="sub-link">{{ 'project.settings_nav_overview' | translate }}</a>
        @if (isProjectAdmin()) {
          <a routerLink="members" routerLinkActive="active" class="sub-link">{{
            'project.settings_nav_members' | translate }}</a>
          <a routerLink="workflow" routerLinkActive="active" class="sub-link">{{
            'project.settings_nav_workflow' | translate }}</a>
          <a routerLink="fields" routerLinkActive="active" class="sub-link">{{
            'project.settings_nav_fields' | translate }}</a>
        }
      </nav>

      <router-outlet />
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    .links-bar { display: flex; flex-wrap: wrap; gap: 14px; margin-bottom: 16px; }
    .nav-link {
      font-size: 13px; font-weight: 500; color: var(--c-primary); text-decoration: none;
    }
    .nav-link:hover { text-decoration: underline; }
    .settings-subnav {
      display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 20px;
      padding-bottom: 12px; border-bottom: 1px solid var(--c-border);
    }
    .sub-link {
      font-size: 13px; padding: 6px 12px; border-radius: var(--radius);
      color: var(--c-text-muted); text-decoration: none; border: 1px solid transparent;
    }
    .sub-link:hover { color: var(--c-text); background: var(--c-surface); }
    .sub-link.active {
      color: var(--c-text); font-weight: 600;
      border-color: var(--c-border); background: var(--c-surface);
    }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectSettingsShellComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly ctx = inject(WorkspaceContextService);
  private readonly auth = inject(AuthService);

  readonly project = signal<ProjectDetail | null>(null);

  readonly isProjectAdmin = computed(() => {
    const p = this.project();
    const uid = this.auth.user()?.id;
    if (!p || !uid) return false;
    const m = p.members.find((x) => x.userId === uid);
    return m !== undefined && m.role === (1 as ProjectRole);
  });

  ngOnInit(): void {
    const d = this.route.snapshot.data['projectDetail'] as ProjectDetail | undefined;
    if (d) {
      this.project.set(d);
      this.ctx.setProject(projectDetailToSummary(d));
    }
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }
}
