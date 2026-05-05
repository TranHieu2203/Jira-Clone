import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export interface SavedFilterDto {
  id: string;
  ownerUserId: string;
  name: string;
  jql: string;
  description: string | null;
  isShared: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateSavedFilterRequest {
  name: string;
  jql: string;
  description?: string | null;
  isShared: boolean;
}

export interface UpdateSavedFilterRequest extends CreateSavedFilterRequest {}

@Injectable({ providedIn: 'root' })
export class SavedFilterApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/saved-filters`;

  /** Filter của current user + filter shared bởi user khác. */
  listMine(): Observable<SavedFilterDto[]> {
    return this.http.get<SavedFilterDto[]>(`${this.base}/mine`);
  }

  getById(id: string): Observable<SavedFilterDto> {
    return this.http.get<SavedFilterDto>(`${this.base}/${id}`);
  }

  create(request: CreateSavedFilterRequest): Observable<SavedFilterDto> {
    return this.http.post<SavedFilterDto>(this.base, request);
  }

  update(id: string, request: UpdateSavedFilterRequest): Observable<SavedFilterDto> {
    return this.http.put<SavedFilterDto>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
