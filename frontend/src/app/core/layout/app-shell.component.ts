import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AppTopbarComponent } from './app-topbar.component';
import { AppSidebarComponent } from './app-sidebar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, AppTopbarComponent, AppSidebarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell">
      <app-topbar (toggleSidebar)="onToggle()" (create)="onCreate()" />
      <div class="body">
        <app-sidebar [collapsed]="sidebarCollapsed()" />
        <main class="main">
          <router-outlet />
        </main>
      </div>
    </div>
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
  readonly sidebarCollapsed = signal(false);

  onToggle(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  onCreate(): void {
    this.router.navigate(['/issues/new']);
  }
}
