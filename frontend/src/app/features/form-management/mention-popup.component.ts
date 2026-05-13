import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MetadataDto } from './metadata.model';

/**
 * Popup gợi ý metadata khi user gõ `@` trong editor.
 * KHÔNG tự bắt phím — parent (editor page) intercept keyDown rồi gọi method điều khiển.
 * Position được kiểm soát ngoài qua input `style` — popup absolute, parent set top/left.
 */
@Component({
  selector: 'app-mention-popup',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="popup" [style.top.px]="anchorTop()" [style.left.px]="anchorLeft()">
      <div class="head">
        <span class="trigger">{{ '@' + (query() || '…') }}</span>
        <span class="hint">{{ 'form_mgmt.mention.hint' | translate }}</span>
      </div>
      @if (visibleItems().length === 0) {
        <p class="empty">{{ 'form_mgmt.mention.empty' | translate }}</p>
      } @else {
        <ul class="list">
          @for (m of visibleItems(); track m.id; let i = $index) {
            <li class="item" [class.active]="i === activeIndex()"
                (mouseenter)="setActive(i)"
                (mousedown)="$event.preventDefault(); pick.emit(m)">
              <span class="value">{{ m.value }}</span>
              <span class="label">{{ m.label }}</span>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .popup {
      position: absolute;
      min-width: 280px; max-width: 340px;
      background: var(--c-surface);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      box-shadow: var(--shadow-md);
      z-index: 1000;
      overflow: hidden;
    }
    .head {
      padding: 6px 10px;
      border-bottom: 1px solid var(--c-border);
      display: flex; flex-direction: column; gap: 2px;
      background: var(--c-surface-2);
    }
    .trigger { font-family: ui-monospace, monospace; font-size: 12px; font-weight: 700; }
    .hint { font-size: 11px; color: var(--c-text-muted); }
    .empty { padding: 10px; margin: 0; font-size: 12px; color: var(--c-text-muted); text-align: center; }
    .list { list-style: none; margin: 0; padding: 4px; max-height: 220px; overflow-y: auto; }
    .item {
      display: flex; flex-direction: column; gap: 1px;
      padding: 5px 8px; border-radius: var(--radius);
      cursor: pointer;
    }
    .item.active, .item:hover { background: var(--c-surface-2); }
    .value { font-family: ui-monospace, monospace; font-size: 11px; font-weight: 600; }
    .label { font-size: 12px; color: var(--c-text-muted); }
  `]
})
export class MentionPopupComponent {
  /** Toàn bộ metadata, popup tự filter theo query. */
  readonly metadata = input<MetadataDto[]>([]);
  /** Pixel offset trong editor host. Parent tính rồi truyền vào. */
  readonly anchorTop = input<number>(8);
  readonly anchorLeft = input<number>(8);
  /** Số item tối đa hiển thị — form.md §4: max 10. */
  readonly maxItems = input<number>(10);

  /** Phát khi user chọn 1 metadata (click hoặc parent gọi pickActive). */
  readonly pick = output<MetadataDto>();

  private readonly queryState = signal('');
  private readonly activeIndexState = signal(0);

  readonly query = this.queryState.asReadonly();
  readonly activeIndex = this.activeIndexState.asReadonly();

  readonly visibleItems = computed<MetadataDto[]>(() => {
    const q = this.queryState().trim().toUpperCase();
    const all = this.metadata();
    const filtered = !q
      ? all
      : all.filter((m) => m.value.includes(q) || m.label.toUpperCase().includes(q));
    return filtered.slice(0, this.maxItems());
  });

  setQuery(q: string): void {
    this.queryState.set(q);
    // Khi danh sách thay đổi, đảm bảo activeIndex không vượt range.
    const max = Math.max(0, this.visibleItems().length - 1);
    if (this.activeIndexState() > max) this.activeIndexState.set(0);
  }

  setActive(i: number): void {
    this.activeIndexState.set(i);
  }

  moveActive(delta: number): void {
    const items = this.visibleItems();
    if (items.length === 0) return;
    const next = (this.activeIndexState() + delta + items.length) % items.length;
    this.activeIndexState.set(next);
  }

  pickActive(): MetadataDto | null {
    const items = this.visibleItems();
    const i = this.activeIndexState();
    if (items.length === 0 || i < 0 || i >= items.length) return null;
    return items[i];
  }
}
