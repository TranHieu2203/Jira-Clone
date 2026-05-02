import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'app.theme';

export type AppTheme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly themeSig = signal<AppTheme>('light');
  readonly theme = this.themeSig.asReadonly();

  /** Gọi từ APP_INITIALIZER — áp theme đã lưu hoặc prefers-color-scheme. */
  init(): void {
    const saved = localStorage.getItem(STORAGE_KEY) as AppTheme | null;
    let initial: AppTheme = 'light';
    if (saved === 'dark' || saved === 'light') {
      initial = saved;
    } else if (typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches) {
      initial = 'dark';
    }
    this.apply(initial);
  }

  toggle(): void {
    this.apply(this.themeSig() === 'dark' ? 'light' : 'dark');
  }

  use(next: AppTheme): void {
    this.apply(next);
  }

  isDark(): boolean {
    return this.themeSig() === 'dark';
  }

  private apply(next: AppTheme): void {
    this.themeSig.set(next);
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem(STORAGE_KEY, next);
  }
}
