import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input, model, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AutoComplete } from 'primeng/autocomplete';
import type { AutoCompleteCompleteEvent, AutoCompleteSelectEvent } from 'primeng/autocomplete';
import { catchError, map, of, switchMap } from 'rxjs';
import { UserApiService, UserSummary } from '@core/api/user.service';

type UserSummaryRow = UserSummary & { label: string };

function withLabel(u: UserSummary): UserSummaryRow {
  return { ...u, label: `${u.userName} — ${u.displayName}` };
}

@Component({
  selector: 'app-user-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, AutoComplete],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-autoComplete
      [(ngModel)]="picked"
      [suggestions]="suggestions()"
      (completeMethod)="onComplete($event)"
      (onSelect)="onSelect($event)"
      (onClear)="onClear()"
      optionLabel="label"
      [dropdown]="true"
      [forceSelection]="true"
      [showClear]="true"
      [minLength]="0"
      [delay]="250"
      [placeholder]="placeholderKey() | translate"
      inputStyleClass="user-picker-input"
      styleClass="user-picker-ac"
      appendTo="body"
    >
      <ng-template let-u #item>
        <span>{{ u.userName }} — {{ u.displayName }}</span>
      </ng-template>
    </p-autoComplete>
  `,
  styles: [`
    :host { display: block; width: 100%; min-width: 0; }
    ::ng-deep .user-picker-input { width: 100%; font-size: 13px; }
  `]
})
export class UserPickerComponent {
  private readonly users = inject(UserApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly userId = model<string | null>(null);
  readonly placeholderKey = input<string>('user.search_placeholder');

  readonly suggestions = signal<UserSummaryRow[]>([]);
  picked: UserSummaryRow | null = null;

  constructor() {
    toObservable(this.userId)
      .pipe(
        switchMap((id) => {
          if (!id) {
            return of<UserSummaryRow | null>(null);
          }
          return this.users.getById(id).pipe(
            map(withLabel),
            catchError(() => of<UserSummaryRow | null>(null))
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((u) => {
        this.picked = u;
        this.cdr.markForCheck();
      });
  }

  onComplete(ev: AutoCompleteCompleteEvent): void {
    this.users.search(ev.query ?? '', 20).subscribe({
      next: (list) => this.suggestions.set(list.map(withLabel))
    });
  }

  onSelect(ev: AutoCompleteSelectEvent): void {
    const row = ev.value as UserSummaryRow | undefined;
    if (row) this.userId.set(row.id);
  }

  onClear(): void {
    this.userId.set(null);
    this.picked = null;
    this.suggestions.set([]);
    this.cdr.markForCheck();
  }
}
