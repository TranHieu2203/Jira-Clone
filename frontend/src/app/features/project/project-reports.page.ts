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
import {
  SprintApiService,
  SprintBurndownDto,
  VelocityReportDto,
} from '@core/api/sprint-api.service';
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

      <!-- F7: Velocity report -->
      @if (velocityLoading()) {
        <p class="intro">{{ 'common.loading' | translate }}</p>
      } @else if (velocity() && velocity()!.sprints.length > 0) {
        <section class="chart-card">
          <h2 class="h">{{ 'reports.velocity_title' | translate }}</h2>
          <p class="meta">
            {{ 'reports.velocity_average' | translate }}: <strong>{{ velocity()!.averageCompleted }}</strong>
            <span class="muted">({{ velocity()!.sprints.length }} {{ 'reports.velocity_sprints_label' | translate }})</span>
          </p>
          <svg class="chart" [attr.viewBox]="velocityViewBox()" preserveAspectRatio="none">
            <rect width="100%" height="100%" fill="var(--c-surface)" />
            @for (g of velocityGridLines(); track g) {
              <line [attr.x1]="pad" [attr.y1]="g" [attr.x2]="velocityChartW() - pad" [attr.y2]="g"
                    stroke="var(--c-border)" stroke-width="0.5" />
            }
            @for (b of velocityBars(); track b.id) {
              <!-- committed bar (lighter) -->
              <rect [attr.x]="b.committedX" [attr.y]="b.committedY"
                    [attr.width]="b.barW" [attr.height]="b.committedH"
                    fill="var(--c-text-muted)" opacity="0.45" />
              <!-- completed bar (darker, overlay) -->
              <rect [attr.x]="b.completedX" [attr.y]="b.completedY"
                    [attr.width]="b.barW" [attr.height]="b.completedH"
                    fill="var(--c-text)" />
              <text [attr.x]="b.labelX" [attr.y]="chartH - 2"
                    text-anchor="middle"
                    font-size="9"
                    fill="var(--c-text-muted)">{{ b.shortName }}</text>
            }
          </svg>
          <div class="legend">
            <span class="lg committed"><i></i> {{ 'reports.velocity_committed' | translate }}</span>
            <span class="lg completed"><i></i> {{ 'reports.velocity_completed' | translate }}</span>
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
    .lg.committed i { background: var(--c-text-muted); opacity: 0.45; }
    .lg.completed i { background: var(--c-text); }
    .muted { color: var(--c-text-muted); margin-left: 4px; }
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

  // F7: velocity state
  readonly velocity = signal<VelocityReportDto | null>(null);
  readonly velocityLoading = signal(false);

  readonly pad = 8;
  readonly chartW = 400;
  readonly chartH = 200;
  readonly velocityBarMin = 36; // px per sprint group, scale chart width theo số sprint

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

  // F7: derived geometry — width scales theo số sprint, height fixed.
  readonly velocityChartW = computed(() => {
    const v = this.velocity();
    if (!v?.sprints?.length) return this.chartW;
    return Math.max(this.chartW, v.sprints.length * this.velocityBarMin * 2 + 2 * this.pad);
  });

  readonly velocityViewBox = computed(() => `0 0 ${this.velocityChartW()} ${this.chartH}`);

  readonly velocityGridLines = computed(() => {
    const n = 4;
    const innerH = this.chartH - 2 * this.pad - 12; // bottom 12px reserved cho label sprint
    const lines: number[] = [];
    for (let i = 0; i <= n; i++) lines.push(this.pad + (innerH * i) / n);
    return lines;
  });

  /**
   * Tính rect coords cho mỗi sprint: 1 cặp bar (committed mờ + completed đậm),
   * vẽ overlay (completed nằm trong committed). Tránh confusion với grouped bars.
   */
  readonly velocityBars = computed(() => {
    const v = this.velocity();
    if (!v?.sprints?.length) return [];
    const innerH = this.chartH - 2 * this.pad - 12;
    const fullW = this.velocityChartW();
    const innerW = fullW - 2 * this.pad;
    const slotW = innerW / v.sprints.length;
    const barW = Math.max(slotW * 0.55, 8);

    let maxY = 1;
    for (const s of v.sprints) maxY = Math.max(maxY, s.committed, s.completed);

    return v.sprints.map((s, i) => {
      const cx = this.pad + slotW * i + (slotW - barW) / 2;
      const committedH = innerH * (s.committed / maxY);
      const completedH = innerH * (s.completed / maxY);
      const baseY = this.pad + innerH;
      return {
        id: s.sprintId,
        shortName: s.name.length > 12 ? s.name.slice(0, 11) + '…' : s.name,
        committedX: cx,
        committedY: baseY - committedH,
        committedH,
        // overlay completed: cùng x, height nhỏ hơn → đáy bar trùng đáy committed.
        completedX: cx,
        completedY: baseY - completedH,
        completedH,
        barW,
        labelX: cx + barW / 2,
      };
    });
  });

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
    this.velocityLoading.set(true);
    try {
      const detail = await firstValueFrom(this.api.getDetailForMemberByKey(projectKey));
      this.project.set(detail);
      this.ctx.setProject(projectDetailToSummary(detail));

      // Burndown + Velocity chạy song song để page load nhanh.
      await Promise.all([
        this.loadBurndown(detail.id),
        this.loadVelocity(detail.id),
      ]);
    } catch {
      this.burndown.set(null);
      this.velocity.set(null);
    }
  }

  private async loadBurndown(projectId: string): Promise<void> {
    try {
      let sprintId: string | null = null;
      const active = await firstValueFrom(this.sprintApi.getActive(projectId));
      if (active) sprintId = active.id;
      else {
        const list = await firstValueFrom(this.sprintApi.list(projectId));
        const completed = list
          .filter((s) => s.status === 2)
          .sort((a, b) => new Date(b.endDate).getTime() - new Date(a.endDate).getTime())[0];
        if (completed) sprintId = completed.id;
      }
      if (!sprintId) {
        this.burndown.set(null);
        return;
      }
      const bd = await firstValueFrom(this.sprintApi.burndown(projectId, sprintId));
      this.burndown.set(bd);
    } catch {
      this.burndown.set(null);
    } finally {
      this.burndownLoading.set(false);
    }
  }

  private async loadVelocity(projectId: string): Promise<void> {
    try {
      const v = await firstValueFrom(this.sprintApi.velocity(projectId, 6));
      this.velocity.set(v);
    } catch {
      this.velocity.set(null);
    } finally {
      this.velocityLoading.set(false);
    }
  }
}
