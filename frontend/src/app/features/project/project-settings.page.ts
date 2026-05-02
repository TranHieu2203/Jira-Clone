import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
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
import { CustomFieldApiService } from '@core/api/custom-field.service';
import {
  ProjectApiService,
  ProjectDetail,
  projectDetailToSummary
} from '@core/api/project.service';
import { Workflow, WorkflowApiService } from '@core/api/workflow.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { ProjectMetaPanelsComponent } from './project-meta-panels.component';
import { ProjectFieldsByIssueTypeRow, loadProjectWorkflowAndCustomFields } from './project-meta.load';

@Component({
  selector: 'app-project-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule,
    AppPageHeaderComponent,
    ProjectMetaPanelsComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="'project.settings_title' | translate">
        <span class="key">{{ p.key }}</span>
      </app-page-header>

      <p class="intro">{{ 'project.settings_readonly_hint' | translate }}</p>

      <div class="links-bar">
        <a [routerLink]="['/projects', p.key]" class="nav-link">{{ 'nav.overview' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'backlog']" class="nav-link">{{ 'nav.backlog' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'board']" class="nav-link">{{ 'nav.board' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'issues']" class="nav-link">{{ 'nav.issues' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'reports']" class="nav-link">{{ 'nav.reports' | translate }}</a>
      </div>

      <app-project-meta-panels
        [projectDetail]="p"
        [workflows]="workflows()"
        [fieldsByIssueType]="fieldsByIssueType()"
        [loadingMeta]="loadingMeta()" />
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    .intro {
      font-size: 13px; color: var(--c-text-muted); margin: 0 0 16px; max-width: 720px; line-height: 1.45;
    }
    .links-bar { display: flex; flex-wrap: wrap; gap: 14px; margin-bottom: 20px; }
    .nav-link {
      font-size: 13px; font-weight: 500; color: var(--c-primary); text-decoration: none;
    }
    .nav-link:hover { text-decoration: underline; }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectSettingsPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProjectApiService);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly cfApi = inject(CustomFieldApiService);
  private readonly ctx = inject(WorkspaceContextService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly project = signal<ProjectDetail | null>(null);
  readonly workflows = signal<Workflow[]>([]);
  readonly fieldsByIssueType = signal<ProjectFieldsByIssueTypeRow[]>([]);
  readonly loadingMeta = signal(false);

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    this.api.getDetailForMemberByKey(key).subscribe((detail) => {
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));
      this.loadProjectMeta(detail);
    });
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }

  private loadProjectMeta(p: ProjectDetail): void {
    this.loadingMeta.set(true);
    loadProjectWorkflowAndCustomFields(p, this.wfApi, this.cfApi).subscribe({
      next: ({ workflows, fieldsByType }) => {
        this.workflows.set(workflows);
        this.fieldsByIssueType.set(fieldsByType);
        this.loadingMeta.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.workflows.set([]);
        this.fieldsByIssueType.set([]);
        this.loadingMeta.set(false);
        this.cdr.markForCheck();
      }
    });
  }
}
