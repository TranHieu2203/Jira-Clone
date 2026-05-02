import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'profile.title' | translate"></app-page-header>

    @if (auth.user(); as u) {
      <dl class="grid">
        <dt>{{ 'profile.display_name' | translate }}</dt>
        <dd>{{ u.displayName }}</dd>
        <dt>{{ 'profile.user_name' | translate }}</dt>
        <dd>{{ u.userName }}</dd>
        @if (u.email) {
          <dt>{{ 'profile.email' | translate }}</dt>
          <dd>{{ u.email }}</dd>
        }
        <dt>{{ 'profile.roles' | translate }}</dt>
        <dd>{{ roleLabels(u.roles) }}</dd>
      </dl>
      <p class="hint">{{ 'profile.hint' | translate }}</p>
    } @else {
      <p class="muted">{{ 'common.loading' | translate }}</p>
    }

    <p class="back"><a routerLink="/workspaces">{{ 'nav.workspaces' | translate }}</a></p>
  `,
  styles: [`
    .grid {
      display: grid;
      grid-template-columns: 160px 1fr;
      gap: 10px 20px;
      max-width: 520px;
      font-size: 14px;
    }
    dt { color: var(--c-text-muted); font-weight: 500; }
    dd { margin: 0; }
    .hint, .muted { font-size: 13px; color: var(--c-text-muted); margin-top: 20px; max-width: 560px; line-height: 1.45; }
    .back { margin-top: 24px; font-size: 13px; }
    .back a { color: var(--c-primary); }
  `]
})
export class ProfilePageComponent {
  readonly auth = inject(AuthService);

  roleLabels(roles: string[]): string {
    return roles?.length ? roles.join(', ') : '—';
  }
}
