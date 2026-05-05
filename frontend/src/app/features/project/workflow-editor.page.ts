import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthService } from '@core/auth/auth.service';
import { ProjectDetail, ProjectRole } from '@core/api/project.service';
import {
  AddStatusRequest,
  AddTransitionRequest,
  CreateWorkflowRequest,
  Workflow,
  WorkflowApiService,
  WorkflowStatus,
  WorkflowTransition
} from '@core/api/workflow.service';

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

const CATEGORY_OPTIONS = [
  { value: 1, labelKey: 'workflow.category.todo' },
  { value: 2, labelKey: 'workflow.category.in_progress' },
  { value: 3, labelKey: 'workflow.category.done' }
] as const;

@Component({
  selector: 'app-workflow-editor-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!isProjectAdmin()) {
      <p class="hint">{{ 'project.admin_only' | translate }}</p>
    } @else {
      <div class="toolbar">
        <label class="lbl">{{ 'workflow_editor.select_workflow' | translate }}</label>
        <p-select [options]="workflowOptions()"
                  [ngModel]="selectedWorkflowId()"
                  (ngModelChange)="onWorkflowSelected($event)"
                  optionLabel="label"
                  optionValue="value"
                  [placeholder]="'common.select' | translate"
                  [style]="{ minWidth: '240px' }" />

        <button pButton type="button" size="small" class="ml"
                (click)="openCreateWorkflow()"
                [label]="'workflow_editor.create_workflow' | translate"></button>
      </div>

      @if (selectedWorkflow(); as wf) {
        <section class="section">
          <h2>{{ 'workflow_editor.statuses' | translate }}</h2>
          <table class="tbl">
            <thead>
              <tr>
                <th>{{ 'workflow_editor.status_name' | translate }}</th>
                <th>{{ 'workflow_editor.status_key' | translate }}</th>
                <th>{{ 'workflow_editor.category' | translate }}</th>
                <th>{{ 'workflow_editor.order' | translate }}</th>
                <th>{{ 'workflow_editor.initial' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (s of sortedStatuses(wf); track s.id) {
                <tr>
                  <td>{{ s.name }}</td>
                  <td class="mono">{{ s.key }}</td>
                  <td>{{ categoryLabelKey(s.category) | translate }}</td>
                  <td>{{ s.order }}</td>
                  <td>
                    @if (wf.initialStatusId === s.id) {
                      <span class="badge">{{ 'workflow_editor.initial_badge' | translate }}</span>
                    } @else {
                      <button pButton type="button" [text]="true" size="small"
                              (click)="setInitial(wf.id, s.id)"
                              [label]="'workflow_editor.set_initial' | translate"></button>
                    }
                  </td>
                  <td class="actions">
                    <button pButton type="button" [text]="true" size="small" class="danger"
                            (click)="confirmRemoveStatus(wf.id, s)"
                            [label]="'common.delete' | translate"></button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
          <button pButton type="button" size="small" (click)="openAddStatus()"
                  [label]="'workflow_editor.add_status' | translate"></button>
        </section>

        <section class="section">
          <h2>{{ 'workflow_editor.transitions' | translate }}</h2>
          <table class="tbl">
            <thead>
              <tr>
                <th>{{ 'workflow_editor.from_status' | translate }}</th>
                <th>{{ 'workflow_editor.to_status' | translate }}</th>
                <th>{{ 'workflow_editor.transition_name' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (t of wf.transitions; track t.id) {
                <tr>
                  <td>{{ fromLabel(wf, t) }}</td>
                  <td>{{ statusName(wf, t.toStatusId) }}</td>
                  <td>{{ t.name }}</td>
                  <td class="actions">
                    <button pButton type="button" [text]="true" size="small" class="danger"
                            (click)="confirmRemoveTransition(wf.id, t)"
                            [label]="'common.delete' | translate"></button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
          <button pButton type="button" size="small" (click)="openAddTransition()"
                  [label]="'workflow_editor.add_transition' | translate"></button>
        </section>
      } @else if (!loadingList()) {
        <p class="hint">{{ 'workflow_editor.no_workflows' | translate }}</p>
      }

      <p-dialog [(visible)]="createWfOpen" [modal]="true"
                [header]="'workflow_editor.create_workflow' | translate"
                [style]="{ width: '420px' }">
        <div class="field">
          <label>{{ 'project.name' | translate }}</label>
          <input pInputText [(ngModel)]="createWfName" name="cwfName" class="w-full" />
        </div>
        <div class="field">
          <label>{{ 'project.key' | translate }}</label>
          <input pInputText [(ngModel)]="createWfKey" name="cwfKey" class="w-full mono" />
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="createWfOpen = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitCreateWorkflow()"
                  [label]="'common.create' | translate"></button>
        </div>
      </p-dialog>

      <p-dialog [(visible)]="addStatusOpen" [modal]="true"
                [header]="'workflow_editor.add_status' | translate"
                [style]="{ width: '440px' }">
        <div class="field">
          <label>{{ 'workflow_editor.status_name' | translate }}</label>
          <input pInputText [(ngModel)]="addStatusName" name="asName" class="w-full" />
        </div>
        <div class="field">
          <label>{{ 'workflow_editor.status_key' | translate }}</label>
          <input pInputText [(ngModel)]="addStatusKey" name="asKey" class="w-full mono" />
        </div>
        <div class="field">
          <label>{{ 'workflow_editor.category' | translate }}</label>
          <p-select [options]="categorySelectOptions()"
                    [(ngModel)]="addStatusCategory"
                    optionLabel="label"
                    optionValue="value"
                    [style]="{ width: '100%' }" />
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="addStatusOpen = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitAddStatus()"
                  [label]="'common.create' | translate"></button>
        </div>
      </p-dialog>

      <p-dialog [(visible)]="addTransOpen" [modal]="true"
                [header]="'workflow_editor.add_transition' | translate"
                [style]="{ width: '480px' }">
        <div class="field">
          <label>{{ 'workflow_editor.from_status' | translate }}</label>
          <p-select [options]="fromStatusOptions()"
                    [(ngModel)]="addTransFromId"
                    optionLabel="label"
                    optionValue="value"
                    [showClear]="true"
                    [placeholder]="'workflow_editor.from_any' | translate"
                    [style]="{ width: '100%' }" />
        </div>
        <div class="field">
          <label>{{ 'workflow_editor.to_status' | translate }}</label>
          <p-select [options]="toStatusOptions()"
                    [(ngModel)]="addTransToId"
                    optionLabel="label"
                    optionValue="value"
                    [style]="{ width: '100%' }" />
        </div>
        <div class="field">
          <label>{{ 'workflow_editor.transition_name' | translate }}</label>
          <input pInputText [(ngModel)]="addTransName" name="atName" class="w-full" />
        </div>
        <div class="dlg-actions">
          <button pButton type="button" [text]="true" (click)="addTransOpen = false"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button" (click)="submitAddTransition()"
                  [label]="'common.create' | translate"></button>
        </div>
      </p-dialog>

      <p-confirmDialog />
    }
  `,
  styles: [`
    .hint { font-size: 13px; color: var(--c-text-muted); max-width: 560px; line-height: 1.45; }
    .toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; margin-bottom: 20px; }
    .lbl { font-size: 13px; color: var(--c-text-muted); }
    .ml { margin-left: auto; }
    .section { margin-bottom: 28px; }
    h2 { font-size: 14px; font-weight: 600; margin: 0 0 12px; color: var(--c-text-muted);
         text-transform: uppercase; letter-spacing: 0.4px; }
    .tbl { width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 12px; }
    .tbl th, .tbl td { text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--c-border); }
    .tbl th { color: var(--c-text-muted); font-weight: 600; }
    .mono { font-family: monospace; font-size: 12px; }
    .actions { text-align: right; }
    .danger { color: var(--c-accent-danger, #dc2626); }
    .badge { font-size: 11px; padding: 2px 8px; border-radius: 4px; background: var(--c-surface-3); }
    .field { margin-bottom: 12px; }
    .field label { display: block; font-size: 12px; margin-bottom: 4px; color: var(--c-text-muted); }
    .w-full { width: 100%; }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
  `]
})
export class WorkflowEditorPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly wfApi = inject(WorkflowApiService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly projectId = signal<string>('');

  readonly workflows = signal<Workflow[]>([]);
  readonly loadingList = signal(true);
  /** Signal để computed `selectedWorkflow` reactive khi đổi workflow. */
  readonly selectedWorkflowId = signal<string | null>(null);

  readonly selectedWorkflow = computed(() => {
    const id = this.selectedWorkflowId();
    if (!id) return null;
    return this.workflows().find((w) => w.id === id) ?? null;
  });

  readonly workflowOptions = computed(() =>
    this.workflows().map((w) => ({ label: w.name + (w.isActive ? ' *' : ''), value: w.id }))
  );

  readonly categorySelectOptions = signal<{ value: number; label: string }[]>([]);

  createWfOpen = false;
  createWfName = '';
  createWfKey = '';

  addStatusOpen = false;
  addStatusName = '';
  addStatusKey = '';
  addStatusCategory = 1;

  addTransOpen = false;
  addTransFromId = '';
  addTransToId: string | null = null;
  addTransName = '';

  ngOnInit(): void {
    const detail = projectDetailFromRoute(this.route);
    this.projectId.set(detail.id);
    forkJoin(CATEGORY_OPTIONS.map((c) => this.translate.get(c.labelKey))).subscribe((labels) => {
      this.categorySelectOptions.set(
        CATEGORY_OPTIONS.map((c, i) => ({ value: c.value, label: labels[i] ?? '' }))
      );
      this.cdr.markForCheck();
    });
    this.reloadWorkflows();
  }

  isProjectAdmin(): boolean {
    const detail = projectDetailFromRoute(this.route);
    const uid = this.auth.user()?.id;
    if (!uid) return false;
    const m = detail.members.find((x) => x.userId === uid);
    return m !== undefined && m.role === (1 as ProjectRole);
  }

  categoryLabelKey(cat: number): string {
    const o = CATEGORY_OPTIONS.find((c) => c.value === cat);
    return o?.labelKey ?? 'workflow.category.todo';
  }

  sortedStatuses(wf: Workflow): WorkflowStatus[] {
    return [...wf.statuses].sort((a, b) => a.order - b.order);
  }

  statusName(wf: Workflow, statusId: string): string {
    return wf.statuses.find((s) => s.id === statusId)?.name ?? statusId;
  }

  fromLabel(wf: Workflow, t: WorkflowTransition): string {
    if (!t.fromStatusId) {
      return '—';
    }
    return this.statusName(wf, t.fromStatusId);
  }

  fromStatusOptions(): { label: string; value: string }[] {
    const wf = this.selectedWorkflow();
    if (!wf) return [];
    return [
      { label: '—', value: '' },
      ...this.sortedStatuses(wf).map((s) => ({ label: s.name, value: s.id }))
    ];
  }

  toStatusOptions(): { label: string; value: string }[] {
    const wf = this.selectedWorkflow();
    if (!wf) return [];
    return this.sortedStatuses(wf).map((s) => ({ label: s.name, value: s.id }));
  }

  onWorkflowSelected(id: string | null): void {
    this.selectedWorkflowId.set(id);
    this.cdr.markForCheck();
  }

  reloadWorkflows(): void {
    this.loadingList.set(true);
    this.wfApi.listByProject(this.projectId()).subscribe({
      next: (list) => {
        this.workflows.set(list);
        const cur = this.selectedWorkflowId();
        if (!cur && list.length > 0) {
          const active = list.find((w) => w.isActive);
          this.selectedWorkflowId.set((active ?? list[0]).id);
        } else if (cur && !list.some((w) => w.id === cur)) {
          this.selectedWorkflowId.set(list.length > 0 ? list[0].id : null);
        }
        this.loadingList.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.workflows.set([]);
        this.loadingList.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  openCreateWorkflow(): void {
    this.createWfName = '';
    this.createWfKey = '';
    this.createWfOpen = true;
  }

  submitCreateWorkflow(): void {
    const name = this.createWfName.trim();
    const key = this.createWfKey.trim().toUpperCase();
    if (!name || !key) return;
    const req: CreateWorkflowRequest = {
      projectId: this.projectId(),
      name,
      key,
      description: null,
      isTemplate: false
    };
    this.wfApi.create(req).subscribe({
      next: (created) => {
        this.createWfOpen = false;
        this.selectedWorkflowId.set(created.id);
        this.reloadWorkflows();
      },
      error: () => {}
    });
  }

  openAddStatus(): void {
    if (!this.selectedWorkflow()) return;
    this.addStatusName = '';
    this.addStatusKey = '';
    this.addStatusCategory = 1;
    this.addStatusOpen = true;
  }

  submitAddStatus(): void {
    const wf = this.selectedWorkflow();
    if (!wf) return;
    const name = this.addStatusName.trim();
    const key = this.addStatusKey.trim().toUpperCase();
    if (!name || !key) return;
    const body: AddStatusRequest = {
      name,
      key,
      category: this.addStatusCategory,
      color: null,
      order: null
    };
    this.wfApi.addStatus(wf.id, body).subscribe({
      next: () => {
        this.addStatusOpen = false;
        this.reloadWorkflows();
      },
      error: () => {}
    });
  }

  setInitial(workflowId: string, statusId: string): void {
    this.wfApi.setInitialStatus(workflowId, statusId).subscribe({
      next: () => this.reloadWorkflows(),
      error: () => {}
    });
  }

  confirmRemoveStatus(workflowId: string, s: WorkflowStatus): void {
    forkJoin({
      header: this.translate.get('workflow_editor.remove_status_title'),
      message: this.translate.get('workflow_editor.remove_status_detail', { name: s.name }),
      accept: this.translate.get('common.delete'),
      reject: this.translate.get('common.cancel')
    }).subscribe((t) => {
      this.confirm.confirm({
        header: t.header,
        message: t.message,
        acceptLabel: t.accept,
        rejectLabel: t.reject,
        accept: () => {
          this.wfApi.removeStatus(workflowId, s.id).subscribe({
            next: () => this.reloadWorkflows(),
            error: () => {}
          });
        }
      });
    });
  }

  confirmRemoveTransition(workflowId: string, t: WorkflowTransition): void {
    forkJoin({
      header: this.translate.get('workflow_editor.remove_transition_title'),
      message: this.translate.get('workflow_editor.remove_transition_detail', { name: t.name }),
      accept: this.translate.get('common.delete'),
      reject: this.translate.get('common.cancel')
    }).subscribe((tr) => {
      this.confirm.confirm({
        header: tr.header,
        message: tr.message,
        acceptLabel: tr.accept,
        rejectLabel: tr.reject,
        accept: () => {
          this.wfApi.removeTransition(workflowId, t.id).subscribe({
            next: () => this.reloadWorkflows(),
            error: () => {}
          });
        }
      });
    });
  }

  openAddTransition(): void {
    if (!this.selectedWorkflow()) return;
    this.addTransFromId = '';
    this.addTransToId = null;
    this.addTransName = '';
    this.addTransOpen = true;
  }

  submitAddTransition(): void {
    const wf = this.selectedWorkflow();
    if (!wf || !this.addTransToId || !this.addTransName.trim()) return;
    const fromId = this.addTransFromId.trim();
    const body: AddTransitionRequest = {
      fromStatusId: fromId.length > 0 ? fromId : null,
      toStatusId: this.addTransToId,
      name: this.addTransName.trim(),
      screenId: null,
      isAutomatic: false
    };
    this.wfApi.addTransition(wf.id, body).subscribe({
      next: () => {
        this.addTransOpen = false;
        this.reloadWorkflows();
      },
      error: () => {}
    });
  }
}
