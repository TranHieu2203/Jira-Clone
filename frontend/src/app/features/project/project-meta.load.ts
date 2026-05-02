import { forkJoin, Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { CustomField, CustomFieldApiService } from '@core/api/custom-field.service';
import { ProjectDetail } from '@core/api/project.service';
import { Workflow, WorkflowApiService } from '@core/api/workflow.service';

export interface ProjectFieldsByIssueTypeRow {
  typeId: string;
  typeName: string;
  fields: CustomField[];
}

export function loadProjectWorkflowAndCustomFields(
  p: ProjectDetail,
  wfApi: WorkflowApiService,
  cfApi: CustomFieldApiService
): Observable<{ workflows: Workflow[]; fieldsByType: ProjectFieldsByIssueTypeRow[] }> {
  const types = p.issueTypes.filter((t) => !t.isSubtask);
  const wf$ = wfApi.listByProject(p.id);
  const fields$ =
    types.length === 0
      ? of<ProjectFieldsByIssueTypeRow[]>([])
      : forkJoin(
          types.map((t) =>
            cfApi.resolve(p.id, t.id).pipe(
              map((fields) => ({ typeId: t.id, typeName: t.name, fields }))
            )
          )
        );
  return forkJoin({ workflows: wf$, fieldsByType: fields$ });
}
