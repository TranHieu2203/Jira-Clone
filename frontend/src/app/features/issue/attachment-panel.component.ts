import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { AttachmentApiService, IssueAttachmentSummary } from '@core/api/attachment.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-attachment-panel',
  standalone: true,
  imports: [CommonModule, TranslateModule, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="att">
      <h3>{{ 'attachment.title' | translate }}</h3>
      <label class="upload">
        <input type="file" class="sr-only" (change)="onPick($event)" [disabled]="uploading()" />
        <button pButton type="button" size="small" [loading]="uploading()"
                [label]="'attachment.upload' | translate"></button>
      </label>
      @if (loading()) {
        <div class="muted">{{ 'common.loading' | translate }}</div>
      } @else if (items().length === 0) {
        <div class="muted">{{ 'attachment.empty' | translate }}</div>
      } @else {
        <ul class="list">
          @for (a of items(); track a.id) {
            <li class="row">
              <span class="name" [title]="a.fileName">{{ a.fileName }}</span>
              <span class="meta">{{ formatSize(a.sizeBytes) }}</span>
              <button pButton type="button" size="small" class="p-button-text"
                      [label]="'attachment.download' | translate"
                      (click)="download(a)"></button>
              @if (canDelete(a)) {
                <button pButton type="button" size="small" class="p-button-text danger"
                        [label]="'common.delete' | translate"
                        [loading]="deletingId() === a.id"
                        (click)="remove(a)"></button>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [`
    .att { margin-bottom: 24px; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 8px;
    }
    .upload { display: inline-block; margin-bottom: 12px; }
    .sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); border: 0; }
    .muted { font-size: 13px; color: var(--c-text-muted); }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
    .row {
      display: flex; flex-wrap: wrap; align-items: center; gap: 8px;
      padding: 8px 10px; border: 1px solid var(--c-border); border-radius: var(--radius);
      background: var(--c-surface);
    }
    .name { flex: 1; min-width: 120px; font-size: 13px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .meta { font-size: 11px; color: var(--c-text-muted); font-family: monospace; }
    ::ng-deep .danger.p-button { color: var(--c-accent-danger); }
  `]
})
export class AttachmentPanelComponent {
  private readonly api = inject(AttachmentApiService);
  private readonly auth = inject(AuthService);

  readonly issueId = input.required<string>();

  readonly items = signal<IssueAttachmentSummary[]>([]);
  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly deletingId = signal<string | null>(null);

  constructor() {
    effect(() => {
      const id = this.issueId();
      if (id) this.loadList(id);
    });
  }

  private loadList(issueId: string): void {
    this.loading.set(true);
    this.api.listByIssue(issueId).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  canDelete(a: IssueAttachmentSummary): boolean {
    const uid = this.auth.user()?.id;
    return !!uid && uid === a.uploadedByUserId;
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  onPick(ev: Event): void {
    const inputEl = ev.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    if (!file) return;
    const id = this.issueId();
    this.uploading.set(true);
    this.api.upload(id, file).subscribe({
      next: () => {
        this.uploading.set(false);
        this.loadList(id);
      },
      error: () => this.uploading.set(false)
    });
  }

  download(a: IssueAttachmentSummary): void {
    const id = this.issueId();
    this.api.downloadBlob(id, a.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = a.fileName;
        anchor.click();
        URL.revokeObjectURL(url);
      }
    });
  }

  remove(a: IssueAttachmentSummary): void {
    const id = this.issueId();
    this.deletingId.set(a.id);
    this.api.delete(id, a.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.loadList(id);
      },
      error: () => this.deletingId.set(null)
    });
  }
}
