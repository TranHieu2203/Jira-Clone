import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { WorkspaceApiService, WorkspaceDetail } from '@core/api/workspace.service';
import { ProjectApiService, ProjectSummary } from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { CreateProjectDialogComponent } from '@features/project/create-project.dialog';

@Component({
  selector: 'app-workspace-detail-page',
  standalone: true,
  imports: [
    CommonModule, RouterModule, TranslateModule, ButtonModule,
    AppPageHeaderComponent, CreateProjectDialogComponent
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

      <h3 class="section-title">{{ 'workspace.members' | translate }}</h3>
      <div class="members">
        @for (m of ws.members; track m.userId) {
          <div class="member">
            <span class="role" [class.owner]="m.role === 1">{{ roleName(m.role) }}</span>
            <code>{{ m.userId }}</code>
          </div>
        }
      </div>

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
}
