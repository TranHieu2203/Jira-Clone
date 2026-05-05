import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export interface SprintDto {
  id: string;
  projectId: string;
  name: string;
  goal?: string | null;
  startDate: string;
  endDate: string;
  status: number;
  orderedIssueIds: string[];
}

export interface CreateSprintRequest {
  name: string;
  startDate: string;
  endDate: string;
  goal?: string | null;
}

export interface UpdateSprintRequest {
  name: string;
  startDate: string;
  endDate: string;
  goal?: string | null;
}

export interface ReorderSprintIssuesRequest {
  issueIds: string[];
}

export interface BurndownDayDto {
  date: string;
  idealRemaining: number;
  actualRemaining: number;
}

export interface SprintBurndownDto {
  sprintId: string;
  totalPoints: number;
  days: BurndownDayDto[];
}

/** F7: 1 entry trong velocity chart. */
export interface SprintVelocityEntryDto {
  sprintId: string;
  name: string;
  startDate: string;
  endDate: string;
  committed: number;
  completed: number;
}

export interface VelocityReportDto {
  projectId: string;
  sprints: SprintVelocityEntryDto[];
  averageCompleted: number;
}

@Injectable({ providedIn: 'root' })
export class SprintApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);

  private base(projectId: string): string {
    return `${this.cfg.apiBaseUrl}/v1/projects/${projectId}/sprints`;
  }

  list(projectId: string): Observable<SprintDto[]> {
    return this.http.get<SprintDto[]>(this.base(projectId));
  }

  getActive(projectId: string): Observable<SprintDto | null> {
    return this.http.get<SprintDto | null>(`${this.base(projectId)}/active`);
  }

  getById(projectId: string, sprintId: string): Observable<SprintDto> {
    return this.http.get<SprintDto>(`${this.base(projectId)}/${sprintId}`);
  }

  burndown(projectId: string, sprintId: string): Observable<SprintBurndownDto> {
    return this.http.get<SprintBurndownDto>(`${this.base(projectId)}/${sprintId}/burndown`);
  }

  /** F7: lịch sử velocity của project — last N completed sprint. */
  velocity(projectId: string, count = 6): Observable<VelocityReportDto> {
    return this.http.get<VelocityReportDto>(`${this.base(projectId)}/velocity?count=${count}`);
  }

  create(projectId: string, body: CreateSprintRequest): Observable<SprintDto> {
    return this.http.post<SprintDto>(this.base(projectId), body);
  }

  update(projectId: string, sprintId: string, body: UpdateSprintRequest): Observable<SprintDto> {
    return this.http.put<SprintDto>(`${this.base(projectId)}/${sprintId}`, body);
  }

  start(projectId: string, sprintId: string): Observable<SprintDto> {
    return this.http.post<SprintDto>(`${this.base(projectId)}/${sprintId}/start`, {});
  }

  complete(projectId: string, sprintId: string): Observable<SprintDto> {
    return this.http.post<SprintDto>(`${this.base(projectId)}/${sprintId}/complete`, {});
  }

  addIssue(projectId: string, sprintId: string, issueId: string): Observable<SprintDto> {
    return this.http.post<SprintDto>(`${this.base(projectId)}/${sprintId}/issues/${issueId}`, {});
  }

  removeIssue(projectId: string, sprintId: string, issueId: string): Observable<unknown> {
    return this.http.delete(`${this.base(projectId)}/${sprintId}/issues/${issueId}`);
  }

  reorderIssues(projectId: string, sprintId: string, body: ReorderSprintIssuesRequest): Observable<SprintDto> {
    return this.http.put<SprintDto>(`${this.base(projectId)}/${sprintId}/issues/order`, body);
  }
}
