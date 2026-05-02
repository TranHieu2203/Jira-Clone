import { APP_INITIALIZER, ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

import { routes } from './app.routes';
import { APP_CONFIG } from './core/config/app-config';
import { environment } from '../environments/environment';
import { LanguageService } from './core/i18n/language.service';
import { ThemeService } from './core/theme/theme.service';
import {
  apiResponseInterceptor,
  authInterceptor,
  errorInterceptor,
  traceIdInterceptor
} from './core/http/interceptors';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, 'assets/i18n/', '.json');
}

export function initLanguageFactory(lang: LanguageService): () => void {
  return () => lang.init(environment.defaultLang, environment.supportedLangs);
}

export function initThemeFactory(theme: ThemeService): () => void {
  return () => {
    theme.init();
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([
      traceIdInterceptor,
      authInterceptor,
      apiResponseInterceptor,
      errorInterceptor
    ])),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: '[data-theme="dark"]' }
      }
    }),
    importProvidersFrom(TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      },
      defaultLanguage: 'vi'
    })),
    MessageService,
    { provide: APP_CONFIG, useValue: environment },
    {
      provide: APP_INITIALIZER,
      useFactory: initLanguageFactory,
      deps: [LanguageService],
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initThemeFactory,
      deps: [ThemeService],
      multi: true
    }
  ]
};
