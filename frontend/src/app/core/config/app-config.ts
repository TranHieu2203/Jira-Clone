import { InjectionToken } from '@angular/core';

export interface AppConfig {
  apiBaseUrl: string;
  defaultLang: 'vi' | 'en';
  supportedLangs: ('vi' | 'en')[];
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export const appConfigDefaults: AppConfig = {
  apiBaseUrl: '/api',
  defaultLang: 'vi',
  supportedLangs: ['vi', 'en']
};
