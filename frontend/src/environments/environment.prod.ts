import { AppConfig } from '../app/core/config/app-config';

export const environment: AppConfig & { production: boolean } = {
  production: true,
  apiBaseUrl: '/api',
  defaultLang: 'vi',
  supportedLangs: ['vi', 'en']
};
