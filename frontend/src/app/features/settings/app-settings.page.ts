import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { LanguageService } from '@core/i18n/language.service';
import { ThemeService } from '@core/theme/theme.service';
import { EmailPreferenceApiService, EmailPreferenceDto } from '@core/api/email-preference-api.service';

@Component({
  selector: 'app-app-settings-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule, CheckboxModule, ButtonModule, AppPageHeaderComponent],
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

    <!-- R6: per-user email opt-out preferences. -->
    <section class="block">
      <h3>{{ 'app_settings.email_title' | translate }}</h3>
      <p class="email-intro">{{ 'app_settings.email_intro' | translate }}</p>

      @if (loadingPrefs()) {
        <p class="hint">{{ 'common.loading' | translate }}</p>
      } @else if (draftPrefs) {
        <div class="prefs">
          <label class="row">
            <p-checkbox [(ngModel)]="draftPrefs.noAssignee" name="noAssignee" [binary]="true" inputId="noAssignee" />
            <span class="lbl"><strong>{{ 'app_settings.email_no_assignee' | translate }}</strong></span>
          </label>
          <label class="row">
            <p-checkbox [(ngModel)]="draftPrefs.noStatus" name="noStatus" [binary]="true" inputId="noStatus" />
            <span class="lbl"><strong>{{ 'app_settings.email_no_status' | translate }}</strong></span>
          </label>
          <label class="row">
            <p-checkbox [(ngModel)]="draftPrefs.noComment" name="noComment" [binary]="true" inputId="noComment" />
            <span class="lbl"><strong>{{ 'app_settings.email_no_comment' | translate }}</strong></span>
          </label>
          <label class="row">
            <p-checkbox [(ngModel)]="draftPrefs.noMention" name="noMention" [binary]="true" inputId="noMention" />
            <span class="lbl"><strong>{{ 'app_settings.email_no_mention' | translate }}</strong></span>
          </label>
        </div>
        <button pButton size="small" [loading]="savingPrefs()" (click)="saveEmailPrefs()"
                [label]="'common.save' | translate"></button>
      }
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
    .email-intro { font-size: 13px; color: var(--c-text-muted); max-width: 640px; line-height: 1.45; margin: 0 0 12px; }
    .prefs { display: flex; flex-direction: column; gap: 8px; margin-bottom: 12px; max-width: 480px; }
    .row { display: flex; align-items: center; gap: 8px; cursor: pointer; }
    .lbl { font-size: 13px; }
    .back { margin-top: 24px; font-size: 13px; }
    .back a { color: var(--c-primary); }
  `]
})
export class AppSettingsPageComponent implements OnInit {
  readonly lang = inject(LanguageService);
  readonly theme = inject(ThemeService);
  private readonly emailApi = inject(EmailPreferenceApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  // R6: email preferences. Dùng plain field cho ngModel two-way (signal alias không
  // mutate được trong @if scope).
  readonly loadingPrefs = signal(false);
  readonly savingPrefs = signal(false);
  /** Mutable copy cho ngModel — sync về BE khi user bấm Save. */
  draftPrefs: EmailPreferenceDto | null = null;

  ngOnInit(): void {
    this.loadPrefs();
  }

  setLang(code: 'vi' | 'en'): void {
    this.lang.use(code);
    this.cdr.markForCheck();
  }

  private loadPrefs(): void {
    this.loadingPrefs.set(true);
    this.emailApi.getMine().subscribe({
      next: (p) => {
        this.draftPrefs = p;
        this.loadingPrefs.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingPrefs.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  saveEmailPrefs(): void {
    const p = this.draftPrefs;
    if (!p) return;
    this.savingPrefs.set(true);
    this.emailApi.updateMine({
      noAssignee: p.noAssignee,
      noStatus: p.noStatus,
      noComment: p.noComment,
      noMention: p.noMention,
    }).subscribe({
      next: (r) => {
        this.draftPrefs = r;
        this.savingPrefs.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.savingPrefs.set(false);
        this.cdr.markForCheck();
      },
    });
  }
}
