import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  computed,
  inject,
  OnInit,
  output,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';
import { LanguageService } from '@core/i18n/language.service';
import { ThemeService } from '@core/theme/theme.service';
import {
  InAppNotificationsApiService,
  InAppNotificationRow
} from '@core/api/in-app-notifications-api.service';

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

      <div class="notif-wrap">
        <button type="button" class="icon-btn notif-btn" (click)="toggleNotif()" aria-label="Notifications">
          @if (unread() > 0) {
            <span class="notif-badge">{{ unread() > 99 ? '99+' : unread() }}</span>
          }
          <span class="icon">🔔</span>
        </button>
        @if (notifOpen()) {
          <div class="notif-panel" (click)="$event.stopPropagation()" role="dialog" aria-label="Notifications panel">
            <div class="notif-head">
              <span>{{ 'notifications.title' | translate }}</span>
              <button type="button" class="linkish" (click)="markAllRead()">
                {{ 'notifications.mark_all' | translate }}
              </button>
            </div>
            @if (notifLoading()) {
              <div class="notif-muted">{{ 'notifications.loading' | translate }}</div>
            } @else if (notifItems().length === 0) {
              <div class="notif-muted">{{ 'notifications.empty' | translate }}</div>
            } @else {
              <ul class="notif-list">
                @for (n of notifItems(); track n.id) {
                  <li>
                    <button type="button" class="notif-row" [class.unread]="!n.isRead" (click)="openNotification(n)">
                      <span class="notif-type">{{ notificationLabelKey(n.type) | translate }}</span>
                      <span class="notif-line">{{ notificationPreview(n) }}</span>
                      <span class="notif-time">{{ n.createdAt | date: 'short' }}</span>
                    </button>
                  </li>
                }
              </ul>
            }
          </div>
        }
      </div>

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
    .notif-wrap { position: relative; }
    .notif-btn { position: relative; }
    .notif-badge {
      position: absolute; top: -2px; right: -2px; min-width: 16px; height: 16px; padding: 0 4px;
      border-radius: 8px; background: var(--c-accent-danger); color: white;
      font-size: 10px; font-weight: 700; line-height: 16px; text-align: center;
    }
    .notif-panel {
      position: absolute; right: 0; top: calc(100% + 6px); width: min(360px, 92vw);
      background: var(--c-surface); border: 1px solid var(--c-border); border-radius: var(--radius);
      box-shadow: 0 8px 24px rgba(0,0,0,0.12); z-index: 200; max-height: 70vh; overflow: auto;
    }
    .notif-head {
      display: flex; justify-content: space-between; align-items: center;
      padding: 10px 12px; border-bottom: 1px solid var(--c-border); font-size: 13px; font-weight: 600;
    }
    .linkish {
      background: none; border: none; cursor: pointer; color: var(--c-primary);
      font-size: 12px; text-decoration: underline; padding: 0;
    }
    .notif-muted { padding: 16px; color: var(--c-text-muted); font-size: 13px; }
    .notif-list { list-style: none; margin: 0; padding: 0; }
    .notif-row {
      width: 100%; text-align: left; display: grid; gap: 4px;
      padding: 10px 12px; border: none; border-bottom: 1px solid var(--c-border);
      background: transparent; cursor: pointer; color: var(--c-text);
    }
    .notif-row:hover { background: var(--c-surface-2); }
    .notif-row.unread { background: var(--c-surface-3); }
    .notif-type { font-size: 11px; font-weight: 600; color: var(--c-text-muted); text-transform: uppercase; }
    .notif-line { font-size: 13px; }
    .notif-time { font-size: 11px; color: var(--c-text-muted); }
  `]
})
export class AppTopbarComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly lang = inject(LanguageService);
  readonly theme = inject(ThemeService);
  private readonly notifApi = inject(InAppNotificationsApiService);

  readonly toggleSidebar = output<void>();
  readonly create = output<void>();
  readonly search = output<string>();

  readonly searchValue = signal('');
  readonly unread = signal(0);
  readonly notifOpen = signal(false);
  readonly notifItems = signal<InAppNotificationRow[]>([]);
  readonly notifLoading = signal(false);

  ngOnInit(): void {
    this.refreshUnread();
    interval(45_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.auth.isAuthenticated()) {
          this.refreshUnread();
        }
      });
  }

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

  toggleNotif(): void {
    if (!this.auth.isAuthenticated()) {
      return;
    }
    this.notifOpen.update((v) => !v);
    if (this.notifOpen()) {
      this.loadNotifs();
    }
    this.cdr.markForCheck();
  }

  notificationLabelKey(type: string): string {
    switch (type) {
      case 'assignee_changed':
        return 'notifications.type.assignee_changed';
      case 'status_changed':
        return 'notifications.type.status_changed';
      case 'comment_added':
        return 'notifications.type.comment_added';
      default:
        return 'notifications.type.generic';
    }
  }

  notificationPreview(n: InAppNotificationRow): string {
    const key = n.payload['issueKey'];
    const k = typeof key === 'string' ? key : '';
    if (n.type === 'comment_added') {
      const prev = n.payload['preview'];
      const p = typeof prev === 'string' ? prev : '';
      return k ? `${k} — ${p}` : p;
    }
    return k || '—';
  }

  openNotification(n: InAppNotificationRow): void {
    const key = n.payload['issueKey'];
    if (typeof key === 'string') {
      void this.router.navigate(['/issues', key]);
    }
    if (!n.isRead) {
      this.notifApi.markRead(n.id).subscribe({
        next: () => {
          this.refreshUnread();
          this.loadNotifs();
        }
      });
    }
    this.notifOpen.set(false);
    this.cdr.markForCheck();
  }

  markAllRead(): void {
    this.notifApi.markAllRead().subscribe({
      next: () => {
        this.refreshUnread();
        this.loadNotifs();
      }
    });
  }

  private refreshUnread(): void {
    if (!this.auth.isAuthenticated()) {
      this.unread.set(0);
      return;
    }
    this.notifApi.unreadCount().subscribe({
      next: (n) => {
        this.unread.set(n);
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  private loadNotifs(): void {
    this.notifLoading.set(true);
    this.notifApi.list(1, 25, false).subscribe({
      next: (p) => {
        this.notifItems.set(p.items);
        this.notifLoading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifLoading.set(false);
        this.cdr.markForCheck();
      }
    });
  }
}
