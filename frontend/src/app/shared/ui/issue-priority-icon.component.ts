import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

/**
 * Jira-style priority arrow icon. PrimeIcons with color theo level:
 * - 1 Lowest  → blue   ↓↓
 * - 2 Low     → green  ↓
 * - 3 Medium  → yellow =
 * - 4 High    → orange ↑
 * - 5 Highest → red    ↑↑
 *
 * Tooltip hiển thị label tiếng Anh. (Localize sau qua i18n nếu cần.)
 */
@Component({
  selector: 'app-issue-priority-icon',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <i class="pi priority-icon"
       [class]="iconClass()"
       [attr.data-pri]="priority()"
       [pTooltip]="label()"></i>
  `,
  styles: [`
    .priority-icon {
      display: inline-block;
      width: 16px; text-align: center;
      font-size: 14px;
    }
    .priority-icon[data-pri="1"] { color: #2684ff; }
    .priority-icon[data-pri="2"] { color: #57a55a; }
    .priority-icon[data-pri="3"] { color: #f5cd47; }
    .priority-icon[data-pri="4"] { color: #fd9941; }
    .priority-icon[data-pri="5"] { color: #e34935; }
  `]
})
export class IssuePriorityIconComponent {
  readonly priority = input.required<number>();

  iconClass(): string {
    switch (this.priority()) {
      case 1: return 'pi-angle-double-down';
      case 2: return 'pi-angle-down';
      case 3: return 'pi-equals';
      case 4: return 'pi-angle-up';
      case 5: return 'pi-angle-double-up';
      default: return 'pi-equals';
    }
  }

  label(): string {
    switch (this.priority()) {
      case 1: return 'Lowest';
      case 2: return 'Low';
      case 3: return 'Medium';
      case 4: return 'High';
      case 5: return 'Highest';
      default: return 'Medium';
    }
  }
}
