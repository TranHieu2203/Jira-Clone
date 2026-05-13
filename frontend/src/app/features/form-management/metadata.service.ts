import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';
import {
  CreateMetadataRequest,
  MetadataDto,
  UpdateMetadataRequest
} from './metadata.model';

@Injectable({ providedIn: 'root' })
export class MetadataService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/form-management/metadata`;

  search(keyword?: string, group?: string): Observable<MetadataDto[]> {
    let params = new HttpParams();
    if (keyword?.trim()) params = params.set('keyword', keyword.trim());
    if (group?.trim()) params = params.set('group', group.trim());
    return this.http.get<MetadataDto[]>(this.base, { params });
  }

  getById(id: string): Observable<MetadataDto> {
    return this.http.get<MetadataDto>(`${this.base}/${id}`);
  }

  create(body: CreateMetadataRequest): Observable<MetadataDto> {
    return this.http.post<MetadataDto>(this.base, body);
  }

  update(id: string, body: UpdateMetadataRequest): Observable<MetadataDto> {
    return this.http.put<MetadataDto>(`${this.base}/${id}`, body);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
