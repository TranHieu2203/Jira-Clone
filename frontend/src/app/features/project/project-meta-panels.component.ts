import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ProjectDetail } from '@core/api/project.service';
import { Workflow, WorkflowStatus } from '@core/api/workflow.service';
import { ProjectFieldsByIssueTypeRow } from './project-meta.load';

@Component({
  selector: 'app-project-meta-panels',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h3 class="section-title">{{ 'project.workflow_title' | translate }}</h3>
    @if (loadingMeta()) {
      <div class="muted">{{ 'common.loading' | translate }}</div>
    } @else if (workflows().length === 0) {
      <p class="hint">{{ 'project.workflow_empty_hint' | translate }}</p>
    } @else {
      @for (wf of workflows(); track wf.id) {
        <div class="wf-card">
          <div class="wf-head">
            <strong>{{ wf.name }}</strong>
            @if (wf.isActive) {
              <span class="badge">{{ 'project.workflow_active' | translate }}</span>
            }
          </div>
          <div class="status-chips">
            @for (s of sortedStatuses(wf); track s.id) {
              <span class="chip">{{ s.name }}</span>
            }
          </div>
        </div>
      }
    }

    <h3 class="section-title">{{ 'project.issue_types' | translate }}</h3>
    <div class="types">
      @for (t of projectDetail().issueTypes; track t.id) {
        <div class="type" [style.borderLeftColor]="t.color || 'var(--c-border-strong)'">
          <strong>{{ t.name }}</strong>
          <span class="key">{{ t.key }}</span>
          @if (t.isSystem) {
            <span class="badge">system</span>
          }
          @if (t.isSubtask) {
            <span class="badge">subtask</span>
          }
        </div>
      }
    </div>

    <h3 class="section-title">{{ 'project.custom_fields_by_type' | translate }}</h3>
    @if (loadingMeta()) {
      <div class="muted">{{ 'common.loading' | translate }}</div>
    } @else if (fieldsByIssueType().length === 0) {
      <p class="hint">{{ 'project.no_custom_fields_rows' | translate }}</p>
    } @else {
      @for (row of fieldsByIssueType(); track row.typeId) {
        <div class="cf-row">
          <h4>{{ row.typeName }}</h4>
          @if (row.fields.length === 0) {
            <p class="muted">{{ 'issue.no_custom_fields' | translate }}</p>
          } @else {
            <ul class="cf-list">
              @for (f of row.fields; track f.id) {
                <li>
                  <span class="fname">{{ f.name }}</span>
                  <span class="fmeta">{{ f.key }} · {{ fieldTypeLabel(f.type) }}</span>
                </li>
              }
            </ul>
          }
        </div>
      }
    }
  `,
  styles: [`
    .section-title {
      font-size: 14px; font-weight: 600; margin: 24px 0 12px; color: var(--c-text-muted);
      text-transform: uppercase; letter-spacing: 0.5px;
    }
    .types { display: flex; flex-direction: column; gap: 6px; }
    .type {
      display: flex; align-items: center; gap: 10px; padding: 10px 12px;
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-left: 3px solid var(--c-border-strong);
      border-radius: var(--radius);
    }
    .type .key { font-size: 11px; font-family: monospace; color: var(--c-text-muted); }
    .badge {
      font-size: 10px; padding: 2px 6px; border-radius: 3px;
      background: var(--c-surface-3); color: var(--c-text-muted);
      text-transform: uppercase;
    }
    .wf-card {
      padding: 12px 14px; margin-bottom: 10px;
      background: var(--c-surface); border: 1px solid var(--c-border); border-radius: var(--radius);
    }
    .wf-head { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
    .status-chips { display: flex; flex-wrap: wrap; gap: 6px; }
    .chip {
      font-size: 12px; padding: 4px 10px; border-radius: var(--radius);
      border: 1px solid var(--c-border); background: var(--c-bg);
    }
    .cf-row { margin-bottom: 16px; }
    .cf-row h4 { margin: 0 0 8px; font-size: 14px; font-weight: 600; }
    .cf-list { margin: 0; padding-left: 18px; color: var(--c-text); }
    .cf-list li { margin-bottom: 6px; }
    .fname { font-weight: 500; margin-right: 8px; }
    .fmeta { font-size: 11px; color: var(--c-text-muted); font-family: monospace; }
    .hint, .muted { font-size: 13px; color: var(--c-text-muted); }
  `]
})
export class ProjectMetaPanelsComponent {
  readonly projectDetail = input.required<ProjectDetail>();
  readonly workflows = input.required<Workflow[]>();
  readonly fieldsByIssueType = input.required<ProjectFieldsByIssueTypeRow[]>();
  readonly loadingMeta = input.required<boolean>();

  sortedStatuses(wf: Workflow): WorkflowStatus[] {
    return [...wf.statuses].sort((a, b) => a.order - b.order);
  }

  fieldTypeLabel(type: number): string {
    switch (type) {
      case 1:
        return 'Text';
      case 2:
        return 'Text area';
      case 3:
        return 'Number';
      case 5:
        return 'Date';
      case 10:
        return 'Select';
      case 11:
        return 'Multi-select';
      default:
        return String(type);
    }
  }
}
