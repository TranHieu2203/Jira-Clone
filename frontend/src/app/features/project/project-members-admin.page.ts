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
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { ProjectApiService, ProjectDetail, ProjectMember, ProjectRole, projectDetailToSummary } from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { UserPickerComponent } from '@shared/ui/user-picker.component';

function projectDetailFromRoute(route: ActivatedRoute): ProjectDetail {
  let r: ActivatedRoute | null = route;
  while (r) {
    const d = r.snapshot.data['projectDetail'];
    if (d) return d as ProjectDetail;
    r = r.parent;
  }
  throw new Error('projectDetail resolver missing');
}

type RoleOpt = { labelKey: string; value: ProjectRole };

@Component({
  selector: 'app-project-members-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ButtonModule, ConfirmDialogModule, UserPickerComponent],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="wrap">
      <h2>{{ 'project.members_title' | translate }}</h2>
      <p class="hint">{{ 'project.members_hint' | translate }}</p>

      <div class="add-row">
        <app-user-picker [(userId)]="addUserId" />
        <select class="role" [(ngModel)]="addRole">
          @for (r of roleOptions; track r.value) {
            <option [ngValue]="r.value">{{ r.labelKey | translate }}</option>
          }
        </select>
        <button pButton type="button" size="small"
                [disabled]="!canAdd() || saving()"
                [loading]="saving()"
                (click)="addMember()"
                [label]="'project.member_add' | translate"></button>
      </div>

      <div class="list">
        @for (m of membersSorted(); track m.userId) {
          <div class="row">
            <div class="id">
              <div class="uid">{{ m.userId.slice(0, 8) }}…</div>
              <div class="joined">{{ 'project.member_joined' | translate }}: {{ m.joinedAt | date:'short' }}</div>
            </div>

            <div class="role-col">
              <select class="role" [ngModel]="m.role" (ngModelChange)="changeRole(m, $event)">
                @for (r of roleOptions; track r.value) {
                  <option [ngValue]="r.value">{{ r.labelKey | translate }}</option>
                }
              </select>
            </div>

            <div class="actions">
              <button pButton type="button" size="small" [text]="true"
                      class="danger"
                      (click)="confirmRemove(m)"
                      [label]="'project.member_remove' | translate"></button>
            </div>
          </div>
        }
      </div>

      <p-confirmDialog />
    </section>
  `,
  styles: [`
    .wrap { max-width: 920px; }
    h2 { margin: 0 0 6px; font-size: 16px; font-weight: 650; }
    .hint { margin: 0 0 16px; font-size: 13px; color: var(--c-text-muted); line-height: 1.45; }
    .add-row {
      display: grid;
      grid-template-columns: 1fr 180px 140px;
      gap: 10px;
      align-items: end;
      padding: 12px;
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      margin-bottom: 14px;
    }
    .role {
      width: 100%;
      padding: 8px 10px;
      border-radius: var(--radius);
      border: 1px solid var(--c-border);
      background: var(--c-surface);
      color: var(--c-text);
      font-size: 13px;
    }
    .list { display: flex; flex-direction: column; gap: 8px; }
    .row {
      display: grid;
      grid-template-columns: 1fr 180px 140px;
      gap: 10px;
      align-items: center;
      padding: 12px;
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      background: var(--c-surface);
    }
    .uid { font-family: monospace; font-size: 13px; color: var(--c-text); }
    .joined { font-size: 11px; color: var(--c-text-muted); margin-top: 2px; }
    .actions { display: flex; justify-content: flex-end; }
    .danger { color: var(--c-accent-danger); }
    @media (max-width: 720px) {
      .add-row, .row { grid-template-columns: 1fr; }
      .actions { justify-content: flex-start; }
    }
  `]
})
export class ProjectMembersAdminPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProjectApiService);
  private readonly ctx = inject(WorkspaceContextService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly project = signal<ProjectDetail>(projectDetailFromRoute(this.route));
  readonly saving = signal(false);

  readonly addUserId = signal<string | null>(null);
  addRole: ProjectRole = 2;

  readonly membersSorted = computed(() => {
    const p = this.project();
    return [...p.members].sort((a, b) => (a.role - b.role) || a.userId.localeCompare(b.userId));
  });

  readonly roleOptions: RoleOpt[] = [
    { labelKey: 'project.role_admin', value: 1 },
    { labelKey: 'project.role_member', value: 2 },
    { labelKey: 'project.role_viewer', value: 3 }
  ];

  ngOnInit(): void {
    this.ctx.setProject(projectDetailToSummary(this.project()));
  }

  canAdd(): boolean {
    const uid = this.addUserId();
    if (!uid) return false;
    const exists = this.project().members.some((m) => m.userId === uid);
    return !exists;
  }

  private reload(): void {
    const key = this.project().key;
    this.api.getDetailForMemberByKey(key).subscribe({
      next: (d) => {
        this.project.set(d);
        this.ctx.setProject(projectDetailToSummary(d));
        this.cdr.markForCheck();
      }
    });
  }

  addMember(): void {
    const uid = this.addUserId();
    if (!uid) return;
    const p = this.project();
    this.saving.set(true);
    this.api.addMember(p.id, uid, this.addRole).subscribe({
      next: (d) => {
        this.project.set(d);
        this.ctx.setProject(projectDetailToSummary(d));
        this.addUserId.set(null);
        this.saving.set(false);
        this.cdr.markForCheck();
      },
      error: () => this.saving.set(false)
    });
  }

  changeRole(m: ProjectMember, role: ProjectRole): void {
    const p = this.project();
    if (m.role === role) return;
    this.api.changeMemberRole(p.id, m.userId, role).subscribe({
      next: (d) => {
        this.project.set(d);
        this.ctx.setProject(projectDetailToSummary(d));
        this.cdr.markForCheck();
      }
    });
  }

  confirmRemove(m: ProjectMember): void {
    this.translate
      .get([
        'project.member_remove_confirm_title',
        'project.member_remove_confirm_detail',
        'common.yes',
        'common.no'
      ])
      .subscribe((t) => {
        this.confirm.confirm({
          header: t['project.member_remove_confirm_title'],
          message: t['project.member_remove_confirm_detail'],
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: t['common.yes'],
          rejectLabel: t['common.no'],
          acceptButtonStyleClass: 'p-button-danger',
          accept: () => this.removeMember(m)
        });
      });
  }

  private removeMember(m: ProjectMember): void {
    const p = this.project();
    this.api.removeMember(p.id, m.userId).subscribe({
      next: (d) => {
        this.project.set(d);
        this.ctx.setProject(projectDetailToSummary(d));
        this.cdr.markForCheck();
      }
    });
  }
}

