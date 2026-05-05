import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CustomFieldApiService } from '@core/api/custom-field.service';
import { ProjectDetail } from '@core/api/project.service';
import { Workflow, WorkflowApiService } from '@core/api/workflow.service';
import { ProjectMetaPanelsComponent } from './project-meta-panels.component';
import { ProjectFieldsByIssueTypeRow, loadProjectWorkflowAndCustomFields } from './project-meta.load';

function projectDetailFromRoute(route: ActivatedRoute): ProjectDetail {
  let r: ActivatedRoute | null = route;
  while (r) {
    const d = r.snapshot.data['projectDetail'];
    if (d) {
      return d as ProjectDetail;
    }
    r = r.parent;
  }
  throw new Error('projectDetail resolver missing');
}

@Component({
  selector: 'app-project-settings-overview',
  standalone: true,
  imports: [CommonModule, TranslateModule, ProjectMetaPanelsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="intro">{{ 'project.settings_overview_hint' | translate }}</p>

    <app-project-meta-panels
      [projectDetail]="project()"
      [workflows]="workflows()"
      [fieldsByIssueType]="fieldsByIssueType()"
      [loadingMeta]="loadingMeta()" />
  `,
  styles: [`
    .intro {
      font-size: 13px; color: var(--c-text-muted); margin: 0 0 16px; max-width: 720px; line-height: 1.45;
    }
  `]
})
export class ProjectSettingsOverviewPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly cfApi = inject(CustomFieldApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly project = signal(projectDetailFromRoute(this.route));
  readonly workflows = signal<Workflow[]>([]);
  readonly fieldsByIssueType = signal<ProjectFieldsByIssueTypeRow[]>([]);
  readonly loadingMeta = signal(false);

  ngOnInit(): void {
    this.loadProjectMeta(this.project());
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
