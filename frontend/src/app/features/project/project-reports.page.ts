import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AppPageHeaderComponent } from '@shared/ui/app-page-header.component';
import {
  ProjectApiService,
  ProjectDetail,
  projectDetailToSummary
} from '@core/api/project.service';
import { WorkspaceContextService } from '@core/layout/workspace-context.service';
import { SprintApiService, SprintBurndownDto } from '@core/api/sprint-api.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-project-reports-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, AppPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (project(); as p) {
      <app-page-header [title]="'nav.reports' | translate">
        <span class="key">{{ p.key }}</span>
      </app-page-header>

      @if (burndownLoading()) {
        <p class="intro">{{ 'common.loading' | translate }}</p>
      } @else if (!burndown()) {
        <p class="intro">{{ 'reports.burndown_need_active' | translate }}</p>
      } @else {
        <section class="chart-card">
          <h2 class="h">{{ 'reports.burndown_title' | translate }}</h2>
          <p class="meta">{{ 'reports.burndown_total_points' | translate }}: {{ burndown()!.totalPoints }}</p>
          <svg class="chart" [attr.viewBox]="viewBox()" preserveAspectRatio="none">
            <rect width="100%" height="100%" fill="var(--c-surface)" />
            @for (g of gridLines(); track g) {
              <line [attr.x1]="pad" [attr.y1]="g" [attr.x2]="chartW - pad" [attr.y2]="g"
                    stroke="var(--c-border)" stroke-width="0.5" />
            }
            <polyline [attr.points]="idealPoints()" fill="none" stroke="var(--c-text-muted)"
                      stroke-width="1.5" stroke-dasharray="4 3" />
            <polyline [attr.points]="actualPoints()" fill="none" stroke="var(--c-text)"
                      stroke-width="2" />
          </svg>
          <div class="legend">
            <span class="lg ideal"><i></i> {{ 'reports.burndown_ideal' | translate }}</span>
            <span class="lg actual"><i></i> {{ 'reports.burndown_actual' | translate }}</span>
          </div>
        </section>
      }

      <div class="links-bar">
        <a [routerLink]="['/projects', p.key]" class="nav-link">{{ 'nav.overview' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'board']" class="nav-link">{{ 'nav.board' | translate }}</a>
        <a [routerLink]="['/projects', p.key, 'backlog']" class="nav-link">{{ 'nav.backlog' | translate }}</a>
      </div>
    } @else {
      <div class="empty">{{ 'common.loading' | translate }}</div>
    }
  `,
  styles: [`
    .key { font-family: monospace; font-size: 12px; color: var(--c-text-muted); }
    .intro { font-size: 14px; color: var(--c-text-muted); max-width: 560px; line-height: 1.45; margin-bottom: 20px; }
    .chart-card {
      background: var(--c-surface-2);
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      padding: 16px;
      max-width: 720px;
      margin-bottom: 24px;
    }
    .h { font-size: 15px; margin: 0 0 8px; color: var(--c-text); }
    .meta { font-size: 12px; color: var(--c-text-muted); margin: 0 0 12px; }
    .chart { width: 100%; height: 220px; display: block; border-radius: var(--radius); }
    .legend { display: flex; gap: 20px; margin-top: 12px; font-size: 12px; color: var(--c-text-muted); }
    .lg { display: inline-flex; align-items: center; gap: 6px; }
    .lg i { width: 20px; height: 3px; display: inline-block; border-radius: 1px; }
    .lg.ideal i { background: var(--c-text-muted); opacity: 0.8; }
    .lg.actual i { background: var(--c-text); }
    .links-bar { display: flex; flex-wrap: wrap; gap: 14px; }
    .nav-link {
      font-size: 13px; font-weight: 500; color: var(--c-primary); text-decoration: none;
    }
    .nav-link:hover { text-decoration: underline; }
    .empty { padding: 40px; text-align: center; color: var(--c-text-muted); }
  `]
})
export class ProjectReportsPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProjectApiService);
  private readonly sprintApi = inject(SprintApiService);
  private readonly ctx = inject(WorkspaceContextService);

  readonly project = signal<ProjectDetail | null>(null);
  readonly burndown = signal<SprintBurndownDto | null>(null);
  readonly burndownLoading = signal(false);

  readonly pad = 8;
  readonly chartW = 400;
  readonly chartH = 200;

  readonly viewBox = computed(() => `0 0 ${this.chartW} ${this.chartH}`);

  readonly gridLines = computed(() => {
    const n = 4;
    const lines: number[] = [];
    const innerH = this.chartH - 2 * this.pad;
    for (let i = 0; i <= n; i++) {
      lines.push(this.pad + (innerH * i) / n);
    }
    return lines;
  });

  readonly idealPoints = computed(() => this.polylinePoints(true));
  readonly actualPoints = computed(() => this.polylinePoints(false));

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('projectKey');
    if (!key) return;
    void this.load(key);
  }

  ngOnDestroy(): void {
    this.ctx.setProject(null);
  }

  private polylinePoints(ideal: boolean): string {
    const bd = this.burndown();
    if (!bd?.days?.length) return '';
    const innerW = this.chartW - 2 * this.pad;
    const innerH = this.chartH - 2 * this.pad;
    const days = bd.days;
    const n = Math.max(days.length - 1, 1);
    let maxY = 1;
    for (const d of days) {
      maxY = Math.max(maxY, d.idealRemaining, d.actualRemaining);
    }
    const pts: string[] = [];
    for (let i = 0; i < days.length; i++) {
      const x = this.pad + (innerW * i) / n;
      const v = ideal ? days[i].idealRemaining : days[i].actualRemaining;
      const y = this.pad + innerH * (1 - v / maxY);
      pts.push(`${x.toFixed(1)},${y.toFixed(1)}`);
    }
    return pts.join(' ');
  }

  private async load(projectKey: string): Promise<void> {
    this.burndownLoading.set(true);
    try {
      const detail = await firstValueFrom(this.api.getDetailForMemberByKey(projectKey));
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));

      let sprintId: string | null = null;
      const active = await firstValueFrom(this.sprintApi.getActive(detail.id));
      if (active) sprintId = active.id;
      else {
        const list = await firstValueFrom(this.sprintApi.list(detail.id));
        const completed = list
          .filter((s) => s.status === 2)
          .sort((a, b) => new Date(b.endDate).getTime() - new Date(a.endDate).getTime())[0];
        if (completed) sprintId = completed.id;
      }
      if (!sprintId) {
        this.burndown.set(null);
        return;
      }
      const bd = await firstValueFrom(this.sprintApi.burndown(detail.id, sprintId));
      this.burndown.set(bd);
    } catch {
      this.burndown.set(null);
    } finally {
      this.burndownLoading.set(false);
    }
  }
}
