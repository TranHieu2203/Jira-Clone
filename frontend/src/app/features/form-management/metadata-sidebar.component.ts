import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { METADATA_GROUPS, MetadataDto } from './metadata.model';

interface MetadataGroupView {
  key: string;
  groupLabel: string;
  items: MetadataDto[];
  expanded: boolean;
}

@Component({
  selector: 'app-metadata-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, InputTextModule, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="head">
      <strong>{{ 'form_mgmt.sidebar.title' | translate }}</strong>
      <small class="hint">{{ 'form_mgmt.sidebar.hint' | translate }}</small>
    </div>

    <div class="search">
      <input pInputText type="text" [(ngModel)]="keyword"
             (ngModelChange)="onKeywordChange($event)"
             [placeholder]="'form_mgmt.sidebar.search_placeholder' | translate" />
    </div>

    @if (metadata().length === 0) {
      <p class="hint pad">{{ 'form_mgmt.sidebar.empty' | translate }}</p>
    } @else {
      <div class="groups">
        @for (g of filteredGroups(); track g.key) {
          <div class="group">
            <button type="button" class="group-head" (click)="toggle(g.key)">
              <span class="caret">{{ g.expanded ? '▾' : '▸' }}</span>
              <span class="g-key">{{ g.key }}</span>
              <span class="g-label">{{ g.groupLabel }}</span>
              <span class="g-count">{{ g.items.length }}</span>
            </button>
            @if (g.expanded) {
              <ul class="items">
                @for (m of g.items; track m.id) {
                  <li>
                    <button type="button" class="item"
                            [draggable]="true"
                            (dragstart)="onDragStart($event, m)"
                            (click)="insert.emit(m)"
                            [title]="m.label + (m.description ? ' — ' + m.description : '')">
                      <span class="m-value">{{ m.value }}</span>
                      <span class="m-label">{{ m.label }}</span>
                    </button>
                  </li>
                }
              </ul>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: var(--c-surface);
      border-right: 1px solid var(--c-border);
      overflow: hidden;
    }
    .head { padding: 12px 12px 6px; display: flex; flex-direction: column; gap: 2px; }
    .head strong { font-size: 13px; font-weight: 600; }
    .hint { color: var(--c-text-muted); font-size: 12px; }
    .hint.pad { padding: 12px; }
    .search { padding: 6px 12px 8px; }
    .search input { width: 100%; font-size: 12px; padding: 6px 8px; }
    .groups { flex: 1; overflow-y: auto; padding: 0 4px 12px; }
    .group { margin-bottom: 2px; }
    .group-head {
      width: 100%; display: flex; align-items: center; gap: 6px;
      padding: 6px 8px; border: none; background: transparent; cursor: pointer;
      font-size: 12px; color: var(--c-text); text-align: left;
      border-radius: var(--radius);
    }
    .group-head:hover { background: var(--c-surface-2); }
    .caret { width: 12px; opacity: 0.7; }
    .g-key { font-family: ui-monospace, monospace; font-weight: 700; }
    .g-label { flex: 1; color: var(--c-text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .g-count { color: var(--c-text-muted); font-size: 11px; padding: 0 4px; background: var(--c-surface-2); border-radius: 999px; }
    .items { list-style: none; padding: 0 0 4px 18px; margin: 0; }
    .items li { margin: 1px 0; }
    .item {
      width: 100%; display: flex; flex-direction: column; gap: 1px;
      padding: 5px 8px; border: 1px solid transparent; background: transparent;
      cursor: grab; text-align: left; border-radius: var(--radius);
    }
    .item:hover { background: var(--c-surface-2); border-color: var(--c-border); }
    .item:active { cursor: grabbing; }
    .m-value { font-family: ui-monospace, monospace; font-size: 11px; font-weight: 600; color: var(--c-text); }
    .m-label { font-size: 12px; color: var(--c-text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  `]
})
export class MetadataSidebarComponent {
  /** Toàn bộ metadata đã load — sidebar tự filter + group ở client cho responsive. */
  readonly metadata = input<MetadataDto[]>([]);
  /** Phát khi user click vào metadata — caller insert vào editor tại cursor. */
  readonly insert = output<MetadataDto>();

  private readonly translate = inject(TranslateService);

  keyword = '';
  private readonly keywordSignal = signal('');
  private readonly expandedKeys = signal<Set<string>>(new Set());
  private readonly groupLabels = signal<Record<string, string>>({});

  constructor() {
    // Pre-translate group label keys (PATTERNS §2.5 — không dùng .instant()).
    const keys = METADATA_GROUPS.map((g) => g.i18nKey);
    this.translate.get(keys).subscribe((t) => {
      const map: Record<string, string> = {};
      for (const g of METADATA_GROUPS) map[g.value] = t[g.i18nKey];
      this.groupLabels.set(map);
    });
  }

  readonly filteredGroups = computed<MetadataGroupView[]>(() => {
    const kw = this.keywordSignal().trim().toUpperCase();
    const labels = this.groupLabels();
    const expanded = this.expandedKeys();

    const items = this.metadata().filter((m) => {
      if (!kw) return true;
      return m.value.includes(kw) || m.label.toUpperCase().includes(kw);
    });

    // Group theo fieldGroup (đã derive ở BE từ prefix của value).
    const byGroup = new Map<string, MetadataDto[]>();
    for (const m of items) {
      const k = m.fieldGroup ?? '?';
      const arr = byGroup.get(k) ?? [];
      arr.push(m);
      byGroup.set(k, arr);
    }

    // Sắp xếp groups theo thứ tự convention (B/C/D/…) + group còn lại append cuối alpha.
    const order = METADATA_GROUPS.map((g) => g.value);
    const allKeys = Array.from(byGroup.keys()).sort((a, b) => {
      const ia = order.indexOf(a);
      const ib = order.indexOf(b);
      if (ia === -1 && ib === -1) return a.localeCompare(b);
      if (ia === -1) return 1;
      if (ib === -1) return -1;
      return ia - ib;
    });

    return allKeys.map((key) => ({
      key,
      groupLabel: labels[key] ?? '',
      items: byGroup.get(key)!.sort((a, b) => a.value.localeCompare(b.value)),
      // Khi đang search → mở hết group; ngược lại tôn trọng state expanded user click.
      expanded: kw.length > 0 ? true : expanded.has(key)
    }));
  });

  onKeywordChange(value: string): void {
    this.keywordSignal.set(value);
  }

  toggle(key: string): void {
    const set = new Set(this.expandedKeys());
    if (set.has(key)) set.delete(key); else set.add(key);
    this.expandedKeys.set(set);
  }

  onDragStart(event: DragEvent, m: MetadataDto): void {
    if (!event.dataTransfer) return;
    // text/plain để fallback paste; custom mime để editor host nhận diện chính xác data của ta.
    event.dataTransfer.setData('application/x-form-mgmt-metadata', JSON.stringify({ value: m.value, label: m.label }));
    event.dataTransfer.setData('text/plain', `«${m.value}»`);
    event.dataTransfer.effectAllowed = 'copy';
  }
}
