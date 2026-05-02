import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { LanguageService } from '@core/i18n/language.service';
import { ThemeService } from '@core/theme/theme.service';

@Component({
  selector: 'app-app-settings-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'app_settings.title' | translate"></app-page-header>

    <p class="intro">{{ 'app_settings.intro' | translate }}</p>

    <section class="block">
      <h3>{{ 'app_settings.language' | translate }}</h3>
      <div class="lang-switch">
        <button type="button" [class.on]="lang.lang() === 'vi'" (click)="setLang('vi')">VI</button>
        <button type="button" [class.on]="lang.lang() === 'en'" (click)="setLang('en')">EN</button>
      </div>
    </section>

    <section class="block">
      <h3>{{ 'app_settings.theme' | translate }}</h3>
      <button type="button" class="theme-btn" (click)="theme.toggle()">
        {{ theme.isDark() ? ('app_settings.theme_light' | translate) : ('app_settings.theme_dark' | translate) }}
      </button>
    </section>

    <p class="hint">{{ 'app_settings.project_hint' | translate }}</p>

    <p class="back"><a routerLink="/workspaces">{{ 'nav.workspaces' | translate }}</a></p>
  `,
  styles: [`
    .intro { font-size: 14px; color: var(--c-text-muted); max-width: 640px; line-height: 1.45; margin-bottom: 24px; }
    .block { margin-bottom: 28px; }
    .block h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 10px;
    }
    .lang-switch { display: flex; gap: 8px; }
    .lang-switch button {
      background: transparent; border: 1px solid var(--c-border);
      color: var(--c-text-muted); padding: 8px 16px; cursor: pointer;
      font-size: 13px; border-radius: var(--radius); font-weight: 500;
    }
    .lang-switch button.on { background: var(--c-text); color: var(--c-on-primary); border-color: var(--c-text); }
    .theme-btn {
      background: var(--c-surface); border: 1px solid var(--c-border);
      color: var(--c-text); padding: 8px 16px; cursor: pointer;
      font-size: 13px; border-radius: var(--radius); font-weight: 500;
    }
    .theme-btn:hover { background: var(--c-surface-2); }
    .hint { font-size: 13px; color: var(--c-text-muted); max-width: 560px; line-height: 1.45; }
    .back { margin-top: 24px; font-size: 13px; }
    .back a { color: var(--c-primary); }
  `]
})
export class AppSettingsPageComponent {
  readonly lang = inject(LanguageService);
  readonly theme = inject(ThemeService);
  private readonly cdr = inject(ChangeDetectorRef);

  setLang(code: 'vi' | 'en'): void {
    this.lang.use(code);
    this.cdr.markForCheck();
  }
}
