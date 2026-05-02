import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AppTopbarComponent } from './app-topbar.component';
import { AppSidebarComponent } from './app-sidebar.component';
import { CreateIssueDialogComponent } from '@features/issue/create-issue.dialog';
import { Issue } from '@core/api/issue.service';
import { WorkspaceContextService } from './workspace-context.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    AppTopbarComponent, AppSidebarComponent, CreateIssueDialogComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell">
      <app-topbar (toggleSidebar)="onToggle()" (create)="openCreate()" />
      <div class="body">
        <app-sidebar [collapsed]="sidebarCollapsed()" />
        <main class="main">
          <router-outlet />
        </main>
      </div>
    </div>

    <app-create-issue-dialog
      [fixedProjectId]="contextProjectId()"
      [(visible)]="createIssueVisible"
      (created)="onIssueCreated($event)" />
  `,
  styles: [`
    .shell { display: flex; flex-direction: column; min-height: 100vh; }
    .body { flex: 1; display: flex; min-height: 0; }
    .main {
      flex: 1; padding: 24px; overflow-y: auto; min-width: 0;
      background: var(--c-bg);
    }
    @media (max-width: 767px) {
      .main { padding: 16px; }
    }
  `]
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly ctx = inject(WorkspaceContextService);

  readonly sidebarCollapsed = signal(false);
  readonly createIssueVisible = signal(false);

  /** Khi sidebar đã có project context (detail/board/issues…), dialog Create Issue pre-fill project. */
  readonly contextProjectId = computed(() => this.ctx.project()?.id ?? null);

  onToggle(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  openCreate(): void {
    this.createIssueVisible.set(true);
  }

  onIssueCreated(issue: Issue): void {
    this.router.navigate(['/issues', issue.key]);
  }
}
