import { AppConfig } from '../app/core/config/app-config';

export const environment: AppConfig & { production: boolean } = {
  production: false,
  apiBaseUrl: 'http://localhost:5000/api',
  defaultLang: 'vi',
  supportedLangs: ['vi', 'en']
};
