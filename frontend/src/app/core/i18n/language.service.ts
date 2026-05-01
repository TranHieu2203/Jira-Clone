import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

const STORAGE_KEY = 'app.lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly langSignal = signal<'vi' | 'en'>('vi');
  readonly lang = this.langSignal.asReadonly();

  getLang(): 'vi' | 'en' {
    return this.langSignal();
  }

  init(defaultLang: 'vi' | 'en', supported: ('vi' | 'en')[]): void {
    this.translate.addLangs(supported);
    this.translate.setDefaultLang(defaultLang);
    const saved = (localStorage.getItem(STORAGE_KEY) as 'vi' | 'en' | null) ?? defaultLang;
    this.use(saved);
  }

  use(lang: 'vi' | 'en'): void {
    this.translate.use(lang);
    this.langSignal.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    document.documentElement.lang = lang;
  }
}
