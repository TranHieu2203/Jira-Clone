import { ChangeDetectionStrategy, Component, inject, input, model, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ProjectApiService, ProjectDetail, ProjectType } from '@core/api/project.service';
import { AuthService } from '@core/auth/auth.service';
import { UserPickerComponent } from '@shared/ui/user-picker.component';

@Component({
  selector: 'app-create-project-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslateModule,
    ButtonModule, DialogModule, InputTextModule, SelectModule, UserPickerComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog [visible]="visible()" (visibleChange)="onVisibleChange($event)"
              [modal]="true" [style]="{ width: '480px' }"
              [header]="'project.create' | translate">
      <form (ngSubmit)="save()" class="form" #f="ngForm">
        <label class="field">
          <span>{{ 'project.name' | translate }} *</span>
          <input pInputText [(ngModel)]="model.name" name="name" required maxlength="200" />
        </label>
        <label class="field">
          <span>{{ 'project.key' | translate }} *</span>
          <input pInputText [(ngModel)]="model.key" name="key"
                 required pattern="^[A-Z][A-Z0-9]{1,9}$"
                 (input)="onKeyInput($event)"
                 [placeholder]="'project.key_placeholder' | translate" />
          <small>{{ 'project.key_hint' | translate }}</small>
        </label>
        <label class="field">
          <span>{{ 'project.type' | translate }} *</span>
          <p-select [(ngModel)]="model.type" name="type"
                    [options]="typeOptions"
                    optionLabel="label" optionValue="value"
                    appendTo="body" />
        </label>
        <label class="field">
          <span>{{ 'project.lead' | translate }}</span>
          <app-user-picker [(userId)]="leadUserId" />
        </label>
        <label class="field">
          <span>{{ 'workspace.description' | translate }}</span>
          <input pInputText [(ngModel)]="model.description" name="description" maxlength="2000" />
        </label>
        <div class="actions">
          <button pButton type="button" [text]="true"
                  (click)="visible.set(false)"
                  [label]="'common.cancel' | translate"></button>
          <button pButton type="submit"
                  [loading]="saving()"
                  [disabled]="f.invalid || saving()"
                  [label]="'common.save' | translate"></button>
        </div>
      </form>
    </p-dialog>
  `,
  styles: [`
    .form { display: flex; flex-direction: column; gap: 12px; }
    .field { display: flex; flex-direction: column; gap: 6px; font-size: 13px; color: var(--c-text-muted); }
    .field small { font-size: 11px; color: var(--c-text-subtle); }
    .actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 8px; }
  `]
})
export class CreateProjectDialogComponent {
  private readonly api = inject(ProjectApiService);
  private readonly auth = inject(AuthService);

  readonly workspaceId = input.required<string>();
  readonly visible = model<boolean>(false);
  readonly leadUserId = model<string | null>(null);
  readonly created = output<ProjectDetail>();

  readonly saving = signal(false);

  readonly typeOptions = [
    { label: 'Scrum', value: 1 as ProjectType },
    { label: 'Kanban', value: 2 as ProjectType }
  ];

  model = { name: '', key: '', description: '', type: 1 as ProjectType };

  onVisibleChange(open: boolean): void {
    this.visible.set(open);
    if (open) this.leadUserId.set(this.auth.user()?.id ?? null);
  }

  onKeyInput(e: Event): void {
    const v = (e.target as HTMLInputElement).value.toUpperCase().replace(/[^A-Z0-9]/g, '');
    this.model.key = v;
  }

  save(): void {
    const fallbackId = this.auth.user()?.id;
    const leadId = this.leadUserId() ?? fallbackId;
    if (!leadId) return;
    this.saving.set(true);
    this.api.create({
      workspaceId: this.workspaceId(),
      name: this.model.name,
      key: this.model.key.toUpperCase(),
      leadId,
      type: this.model.type,
      description: this.model.description || null
    }).subscribe({
      next: (p) => {
        this.saving.set(false);
        this.visible.set(false);
        this.created.emit(p);
        this.model = { name: '', key: '', description: '', type: 1 };
        this.leadUserId.set(this.auth.user()?.id ?? null);
      },
      error: () => this.saving.set(false)
    });
  }
}
