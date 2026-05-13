import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import { registerSyncfusionLicense } from './app/features/form-management/syncfusion-license';

registerSyncfusionLicense();

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
