import { ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AuthService } from '@core/auth/auth.service';
import { LanguageService } from '@core/i18n/language.service';
import { ThemeService } from '@core/theme/theme.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, MenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="topbar">
      <button class="icon-btn toggle" (click)="toggleSidebar.emit()" aria-label="Toggle sidebar">
        <span class="icon">☰</span>
      </button>
      <a routerLink="/" class="brand">Jira Clone</a>

      <div class="search">
        <span class="search-icon">⌕</span>
        <input
          type="text"
          [placeholder]="'common.search_placeholder' | translate"
          [value]="searchValue()"
          (input)="onSearch($event)"
          aria-label="Search" />
        <kbd>⌘K</kbd>
      </div>

      <div class="spacer"></div>

      <button class="icon-btn" (click)="onCreate()" aria-label="Create">
        <span class="plus">+</span>
        <span class="hide-sm">{{ 'common.create' | translate }}</span>
      </button>

      <button class="icon-btn" aria-label="Notifications">
        <span class="icon">🔔</span>
      </button>

      <button type="button" class="icon-btn theme-btn" (click)="toggleTheme()"
              [attr.aria-label]="'theme.toggle_aria' | translate">
        <span class="icon theme-icon">{{ theme.isDark() ? '☀' : '☾' }}</span>
      </button>

      <div class="lang-switch">
        <button [class.on]="lang.lang() === 'vi'" (click)="lang.use('vi')">VI</button>
        <button [class.on]="lang.lang() === 'en'" (click)="lang.use('en')">EN</button>
      </div>

      @if (auth.user()) {
        <p-menu #userMenu [model]="userMenuItems()" [popup]="true" appendTo="body" />
        <button class="profile" (click)="userMenu.toggle($event)" aria-label="Profile">
          <span class="avatar">{{ initials() }}</span>
        </button>
      }
    </header>
  `,
  styles: [`
    .topbar {
      display: flex; align-items: center; gap: 12px;
      padding: 0 16px; height: 48px;
      background: var(--c-surface);
      border-bottom: 1px solid var(--c-border);
      position: sticky; top: 0; z-index: 100;
    }
    .icon-btn {
      display: inline-flex; align-items: center; gap: 6px;
      background: transparent; border: 1px solid transparent;
      color: var(--c-text); padding: 6px 10px;
      border-radius: var(--radius); cursor: pointer;
      font-size: 13px; font-weight: 500;
    }
    .icon-btn:hover { background: var(--c-surface-2); border-color: var(--c-border); }
    .toggle .icon { font-size: 18px; }
    .brand { font-weight: 700; font-size: 14px; color: var(--c-text); padding: 0 4px; }
    .search {
      flex: 0 1 360px; display: flex; align-items: center; gap: 6px;
      background: var(--c-surface-2); border: 1px solid var(--c-border);
      padding: 4px 10px; border-radius: var(--radius);
      max-width: 360px;
    }
    .search input {
      flex: 1; border: none; background: transparent; outline: none;
      font-size: 13px; color: var(--c-text);
    }
    .search input::placeholder { color: var(--c-text-subtle); }
    .search-icon { color: var(--c-text-muted); font-size: 14px; }
    .search kbd {
      font-size: 10px; color: var(--c-text-muted);
      background: var(--c-surface); padding: 1px 6px; border-radius: 3px;
      border: 1px solid var(--c-border);
    }
    .spacer { flex: 1; }
    .plus { font-size: 16px; line-height: 1; }
    .hide-sm { display: none; }
    @media (min-width: 640px) { .hide-sm { display: inline; } }
    .lang-switch { display: flex; gap: 4px; }
    .lang-switch button {
      background: transparent; border: 1px solid var(--c-border);
      color: var(--c-text-muted); padding: 3px 8px; cursor: pointer;
      font-size: 11px; border-radius: 4px; font-weight: 500;
    }
    .lang-switch button.on { background: var(--c-text); color: var(--c-on-primary); border-color: var(--c-text); }
    .theme-btn .theme-icon { font-size: 15px; line-height: 1; opacity: 0.85; }
    .profile {
      background: transparent; border: none; cursor: pointer;
      width: 32px; height: 32px; padding: 0;
    }
    .avatar {
      display: inline-flex; align-items: center; justify-content: center;
      width: 28px; height: 28px; border-radius: 50%;
      background: var(--c-text); color: var(--c-on-primary);
      font-size: 11px; font-weight: 600;
    }
  `]
})
export class AppTopbarComponent {
  private readonly cdr = inject(ChangeDetectorRef);
  readonly auth = inject(AuthService);
  readonly lang = inject(LanguageService);
  readonly theme = inject(ThemeService);

  readonly toggleSidebar = output<void>();
  readonly create = output<void>();
  readonly search = output<string>();

  readonly searchValue = signal('');

  readonly initials = computed(() => {
    const name = this.auth.user()?.displayName ?? '';
    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  readonly userMenuItems = computed<MenuItem[]>(() => [
    { label: this.auth.user()?.displayName ?? '', disabled: true },
    { separator: true },
    { label: 'Profile', icon: 'pi pi-user', routerLink: '/profile' },
    { label: 'Settings', icon: 'pi pi-cog', routerLink: '/settings' },
    { separator: true },
    { label: 'Logout', icon: 'pi pi-sign-out', command: () => this.auth.logout() }
  ]);

  onSearch(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.searchValue.set(v);
    this.search.emit(v);
  }

  onCreate(): void {
    this.create.emit();
  }

  toggleTheme(): void {
    this.theme.toggle();
    this.cdr.markForCheck();
  }
}
