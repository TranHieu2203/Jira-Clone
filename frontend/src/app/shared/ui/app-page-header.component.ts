import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="ph">
      <div>
        <h1>{{ title() }}</h1>
        @if (subtitle()) { <p>{{ subtitle() }}</p> }
      </div>
      <div class="ph-actions"><ng-content /></div>
    </header>
  `,
  styles: [`
    .ph { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 16px; gap: 16px; flex-wrap: wrap; }
    h1 { margin: 0; font-size: 22px; font-weight: 700; color: var(--c-text); }
    p { margin: 4px 0 0; color: var(--c-text-muted); font-size: 13px; }
    .ph-actions { display: flex; gap: 8px; }
  `]
})
export class AppPageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | undefined>();
}
