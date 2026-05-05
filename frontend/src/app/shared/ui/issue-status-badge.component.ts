import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StatusCacheService } from '@core/api/status-cache.service';

/**
 * Jira-style status badge: uppercase bold text với background color theo workflow category.
 * - Cat 1 (To-do)      → grey
 * - Cat 2 (In progress) → blue
 * - Cat 3 (Done)        → green
 *
 * Có dark theme override. Render compact (size=sm) hoặc default cho header.
 */
@Component({
  selector: 'app-issue-status-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="status-badge" [attr.data-cat]="cat()" [attr.data-size]="size()">
      {{ name() }}
    </span>
  `,
  styles: [`
    .status-badge {
      display: inline-flex; align-items: center;
      padding: 3px 8px;
      border-radius: 3px;
      font-size: 10px; font-weight: 700;
      text-transform: uppercase; letter-spacing: 0.3px;
      background: #dfe1e6; color: #42526e;
      white-space: nowrap;
    }
    .status-badge[data-cat="2"] { background: #deebff; color: #0747a6; }
    .status-badge[data-cat="3"] { background: #e3fcef; color: #006644; }
    .status-badge[data-size="lg"] { font-size: 11px; padding: 4px 10px; letter-spacing: 0.4px; }
    [data-theme="dark"] .status-badge { background: #44546f; color: #c7d1e0; }
    [data-theme="dark"] .status-badge[data-cat="2"] { background: #1c3d6e; color: #a0c4f4; }
    [data-theme="dark"] .status-badge[data-cat="3"] { background: #1a4731; color: #8fdcb0; }
  `]
})
export class IssueStatusBadgeComponent {
  private readonly cache = inject(StatusCacheService);

  readonly statusId = input.required<string>();
  readonly size = input<'sm' | 'lg'>('sm');

  readonly name = computed(() => {
    this.cache.version(); // subscribe → re-eval khi cache fill
    const id = this.statusId();
    return this.cache.nameOf(id) ?? id.slice(0, 8) + '…';
  });

  readonly cat = computed(() => {
    this.cache.version();
    return this.cache.categoryOf(this.statusId()) ?? 1;
  });
}
