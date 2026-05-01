import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import { Workspace, WorkspaceApiService } from '@core/api/workspace.service';

@Component({
  selector: 'app-workspaces-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TranslateModule,
    ButtonModule, DialogModule, InputTextModule,
    AppPageHeaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'workspace.title' | translate">
      <button pButton (click)="openCreate()" [label]="'workspace.create' | translate"></button>
    </app-page-header>

    @if (loading()) {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    } @else if (rows().length === 0) {
      <div class="empty">{{ 'workspace.empty' | translate }}</div>
    } @else {
      <div class="grid">
        @for (w of rows(); track w.id) {
          <a class="card" [routerLink]="['/workspaces', w.slug]">
            <div class="avatar">{{ w.name[0] }}</div>
            <div class="content">
              <div class="name">{{ w.name }}</div>
              <div class="slug">{{ '@' }}{{ w.slug }}</div>
              @if (w.description) {
                <div class="desc">{{ w.description }}</div>
              }
              <div class="meta">{{ w.memberCount }} {{ 'workspace.members' | translate }}</div>
            </div>
          </a>
        }
      </div>
    }

    <p-dialog [(visible)]="dialogVisible" [modal]="true" [style]="{ width: '440px' }"
              [header]="'workspace.create' | translate">
      <form (ngSubmit)="save()" class="form">
        <label class="field">
          <span>{{ 'workspace.name' | translate }}</span>
          <input pInputText [(ngModel)]="model.name" name="name" required />
        </label>
        <label class="field">
          <span>{{ 'workspace.slug' | translate }}</span>
          <input pInputText [(ngModel)]="model.slug" name="slug" required pattern="^[a-z][a-z0-9-]+$" />
          <small>{{ 'workspace.slug_hint' | translate }}</small>
        </label>
        <label class="field">
          <span>{{ 'workspace.description' | translate }}</span>
          <input pInputText [(ngModel)]="model.description" name="description" />
        </label>
        <div class="actions">
          <button pButton type="button" [text]="true" (click)="dialogVisible = false" [label]="'common.cancel' | translate"></button>
          <button pButton type="submit" [loading]="saving()" [label]="'common.save' | translate"></button>
        </div>
      </form>
    </p-dialog>
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
    .card {
      display: flex; gap: 14px; padding: 16px;
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); text-decoration: none; color: var(--c-text);
      transition: border-color 0.1s, box-shadow 0.1s;
    }
    .card:hover { border-color: var(--c-border-strong); box-shadow: var(--shadow-sm); text-decoration: none; }
    .avatar {
      flex-shrink: 0; width: 40px; height: 40px; border-radius: 50%;
      background: var(--c-text); color: var(--c-on-primary);
      display: flex; align-items: center; justify-content: center;
      font-weight: 600; font-size: 16px;
    }
    .content { flex: 1; min-width: 0; }
    .name { font-weight: 600; font-size: 15px; }
    .slug { font-size: 12px; color: var(--c-text-muted); margin-top: 2px; }
    .desc { font-size: 13px; color: var(--c-text-muted); margin-top: 6px;
            overflow: hidden; text-overflow: ellipsis; display: -webkit-box;
            -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
    .meta { font-size: 11px; color: var(--c-text-subtle); margin-top: 8px; }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
    .form { display: flex; flex-direction: column; gap: 12px; }
    .field { display: flex; flex-direction: column; gap: 6px; font-size: 13px; color: var(--c-text-muted); }
    .field small { font-size: 11px; color: var(--c-text-subtle); }
    .actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 8px; }
  `]
})
export class WorkspacesPageComponent implements OnInit {
  private readonly api = inject(WorkspaceApiService);

  readonly rows = signal<Workspace[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);

  dialogVisible = false;
  model = { name: '', slug: '', description: '' };

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.listMine().subscribe({
      next: (list) => { this.rows.set(list); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.model = { name: '', slug: '', description: '' };
    this.dialogVisible = true;
  }

  save(): void {
    this.saving.set(true);
    this.api.create({
      name: this.model.name,
      slug: this.model.slug.toLowerCase(),
      description: this.model.description || null
    }).subscribe({
      next: () => { this.saving.set(false); this.dialogVisible = false; this.reload(); },
      error: () => this.saving.set(false)
    });
  }
}
