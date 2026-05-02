import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import {
  ProjectApiService,
  ProjectDetail,
  projectDetailToSummary
} from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';

@Component({
  selector: 'app-project-reports-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="'nav.reports' | translate">
        <span class="key">{{ p.key }}</span>
      </app-page-header>

      <p class="intro">{{ 'project.reports_coming_soon' | translate }}</p>

      <div class="links-bar">
        <a [routerLink]="['/projects', p.key]" class="nav-link">{{ 'nav.overview' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'board']" class="nav-link">{{ 'nav.board' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'issues']" class="nav-link">{{ 'nav.issues' | translate }}</a>
      </div>
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    .intro { font-size: 14px; color: var(--c-text-muted); max-width: 560px; line-height: 1.45; margin-bottom: 20px; }
    .links-bar { display: flex; flex-wrap: wrap; gap: 14px; }
    .nav-link {
      font-size: 13px; font-weight: 500; color: var(--c-primary); text-decoration: none;
    }
    .nav-link:hover { text-decoration: underline; }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectReportsPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProjectApiService);
  private readonly ctx = inject(WorkspaceContextService);

  readonly project = signal<ProjectDetail | null>(null);

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    this.api.getDetailForMemberByKey(key).subscribe((detail) => {
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));
    });
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }
}
