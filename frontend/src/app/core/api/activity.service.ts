import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList } from '@shared/models/api-response';
import { APP_CONFIG } from '@core/config/app-config';

export interface ActivityItem {
  id: string;
  issueId: string;
  occurredAt: string;
  kind: string;
  actorUserId: string | null;
  payload: Record<string, unknown> | null;
}

@Injectable({ providedIn: 'root' })
export class ActivityApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/activity`;

  listByIssue(issueId: string, pageIndex = 1, pageSize = 50): Observable<PagedList<ActivityItem>> {
    const params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', pageSize);
    return this.http.get<PagedList<ActivityItem>>(`${this.base}/by-issue/${issueId}`, { params });
  }
}
