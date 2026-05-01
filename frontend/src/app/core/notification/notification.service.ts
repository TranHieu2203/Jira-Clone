import { Injectable, signal } from '@angular/core';
import { ApiError } from '@shared/models/api-response';
import { MessageService } from 'primeng/api';

export interface ErrorPayload {
  messageKey: string | null;
  errors?: ApiError[] | null;
  traceId: string;
  status?: number;
}

// Carried in MessageService.add as `data` so the toast template can translate it via the pipe.
export interface ToastData {
  messageKey: string;
  args?: Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly errorState = signal<ErrorPayload | null>(null);
  readonly currentError = this.errorState.asReadonly();

  constructor(private readonly toast: MessageService) {}

  success(messageKey: string, args?: Record<string, unknown>): void {
    this.toast.add({
      severity: 'success',
      data: { messageKey, args } satisfies ToastData,
      life: 3500
    });
  }

  info(messageKey: string, args?: Record<string, unknown>): void {
    this.toast.add({
      severity: 'info',
      data: { messageKey, args } satisfies ToastData,
      life: 3000
    });
  }

  error(payload: ErrorPayload): void {
    this.errorState.set(payload);
  }

  dismissError(): void {
    this.errorState.set(null);
  }
}
