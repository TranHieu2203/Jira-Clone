import { ChangeDetectionStrategy, Component, OnChanges, SimpleChanges, computed, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { Comment, CommentApiService } from '@core/api/comment.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-comments-thread',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ButtonModule, TextareaModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="thread">
      <h3>{{ 'comment.title' | translate }} <span class="count">{{ comments().length }}</span></h3>

      @if (loading()) {
        <div class="empty">{{ 'common.loading' | translate }}</div>
      } @else if (comments().length === 0) {
        <div class="empty">{{ 'comment.empty' | translate }}</div>
      }

      <div class="list">
        @for (c of comments(); track c.id) {
          <article class="comment" [class.editing]="editingId() === c.id">
            <div class="head">
              <div class="avatar">{{ initials(c.authorId) }}</div>
              <div class="meta">
                <span class="author">{{ c.authorId === currentUserId() ? ('comment.you' | translate) : authorLabel(c.authorId) }}</span>
                <time>{{ c.createdAt | date:'short' }}</time>
                @if (c.isEdited) { <span class="edited">({{ 'comment.edited' | translate }})</span> }
              </div>
              @if (c.authorId === currentUserId() && editingId() !== c.id) {
                <div class="actions">
                  <button (click)="startEdit(c)" class="link" type="button">{{ 'common.edit' | translate }}</button>
                  <button (click)="requestDelete(c)" class="link danger" type="button">{{ 'common.delete' | translate }}</button>
                </div>
              }
            </div>

            @if (editingId() === c.id) {
              <textarea pTextarea rows="3" [(ngModel)]="editBody" name="editBody-{{c.id}}"></textarea>
              <div class="form-actions">
                <button pButton type="button" [text]="true" size="small"
                        (click)="cancelEdit()"
                        [label]="'common.cancel' | translate"></button>
                <button pButton type="button" size="small"
                        [loading]="saving()"
                        (click)="saveEdit(c)"
                        [label]="'common.save' | translate"></button>
              </div>
            } @else {
              <div class="body">{{ c.body }}</div>
              @if (c.mentions.length > 0) {
                <div class="mentions">
                  @for (m of c.mentions; track m) {
                    <span class="mention">{{ '@' }}{{ m }}</span>
                  }
                </div>
              }
            }
          </article>
        }
      </div>

      <div class="composer">
        <div class="avatar self">{{ initials(currentUserId() ?? '?') }}</div>
        <div class="input">
          <textarea pTextarea rows="2" [(ngModel)]="newBody" name="newBody"
                    [placeholder]="'comment.placeholder' | translate"></textarea>
          <div class="form-actions">
            <button pButton type="button" size="small"
                    [loading]="saving()"
                    [disabled]="!canSubmit() || saving()"
                    (click)="submit()"
                    [label]="'comment.add' | translate"></button>
          </div>
        </div>
      </div>

      <p-confirmDialog />
    </section>
  `,
  styles: [`
    .thread { margin-top: 24px; }
    h3 {
      font-size: 12px; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.5px; color: var(--c-text-muted); margin: 0 0 12px;
    }
    .count {
      font-size: 11px; padding: 1px 6px; border-radius: 10px;
      background: var(--c-surface-3); color: var(--c-text-muted);
      margin-left: 4px;
    }
    .empty { padding: 16px; text-align: center; color: var(--c-text-subtle); font-size: 13px; }
    .list { display: flex; flex-direction: column; gap: 12px; }
    .comment {
      display: flex; flex-direction: column; gap: 6px;
    }
    .head { display: flex; align-items: center; gap: 10px; }
    .avatar {
      flex-shrink: 0;
      width: 28px; height: 28px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      background: var(--c-text); color: var(--c-on-primary);
      font-size: 10px; font-weight: 600;
    }
    .avatar.self { background: var(--c-surface-3); color: var(--c-text); }
    .meta { flex: 1; display: flex; gap: 6px; align-items: baseline; font-size: 12px; }
    .author { font-weight: 600; color: var(--c-text); }
    time { color: var(--c-text-subtle); font-size: 11px; }
    .edited { color: var(--c-text-subtle); font-size: 11px; font-style: italic; }
    .actions { display: flex; gap: 8px; }
    .link {
      background: transparent; border: none; color: var(--c-text-muted);
      cursor: pointer; font-size: 11px; padding: 0;
    }
    .link:hover { color: var(--c-text); text-decoration: underline; }
    .link.danger:hover { color: var(--c-accent-danger); }
    .body {
      padding: 10px 12px; background: var(--c-surface);
      border: 1px solid var(--c-border); border-radius: var(--radius);
      white-space: pre-wrap; font-size: 13px; margin-left: 38px;
    }
    .editing textarea {
      margin-left: 38px; width: calc(100% - 38px); min-height: 60px;
    }
    .mentions { margin-left: 38px; display: flex; gap: 4px; flex-wrap: wrap; }
    .mention {
      font-size: 11px; padding: 1px 6px; border-radius: 3px;
      background: var(--c-surface-2); color: var(--c-text-muted);
    }
    .composer {
      margin-top: 16px; display: flex; gap: 10px; align-items: flex-start;
    }
    .composer .input { flex: 1; display: flex; flex-direction: column; gap: 8px; }
    .composer textarea { width: 100%; resize: vertical; }
    .form-actions {
      display: flex; gap: 8px; justify-content: flex-end;
      margin-left: 38px;
    }
    .composer .form-actions { margin-left: 0; }
  `]
})
export class CommentsThreadComponent implements OnChanges {
  private readonly api = inject(CommentApiService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly issueId = input.required<string>();

  readonly comments = signal<Comment[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly currentUserId = computed(() => this.auth.user()?.id ?? null);

  newBody = '';
  editBody = '';

  readonly canSubmit = () => this.newBody.trim().length > 0;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['issueId']) this.reload();
  }

  reload(): void {
    const id = this.issueId();
    if (!id) return;
    this.loading.set(true);
    this.api.listByIssue(id).subscribe({
      next: (page) => { this.comments.set(page.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    if (!this.canSubmit()) return;
    this.saving.set(true);
    this.api.create({ issueId: this.issueId(), body: this.newBody }).subscribe({
      next: (c) => {
        this.comments.update(list => [...list, c]);
        this.newBody = '';
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  startEdit(c: Comment): void {
    this.editingId.set(c.id);
    this.editBody = c.body;
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editBody = '';
  }

  saveEdit(c: Comment): void {
    if (!this.editBody.trim()) return;
    this.saving.set(true);
    this.api.update(c.id, { body: this.editBody }).subscribe({
      next: (updated) => {
        this.comments.update(list => list.map(x => x.id === updated.id ? updated : x));
        this.editingId.set(null);
        this.editBody = '';
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  requestDelete(c: Comment): void {
    this.translate
      .get([
        'comment.delete_confirm_title',
        'comment.delete_confirm_detail',
        'common.yes',
        'common.no'
      ])
      .subscribe((t) => {
        this.confirm.confirm({
          header: t['comment.delete_confirm_title'],
          message: t['comment.delete_confirm_detail'],
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: t['common.yes'],
          rejectLabel: t['common.no'],
          acceptButtonStyleClass: 'p-button-danger',
          accept: () => this.performDelete(c)
        });
      });
  }

  private performDelete(c: Comment): void {
    this.api.delete(c.id).subscribe({
      next: () => {
        this.comments.update((list) => list.filter((x) => x.id !== c.id));
      }
    });
  }

  initials(userId: string): string {
    if (userId === '?' || !userId) return '?';
    return userId.slice(0, 2).toUpperCase();
  }

  authorLabel(userId: string): string {
    return userId.slice(0, 8) + '…';
  }
}
