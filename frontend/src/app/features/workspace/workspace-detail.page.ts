import { ChangeDetectionStrategy, Component, OnInit, inject, model, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { WorkspaceApiService, WorkspaceDetail, WorkspaceRole } from '@core/api/workspace.service';
import { ProjectApiService, ProjectSummary } from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { CreateProjectDialogComponent } from '@features/project/create-project.dialog';
import { UserPickerComponent } from '@shared/ui/user-picker.component';

@Component({
  selector: 'app-workspace-detail-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule, ButtonModule, DialogModule,
    AppPageHeaderComponent, CreateProjectDialogComponent, UserPickerComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (workspace(); as ws) {
      <app-page-header [title]="ws.name">
        <button pButton (click)="dialogVisible.set(true)" [label]="'project.create' | translate"></button>
      </app-page-header>

      <section class="meta">
        <div><strong>{{ '@' }}{{ ws.slug }}</strong></div>
        @if (ws.description) { <div class="desc">{{ ws.description }}</div> }
      </section>

      <h3 class="section-title">{{ 'project.title' | translate }}</h3>
      @if (loadingProjects()) {
        <div class="empty">{{ 'common.loading' | translate }}</div>
      } @else if (projects().length === 0) {
        <div class="empty">{{ 'project.empty' | translate }}</div>
      } @else {
        <div class="grid">
          @for (p of projects(); track p.id) {
            <a class="proj" [routerLink]="['/projects', p.key]">
              <div class="key">{{ p.key }}</div>
              <div class="name">{{ p.name }}</div>
              <div class="type">{{ p.type === 1 ? 'Scrum' : 'Kanban' }}</div>
            </a>
          }
        </div>
      }

      <div class="members-head">
        <h3 class="section-title inline">{{ 'workspace.members' | translate }}</h3>
        <button pButton type="button" size="small"
                (click)="addMemberOpen.set(true)"
                [label]="'workspace.add_member' | translate"></button>
      </div>
      <div class="members">
        @for (m of ws.members; track m.userId) {
          <div class="member">
            <span class="role" [class.owner]="m.role === 1">{{ roleName(m.role) }}</span>
            <code>{{ m.userId }}</code>
          </div>
        }
      </div>

      <p-dialog [visible]="addMemberOpen()"
                (visibleChange)="addMemberOpen.set($event)"
                [modal]="true"
                [style]="{ width: '420px' }"
                [header]="'workspace.add_member' | translate"
                (onHide)="resetAddMember()">
        <div class="dlg-field">
          <label>{{ 'workspace.member_pick_user' | translate }}</label>
          <app-user-picker [(userId)]="newMemberUserId" />
        </div>
        <div class="dlg-field">
          <label>{{ 'workspace.member_role' | translate }}</label>
          <select [(ngModel)]="newMemberRole" class="role-native">
            <option [ngValue]="3">{{ 'workspace.role_member' | translate }}</option>
            <option [ngValue]="2">{{ 'workspace.role_admin' | translate }}</option>
          </select>
        </div>
        <div class="dlg-actions">
          <button pButton type="button" class="p-button-text"
                  (click)="addMemberOpen.set(false)"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="button"
                  [loading]="addingMember()"
                  [disabled]="!newMemberUserId()"
                  (click)="submitAddMember()"
                  [label]="'workspace.add_member' | translate"></button>
        </div>
      </p-dialog>

      <app-create-project-dialog
        [workspaceId]="ws.id"
        [(visible)]="dialogVisible"
        (created)="onProjectCreated()" />
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .meta { color: var(--c-text-muted); margin-bottom: 16px; }
    .meta .desc { margin-top: 4px; }
    .section-title { font-size: 14px; font-weight: 600; margin: 24px 0 12px; color: var(--c-text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
    .section-title.inline { margin: 0; }
    .members-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin: 24px 0 12px; flex-wrap: wrap; }
    .dlg-field { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
    .dlg-field label { font-size: 11px; text-transform: uppercase; color: var(--c-text-muted); letter-spacing: 0.5px; }
    .role-native {
      padding: 8px 10px; border-radius: var(--radius); border: 1px solid var(--c-border);
      background: var(--c-surface); color: var(--c-text); font-size: 14px;
    }
    .dlg-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
    .proj {
      display: flex; flex-direction: column; gap: 4px; padding: 12px;
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); text-decoration: none; color: var(--c-text);
    }
    .proj:hover { border-color: var(--c-border-strong); text-decoration: none; }
    .proj .key { font-size: 11px; color: var(--c-text-muted); font-family: monospace; }
    .proj .name { font-weight: 600; }
    .proj .type { font-size: 11px; color: var(--c-text-subtle); }
    .members { display: flex; flex-direction: column; gap: 4px; }
    .member { display: flex; align-items: center; gap: 12px; padding: 6px 0; }
    .role {
      font-size: 10px; padding: 2px 8px; border-radius: 10px;
      background: var(--c-surface-3); color: var(--c-text-muted);
      text-transform: uppercase; font-weight: 600;
    }
    .role.owner { background: var(--c-text); color: var(--c-on-primary); }
    .empty { padding: 24px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class WorkspaceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly wsApi = inject(WorkspaceApiService);
  private readonly projApi = inject(ProjectApiService);
  private readonly ctx = inject(WorkspaceContextService);

  readonly workspace = signal<WorkspaceDetail | null>(null);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly loadingProjects = signal(false);
  readonly dialogVisible = signal(false);

  readonly addMemberOpen = signal(false);
  readonly newMemberUserId = model<string | null>(null);
  readonly newMemberRole = model<WorkspaceRole>(3);
  readonly addingMember = signal(false);

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) return;
    this.wsApi.getBySlug(slug).subscribe((ws) => {
      this.workspace.set(ws);
      this.ctx.setWorkspace(ws);
      this.loadProjects(ws.id);
    });
  }

  private loadProjects(workspaceId: string): void {
    this.loadingProjects.set(true);
    this.projApi.listByWorkspace(workspaceId).subscribe({
      next: (list) => { this.projects.set(list); this.loadingProjects.set(false); },
      error: () => this.loadingProjects.set(false)
    });
  }

  onProjectCreated(): void {
    const ws = this.workspace();
    if (ws) this.loadProjects(ws.id);
  }

  roleName(role: number): string {
    return role === 1 ? 'Owner' : role === 2 ? 'Admin' : 'Member';
  }

  resetAddMember(): void {
    this.newMemberUserId.set(null);
    this.newMemberRole.set(3);
  }

  submitAddMember(): void {
    const ws = this.workspace();
    const uid = this.newMemberUserId();
    if (!ws || !uid) return;
    this.addingMember.set(true);
    this.wsApi.addMember(ws.id, uid, this.newMemberRole()).subscribe({
      next: (d) => {
        this.workspace.set(d);
        this.ctx.setWorkspace(d);
        this.addingMember.set(false);
        this.addMemberOpen.set(false);
        this.resetAddMember();
      },
      error: () => this.addingMember.set(false)
    });
  }
}
