import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export interface UserSummary {
  id: string;
  userName: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/users`;

  search(query: string, take = 20): Observable<UserSummary[]> {
    const params = new HttpParams().set('q', query).set('take', String(take));
    return this.http.get<UserSummary[]>(`${this.base}/search`, { params });
  }

  getById(id: string): Observable<UserSummary> {
    return this.http.get<UserSummary>(`${this.base}/${id}`);
  }
}
