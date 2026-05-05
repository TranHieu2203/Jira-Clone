import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { IssueTypeCacheService } from '@core/api/issue-type-cache.service';

/**
 * Jira-style issue type pill: square colored tag với chữ E/S/T/B/↳ tuỳ key.
 *
 * Lookup IssueType qua `IssueTypeCacheService` (cần được warm trước qua
 * `ensureProjectLoaded(projectId)`). Fallback về initial của name nếu chưa cache.
 */
@Component({
  selector: 'app-issue-type-pill',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="type-pill"
          [style.background]="color()"
          [attr.data-size]="size()"
          [pTooltip]="name()">{{ initial() }}</span>
  `,
  styles: [`
    .type-pill {
      display: inline-flex; align-items: center; justify-content: center;
      width: 18px; height: 18px;
      flex: 0 0 18px;
      border-radius: 3px;
      color: white; font-size: 10px; font-weight: 700;
      text-transform: uppercase;
      vertical-align: middle;
    }
    .type-pill[data-size="lg"] {
      width: 22px; height: 22px; flex: 0 0 22px;
      font-size: 12px;
    }
  `]
})
export class IssueTypePillComponent {
  private readonly cache = inject(IssueTypeCacheService);

  readonly typeId = input.required<string>();
  readonly size = input<'sm' | 'lg'>('sm');

  readonly name = computed(() => {
    this.cache.version();
    return this.cache.nameOf(this.typeId()) ?? '';
  });
  readonly color = computed(() => {
    this.cache.version();
    return this.cache.colorOf(this.typeId()) ?? '#6b7280';
  });
  readonly initial = computed(() => {
    this.cache.version();
    const t = this.cache.get(this.typeId());
    if (!t) return '?';
    if (t.key === 'EPIC') return 'E';
    if (t.key === 'STORY') return 'S';
    if (t.key === 'TASK') return 'T';
    if (t.key === 'BUG') return 'B';
    if (t.key === 'SUBTASK') return '↳';
    return t.name.slice(0, 1).toUpperCase();
  });
}
