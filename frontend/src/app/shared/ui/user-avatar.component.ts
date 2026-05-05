import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { UserCacheService } from '@core/api/user-cache.service';

/**
 * Jira-style user avatar — circle with initials.
 * - userId null → dashed empty circle với icon pi-user
 * - Có name lookup qua UserCacheService (cần warm trước)
 * - 3 size: sm (20px), md (24px default), lg (32px)
 */
@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (userId()) {
      <span class="avatar"
            [attr.data-size]="size()"
            [pTooltip]="name() ?? '?'">{{ initials() }}</span>
    } @else {
      <span class="avatar avatar-empty"
            [attr.data-size]="size()"
            [pTooltip]="emptyTooltip()">
        <i class="pi pi-user"></i>
      </span>
    }
  `,
  styles: [`
    .avatar {
      display: inline-flex; align-items: center; justify-content: center;
      width: 24px; height: 24px; border-radius: 50%;
      background: var(--c-text); color: var(--c-on-primary);
      font-size: 10px; font-weight: 600;
      flex: 0 0 24px;
    }
    .avatar[data-size="sm"] { width: 20px; height: 20px; flex: 0 0 20px; font-size: 9px; }
    .avatar[data-size="lg"] { width: 32px; height: 32px; flex: 0 0 32px; font-size: 12px; }
    .avatar-empty {
      background: transparent; color: var(--c-text-subtle);
      border: 1px dashed var(--c-border);
    }
    .avatar-empty .pi { font-size: 11px; }
    .avatar[data-size="sm"].avatar-empty .pi { font-size: 9px; }
    .avatar[data-size="lg"].avatar-empty .pi { font-size: 14px; }
  `]
})
export class UserAvatarComponent {
  private readonly cache = inject(UserCacheService);

  readonly userId = input<string | null | undefined>(null);
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly emptyTooltip = input<string>('Unassigned');

  readonly name = computed(() => {
    this.cache.version();
    return this.cache.displayNameOf(this.userId());
  });
  readonly initials = computed(() => {
    this.cache.version();
    return this.cache.initialsOf(this.userId());
  });
}
