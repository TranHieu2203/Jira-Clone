import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ToastModule } from 'primeng/toast';
import { ErrorDialogComponent } from './core/notification/error-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, ToastModule, TranslateModule, ErrorDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <router-outlet />
    <p-toast position="top-right">
      <ng-template let-message pTemplate="message">
        <div class="toast-row">
          <i [class]="iconFor(message.severity)"></i>
          <span class="toast-text">
            @if (message.data?.messageKey) {
              {{ message.data.messageKey | translate: (message.data.args ?? null) }}
            } @else {
              {{ message.summary }}
            }
          </span>
        </div>
      </ng-template>
    </p-toast>
    <app-error-dialog />
  `,
  styles: [`
    .toast-row { display: flex; align-items: center; gap: 8px; padding: 4px 0; }
    .toast-row i { font-size: 16px; }
    .toast-text { font-size: 13px; color: var(--c-text); }
  `]
})
export class AppComponent {
  iconFor(severity: string): string {
    switch (severity) {
      case 'success': return 'pi pi-check-circle';
      case 'info': return 'pi pi-info-circle';
      case 'warn': return 'pi pi-exclamation-triangle';
      case 'error': return 'pi pi-times-circle';
      default: return 'pi pi-info-circle';
    }
  }
}
