import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';
import { PagedList } from '@shared/models/api-response';

export interface InAppNotificationRow {
  id: string;
  type: string;
  payload: Record<string, unknown>;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class InAppNotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/notifications`;

  list(pageIndex = 1, pageSize = 20, unreadOnly = false): Observable<PagedList<InAppNotificationRow>> {
    const params = new HttpParams()
      .set('pageIndex', String(pageIndex))
      .set('pageSize', String(pageSize))
      .set('unreadOnly', unreadOnly ? 'true' : 'false');
    return this.http.get<PagedList<InAppNotificationRow>>(this.base, { params });
  }

  unreadCount(): Observable<number> {
    return this.http.get<number>(`${this.base}/unread-count`);
  }

  markRead(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/${id}/read`, {});
  }

  markAllRead(): Observable<unknown> {
    return this.http.post(`${this.base}/read-all`, {});
  }
}
