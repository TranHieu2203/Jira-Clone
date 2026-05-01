import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-error-dialog',
  standalone: true,
  imports: [CommonModule, TranslateModule, DialogModule, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [style]="{ width: '480px' }"
      [header]="'common.error' | translate">
      @if (errorPayload(); as err) {
        <div class="error-body">
          <div class="error-icon">⊘</div>
          <p class="error-msg">
            {{ (err.messageKey ?? 'system.unexpected') | translate }}
          </p>

          @if (err.errors && err.errors.length) {
            <ul class="error-list">
              @for (e of err.errors; track e.code) {
                <li>
                  <strong>{{ e.code }}</strong>:
                  {{ e.messageKey | translate: (asArgs(e.args)) }}
                  @if (e.field) { <em>({{ e.field }})</em> }
                </li>
              }
            </ul>
          }

          <div class="trace">
            <span class="trace-label">{{ 'common.trace_id' | translate }}:</span>
            <code class="trace-id">{{ err.traceId }}</code>
            <button class="trace-copy" type="button" (click)="copy(err.traceId)">
              {{ (copiedSig() ? 'common.copied' : 'common.copy') | translate }}
            </button>
          </div>
        </div>
      }
      <ng-template pTemplate="footer">
        <p-button [label]="'common.ok' | translate" (onClick)="close()" />
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .error-body { display: flex; flex-direction: column; gap: 12px; }
    .error-icon { font-size: 28px; color: var(--c-accent-danger); font-weight: 700; }
    .error-msg { margin: 0; font-size: 15px; color: var(--c-text); }
    .error-list { margin: 0; padding-left: 20px; color: var(--c-text-muted); font-size: 13px; }
    .trace {
      margin-top: 8px;
      padding: 10px 12px;
      background: var(--c-surface-2);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }
    .trace-label { color: var(--c-text-muted); font-size: 12px; }
    .trace-id {
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      font-size: 12px;
      color: var(--c-text);
      word-break: break-all;
    }
    .trace-copy {
      margin-left: auto;
      background: transparent;
      color: var(--c-text);
      border: 1px solid var(--c-border);
      border-radius: 4px;
      padding: 4px 10px;
      cursor: pointer;
      font-size: 12px;
    }
    .trace-copy:hover { background: var(--c-surface-3); }
  `]
})
export class ErrorDialogComponent {
  private readonly notif = inject(NotificationService);
  private readonly translate = inject(TranslateService);

  readonly errorPayload = this.notif.currentError;
  readonly visible = computed(() => this.errorPayload() !== null);
  readonly copiedSig = signal(false);

  asArgs(value: unknown): Record<string, unknown> | undefined {
    return (value && typeof value === 'object') ? (value as Record<string, unknown>) : undefined;
  }

  close(): void { this.notif.dismissError(); }
  onVisibleChange(v: boolean): void { if (!v) this.close(); }

  async copy(traceId: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(traceId);
      this.copiedSig.set(true);
      setTimeout(() => this.copiedSig.set(false), 2000);
    } catch { /* ignore clipboard failure */ }
  }
}
