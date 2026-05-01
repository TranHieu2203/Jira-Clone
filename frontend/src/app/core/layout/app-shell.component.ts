import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../auth/auth.service';
import { LanguageService } from '../i18n/language.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell">
      <header class="topbar">
        <a routerLink="/" class="brand">Jira Clone</a>
        <nav>
          <a routerLink="/products" routerLinkActive="active">{{ 'product.title' | translate }}</a>
        </nav>
        <div class="spacer"></div>
        <div class="lang-switch">
          <button [class.on]="lang.lang() === 'vi'" (click)="lang.use('vi')">VI</button>
          <button [class.on]="lang.lang() === 'en'" (click)="lang.use('en')">EN</button>
        </div>
        @if (auth.user()) {
          <span class="user">{{ auth.user()?.displayName }}</span>
          <button class="logout" (click)="auth.logout()">{{ 'common.logout' | translate }}</button>
        }
      </header>
      <main class="main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .shell { display: flex; flex-direction: column; min-height: 100vh; }
    .topbar {
      display: flex; align-items: center; gap: 16px;
      padding: 0 24px; height: 56px;
      background: var(--c-surface);
      border-bottom: 1px solid var(--c-border);
    }
    .brand { font-weight: 700; font-size: 16px; color: var(--c-text); }
    nav a {
      padding: 8px 12px; border-radius: var(--radius); font-weight: 500;
      color: var(--c-text-muted); text-decoration: none;
    }
    nav a.active, nav a:hover { color: var(--c-text); background: var(--c-surface-2); text-decoration: none; }
    .spacer { flex: 1; }
    .lang-switch button {
      background: transparent; border: 1px solid var(--c-border);
      color: var(--c-text-muted); padding: 4px 10px; cursor: pointer;
      font-size: 12px; border-radius: 4px;
    }
    .lang-switch button.on { background: var(--c-text); color: var(--c-on-primary); border-color: var(--c-text); }
    .user { font-size: 13px; color: var(--c-text-muted); }
    .logout {
      background: transparent; color: var(--c-text); border: 1px solid var(--c-border);
      padding: 6px 12px; border-radius: var(--radius); cursor: pointer; font-size: 13px;
    }
    .main { flex: 1; padding: 24px; max-width: 1280px; width: 100%; margin: 0 auto; }
  `]
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  readonly lang = inject(LanguageService);
}
