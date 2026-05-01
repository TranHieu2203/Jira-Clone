import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CardModule } from 'primeng/card';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ButtonModule, InputTextModule, PasswordModule, CardModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wrap">
      <p-card [header]="'auth.title' | translate" styleClass="card">
        <form (ngSubmit)="submit()" class="form">
          <label class="field">
            <span>{{ 'auth.username' | translate }}</span>
            <input pInputText [(ngModel)]="userName" name="userName" autocomplete="username" required />
          </label>
          <label class="field">
            <span>{{ 'auth.password' | translate }}</span>
            <input pInputText type="password" [(ngModel)]="password" name="password" autocomplete="current-password" required />
          </label>
          <p-button type="submit" [loading]="loading()" [label]="'auth.login' | translate" styleClass="full" />
        </form>
      </p-card>
    </div>
  `,
  styles: [`
    .wrap { display: flex; align-items: center; justify-content: center; min-height: 100vh; background: var(--c-surface-2); }
    :host ::ng-deep .card { width: 360px; }
    .form { display: flex; flex-direction: column; gap: 14px; }
    .field { display: flex; flex-direction: column; gap: 6px; font-size: 13px; color: var(--c-text-muted); }
    .field input { width: 100%; }
    :host ::ng-deep .full .p-button { width: 100%; }
  `]
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  userName = 'admin';
  password = 'Admin@123';
  readonly loading = signal(false);

  submit(): void {
    this.loading.set(true);
    this.auth.login(this.userName, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/products']);
      },
      error: () => this.loading.set(false)
    });
  }
}
