import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  forwardRef,
  inject,
  input,
  OnDestroy,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import Quill from 'quill';
import { UserApiService, UserSummary } from '@core/api/user.service';

@Component({
  selector: 'app-rich-text-editor',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rte-wrap" [class.disabled]="disabled">
      <div #host class="quill-host"></div>
      @if (mentionEnabled() && mentionOpen() && mentionUsers().length > 0) {
        <ul class="mention-list" role="listbox"
            [style.left.px]="mentionPos()?.left ?? 8"
            [style.top.px]="mentionPos()?.top ?? 44">
          @for (u of mentionUsers(); track u.id) {
            <li role="option">
              <button type="button" class="mention-item" (click)="pickMention(u)">
                <span class="mu">{{ u.userName }}</span>
                <span class="md">{{ u.displayName }}</span>
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .rte-wrap {
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      background: var(--c-surface);
      position: relative;
    }
    .rte-wrap.disabled { opacity: 0.55; pointer-events: none; }
    :host ::ng-deep .ql-toolbar {
      border: none;
      border-bottom: 1px solid var(--c-border);
      background: var(--c-surface-2);
    }
    :host ::ng-deep .ql-container {
      border: none;
      font-family: inherit;
      font-size: 14px;
      min-height: 120px;
    }
    :host ::ng-deep .ql-editor {
      min-height: 120px;
      color: var(--c-text);
    }
    :host ::ng-deep .ql-editor.ql-blank::before {
      color: var(--c-text-muted);
      font-style: normal;
    }
    :host ::ng-deep .ql-stroke { stroke: var(--c-text); }
    :host ::ng-deep .ql-fill { fill: var(--c-text); }

    .mention-list {
      position: absolute;
      z-index: 30;
      min-width: 260px;
      max-width: calc(100% - 16px);
      margin: 0;
      padding: 4px 0;
      list-style: none;
      max-height: 220px;
      overflow-y: auto;
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      box-shadow: 0 8px 24px rgba(0,0,0,0.08);
    }
    .mention-item {
      display: flex;
      gap: 8px;
      width: 100%;
      padding: 8px 12px;
      border: none;
      background: transparent;
      cursor: pointer;
      text-align: left;
      font-size: 13px;
      color: var(--c-text);
    }
    .mention-item:hover { background: var(--c-surface-2); }
    .mention-item .mu { font-family: monospace; font-weight: 600; }
    .mention-item .md { color: var(--c-text-muted); }
  `],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditorComponent),
      multi: true
    }
  ]
})
export class RichTextEditorComponent implements ControlValueAccessor, AfterViewInit, OnDestroy {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly users = inject(UserApiService);

  @ViewChild('host', { read: ElementRef }) hostRef?: ElementRef<HTMLDivElement>;

  /** ngx-translate key cho placeholder (optional). */
  /** Placeholder hiển thị trong editor (parent có thể bind sau khi translate). */
  readonly placeholderText = input<string>('');
  /** Bật dropdown mention (@username) trong editor. */
  readonly mentionEnabled = input<boolean>(false);

  private quill: Quill | null = null;
  private value = '';
  disabled = false;

  private mentionAtIndex = -1;
  readonly mentionOpen = signal(false);
  readonly mentionUsers = signal<UserSummary[]>([]);
  readonly mentionPos = signal<{ left: number; top: number } | null>(null);

  private onChange: (v: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  ngAfterViewInit(): void {
    const el = this.hostRef?.nativeElement;
    if (!el) return;

    this.quill = new Quill(el, {
      theme: 'snow',
      placeholder: this.placeholderText() || undefined,
      modules: {
        toolbar: [
          ['bold', 'italic'],
          [{ list: 'ordered' }, { list: 'bullet' }],
          ['link', 'clean']
        ]
      }
    });

    // Close mention on Escape.
    this.quill.root.addEventListener('keydown', (e: KeyboardEvent) => {
      if (!this.mentionEnabled()) return;
      if (e.key === 'Escape' && this.mentionOpen()) {
        e.preventDefault();
        this.closeMention();
      }
    });

    this.quill.on('text-change', () => {
      if (!this.quill) return;
      const html = this.quill.root.innerHTML;
      const plain = this.quill.getText().trim();
      this.value = plain.length === 0 ? '' : html;
      this.onChange(this.value);
      this.onTouched();
      if (this.mentionEnabled()) {
        this.scanMention();
      }
      this.cdr.markForCheck();
    });

    this.quill.on('selection-change', () => {
      if (!this.mentionEnabled()) return;
      // Khi user click chỗ khác, close nếu không còn trong context mention.
      this.scanMention();
    });

    if (this.value) {
      this.quill.root.innerHTML = this.value;
    }

    this.quill.enable(!this.disabled);
  }

  ngOnDestroy(): void {
    this.quill = null;
  }

  private scanMention(): void {
    const q = this.quill;
    if (!q) return;
    // Quill can return null selection inside text-change; `true` returns last known range.
    const sel = q.getSelection(true);
    if (!sel) {
      this.closeMention();
      return;
    }

    const pos = sel.index;
    const before = q.getText(0, pos);
    const at = before.lastIndexOf('@');
    if (at < 0) {
      this.closeMention();
      return;
    }
    // Chỉ chặn khi @ dính vào từ ASCII (Jira cho phép sau dấu câu, xuống dòng, v.v.).
    if (at > 0) {
      const ch = before[at - 1];
      if (/[A-Za-z0-9_]/.test(ch)) {
        this.closeMention();
        return;
      }
    }
    const queryRaw = before.slice(at + 1);
    const query = queryRaw.replace(/\u200B/g, '').trimEnd(); // normalize Quill artifacts + trailing newline
    if (query.length > 64 || /[\s\n]/.test(query)) {
      this.closeMention();
      return;
    }

    this.mentionAtIndex = at;
    // Position dropdown near caret.
    try {
      const bounds = q.getBounds(pos) ?? { left: 0, top: 0 };
      const hostWidth = this.hostRef?.nativeElement.clientWidth ?? 600;
      const left = Math.max(8, Math.min(bounds.left + 8, hostWidth - 280));
      const top = bounds.top + 48; // toolbar + gap
      this.mentionPos.set({ left, top });
    } catch {
      this.mentionPos.set({ left: 8, top: 44 });
    }
    // empty query => show some users (API supports empty q? current impl does trim; still returns all?).
    this.users.search(query, 15).subscribe({
      next: (list) => {
        if (list.length === 0) {
          this.closeMention();
          return;
        }
        this.mentionUsers.set(list);
        this.mentionOpen.set(true);
        this.cdr.markForCheck();
      },
      error: () => this.closeMention()
    });
  }

  pickMention(u: UserSummary): void {
    const q = this.quill;
    if (!q || this.mentionAtIndex < 0) return;
    const sel = q.getSelection();
    const pos = sel?.index ?? q.getLength();

    const insert = `@${u.userName} `;
    q.deleteText(this.mentionAtIndex, pos - this.mentionAtIndex, 'user');
    q.insertText(this.mentionAtIndex, insert, 'user');
    q.setSelection(this.mentionAtIndex + insert.length, 0, 'user');
    this.closeMention();
  }

  private closeMention(): void {
    this.mentionOpen.set(false);
    this.mentionUsers.set([]);
    this.mentionAtIndex = -1;
    this.mentionPos.set(null);
    this.cdr.markForCheck();
  }

  writeValue(obj: string | null): void {
    this.value = obj ?? '';
    if (this.quill) {
      const cur = this.quill.root.innerHTML;
      if (cur !== this.value) {
        this.quill.root.innerHTML = this.value;
      }
    }
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (v: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.quill?.enable(!isDisabled);
    this.cdr.markForCheck();
  }
}
