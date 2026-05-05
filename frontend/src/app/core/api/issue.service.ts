import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList } from '@shared/models/api-response';
import { APP_CONFIG } from '@core/config/app-config';

// 1=Lowest..5=Highest
export type IssuePriority = 1 | 2 | 3 | 4 | 5;

export interface IssueSummary {
  id: string;
  projectId: string;
  key: string;
  issueTypeId: string;
  currentStatusId: string;
  summary: string;
  priority: IssuePriority;
  assigneeId?: string | null;
  createdAt: string;
  parentIssueId?: string | null;
  storyPoints?: number | null;
  labels?: string[] | null;
}

export interface Issue {
  id: string;
  projectId: string;
  key: string;
  number: number;
  issueTypeId: string;
  workflowId: string;
  currentStatusId: string;
  summary: string;
  description?: string | null;
  priority: IssuePriority;
  reporterId: string;
  assigneeId?: string | null;
  parentIssueId?: string | null;
  labels: string[];
  dueDate?: string | null;
  storyPoints?: number | null;
  originalEstimateMinutes?: number | null;
  remainingEstimateMinutes?: number | null;
  timeSpentMinutes?: number | null;
  isArchived: boolean;
  watchers: string[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateIssueRequest {
  projectId: string;
  issueTypeId: string;
  summary: string;
  description?: string | null;
  priority?: IssuePriority | null;
  assigneeId?: string | null;
  parentIssueId?: string | null;
  dueDate?: string | null;
  storyPoints?: number | null;
  labels?: string[] | null;
  customFieldValues?: Record<string, unknown> | null;
}

export interface UpdateIssueRequest {
  summary: string;
  description?: string | null;
  priority?: IssuePriority | null;
  assigneeId?: string | null;
  parentIssueId?: string | null;
  dueDate?: string | null;
  storyPoints?: number | null;
  labels?: string[] | null;
  originalEstimateMinutes?: number | null;
  remainingEstimateMinutes?: number | null;
  timeSpentMinutes?: number | null;
}

export interface IssueFieldFilterRequest {
  customFieldId: string;
  indexedStringEquals?: string | null;
  indexedNumberEquals?: number | null;
  indexedDateEquals?: string | null;
}

export interface SearchIssuesRequest {
  projectId?: string | null;
  issueTypeId?: string | null;
  assigneeId?: string | null;
  reporterId?: string | null;
  currentStatusId?: string | null;
  priority?: number | null;
  textSearch?: string | null;
  includeArchived?: boolean | null;
  pageIndex?: number;
  pageSize?: number;
  sort?: string | null;
  jql?: string | null;
  fieldFilters?: IssueFieldFilterRequest[] | null;
  /** Filter to these issue IDs (AND with field filters if present). */
  issueIds?: string[] | null;
  /** Exclude these issue IDs from results. */
  excludeIssueIds?: string[] | null;
}

export interface TransitionIssueRequest {
  transitionId: string;
  inputs?: Record<string, unknown> | null;
  comment?: string | null;
}

// ── F5: Bulk edit ───────────────────────────────────────────────

export interface BulkUpdateOperationsDto {
  assigneeId?: string | null;
  clearAssignee?: boolean;
  priority?: number | null;
  addLabels?: string[] | null;
  removeLabels?: string[] | null;
  archive?: boolean | null;
}

export interface BulkUpdateRequest {
  issueIds: string[];
  operations: BulkUpdateOperationsDto;
}

export interface BulkUpdateFailureDto {
  issueId: string;
  messageKey: string;
}

export interface BulkUpdateResultDto {
  succeeded: string[];
  failed: BulkUpdateFailureDto[];
}

@Injectable({ providedIn: 'root' })
export class IssueApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/issues`;

  getById(id: string): Observable<Issue> {
    return this.http.get<Issue>(`${this.base}/${id}`);
  }

  getByKey(issueKey: string): Observable<Issue> {
    return this.http.get<Issue>(`${this.base}/by-key/${issueKey}`);
  }

  search(req: SearchIssuesRequest): Observable<PagedList<IssueSummary>> {
    return this.http.post<PagedList<IssueSummary>>(`${this.base}/search`, req);
  }

  listChildren(id: string): Observable<IssueSummary[]> {
    return this.http.get<IssueSummary[]>(`${this.base}/${id}/children`);
  }

  create(req: CreateIssueRequest): Observable<Issue> {
    return this.http.post<Issue>(this.base, req);
  }

  update(id: string, req: UpdateIssueRequest): Observable<Issue> {
    return this.http.put<Issue>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  transition(id: string, req: TransitionIssueRequest): Observable<Issue> {
    return this.http.post<Issue>(`${this.base}/${id}/transition`, req);
  }

  addWatcher(id: string, userId: string): Observable<Issue> {
    return this.http.post<Issue>(`${this.base}/${id}/watchers/${userId}`, {});
  }

  removeWatcher(id: string, userId: string): Observable<Issue> {
    return this.http.delete<Issue>(`${this.base}/${id}/watchers/${userId}`);
  }

  /** F5: Bulk update — apply ops to many issues in 1 request. */
  bulkUpdate(req: BulkUpdateRequest): Observable<BulkUpdateResultDto> {
    return this.http.post<BulkUpdateResultDto>(`${this.base}/bulk-update`, req);
  }

  /**
   * F8: Export search results as CSV. Trả Blob; UI tạo URL.createObjectURL để download.
   * Server cap 5000 rows — narrow filter nếu vượt.
   */
  exportCsv(req: SearchIssuesRequest): Observable<Blob> {
    return this.http.post(`${this.base}/export.csv`, req, { responseType: 'blob' });
  }
}
