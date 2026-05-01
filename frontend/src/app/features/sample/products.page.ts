import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { Product, ProductService } from './product.service';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslateModule,
    ButtonModule, TableModule, DialogModule, InputTextModule, InputNumberModule,
    ConfirmDialogModule, ToggleSwitchModule, AppPageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  template: `
    <app-page-header [title]="'product.list' | translate">
      <button pButton (click)="openCreate()" [label]="'product.create' | translate"></button>
    </app-page-header>

    <p-table [value]="rows()" [loading]="loading()" stripedRows>
      <ng-template pTemplate="header">
        <tr>
          <th>{{ 'product.sku' | translate }}</th>
          <th>{{ 'product.name' | translate }}</th>
          <th class="r">{{ 'product.price' | translate }}</th>
          <th>{{ 'product.is_active' | translate }}</th>
          <th>{{ 'product.created_at' | translate }}</th>
          <th class="r">{{ 'product.actions' | translate }}</th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-r>
        <tr>
          <td><code>{{ r.sku }}</code></td>
          <td>{{ r.name }}</td>
          <td class="r">{{ r.price | number:'1.0-2' }}</td>
          <td>{{ r.isActive ? '✓' : '⊘' }}</td>
          <td>{{ r.createdAt | date:'short' }}</td>
          <td class="r">
            <button pButton size="small" [text]="true" (click)="openEdit(r)" [label]="'common.edit' | translate"></button>
            <button pButton size="small" [text]="true" severity="danger" (click)="confirmDelete(r)" [label]="'common.delete' | translate"></button>
          </td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr><td colspan="6" class="empty">{{ 'common.loading' | translate }}</td></tr>
      </ng-template>
    </p-table>

    <p-dialog [(visible)]="dialogVisible" [modal]="true" [style]="{ width: '440px' }"
              [header]="(editing() ? 'product.edit' : 'product.create') | translate">
      <form (ngSubmit)="save()" class="form">
        <label class="field">
          <span>{{ 'product.name' | translate }}</span>
          <input pInputText [(ngModel)]="model.name" name="name" required />
        </label>
        <label class="field">
          <span>{{ 'product.sku' | translate }}</span>
          <input pInputText [(ngModel)]="model.sku" name="sku" [disabled]="!!editing()" required />
        </label>
        <label class="field">
          <span>{{ 'product.price' | translate }}</span>
          <p-inputNumber [(ngModel)]="model.price" name="price" mode="decimal" [minFractionDigits]="0" [maxFractionDigits]="2" required />
        </label>
        <label class="field">
          <span>{{ 'product.description' | translate }}</span>
          <input pInputText [(ngModel)]="model.description" name="description" />
        </label>
        @if (editing()) {
          <label class="field row">
            <span>{{ 'product.is_active' | translate }}</span>
            <p-toggleSwitch [(ngModel)]="model.isActive" name="isActive" />
          </label>
        }
        <div class="actions">
          <button pButton type="button" [text]="true" (click)="dialogVisible = false" [label]="'common.cancel' | translate"></button>
          <button pButton type="submit" [loading]="saving()" [label]="'common.save' | translate"></button>
        </div>
      </form>
    </p-dialog>

    <p-confirmDialog />
  `,
  styles: [`
    .r { text-align: right; }
    .empty { text-align: center; color: var(--c-text-muted); padding: 24px; }
    .form { display: flex; flex-direction: column; gap: 12px; }
    .field { display: flex; flex-direction: column; gap: 6px; font-size: 13px; color: var(--c-text-muted); }
    .field.row { flex-direction: row; align-items: center; justify-content: space-between; }
    .actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 8px; }
  `]
})
export class ProductsPageComponent implements OnInit {
  private readonly api = inject(ProductService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly rows = signal<Product[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editing = signal<Product | null>(null);

  dialogVisible = false;
  model = { name: '', sku: '', price: 0, description: '', isActive: true };

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.search({ pageIndex: 1, pageSize: 50 }).subscribe({
      next: (page) => { this.rows.set(page.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.model = { name: '', sku: '', price: 0, description: '', isActive: true };
    this.dialogVisible = true;
  }

  openEdit(p: Product): void {
    this.editing.set(p);
    this.model = { name: p.name, sku: p.sku, price: p.price, description: p.description ?? '', isActive: p.isActive };
    this.dialogVisible = true;
  }

  save(): void {
    this.saving.set(true);
    const editing = this.editing();
    const obs = editing
      ? this.api.update(editing.id, { name: this.model.name, price: this.model.price, description: this.model.description, isActive: this.model.isActive })
      : this.api.create({ name: this.model.name, sku: this.model.sku, price: this.model.price, description: this.model.description });
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible = false; this.reload(); },
      error: () => this.saving.set(false)
    });
  }

  confirmDelete(p: Product): void {
    this.translate
      .get(['product.delete_confirm', 'common.delete', 'common.yes', 'common.cancel'], { name: p.name })
      .subscribe((t) => {
        this.confirm.confirm({
          header: t['common.delete'],
          message: t['product.delete_confirm'],
          acceptLabel: t['common.yes'],
          rejectLabel: t['common.cancel'],
          acceptButtonStyleClass: 'p-button-danger',
          accept: () => this.api.delete(p.id).subscribe(() => this.reload())
        });
      });
  }
}
