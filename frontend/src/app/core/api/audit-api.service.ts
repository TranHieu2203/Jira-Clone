import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';
import { PagedList } from '@shared/models/api-response';

export interface AuditEntryDto {
  id: string;
  actorUserId: string | null;
  action: string;
  scope: string;
  scopeId: string | null;
  payloadJson: string | null;
  occurredAt: string;
  traceId: string | null;
}

export interface SearchAuditQuery {
  actorUserId?: string | null;
  action?: string | null;
  scope?: string | null;
  scopeId?: string | null;
  from?: string | null;
  to?: string | null;
  pageIndex?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/admin/audit`;

  search(q: SearchAuditQuery): Observable<PagedList<AuditEntryDto>> {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(q)) {
      if (v !== null && v !== undefined && v !== '') {
        params = params.set(k, String(v));
      }
    }
    return this.http.get<PagedList<AuditEntryDto>>(this.base, { params });
  }
}
