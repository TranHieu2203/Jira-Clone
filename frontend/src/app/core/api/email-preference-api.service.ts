import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export interface EmailPreferenceDto {
  userId: string;
  noAssignee: boolean;
  noStatus: boolean;
  noComment: boolean;
  noMention: boolean;
}

export interface UpdateEmailPreferenceRequest {
  noAssignee: boolean;
  noStatus: boolean;
  noComment: boolean;
  noMention: boolean;
}

@Injectable({ providedIn: 'root' })
export class EmailPreferenceApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/me/email-preferences`;

  getMine(): Observable<EmailPreferenceDto> {
    return this.http.get<EmailPreferenceDto>(this.base);
  }

  updateMine(req: UpdateEmailPreferenceRequest): Observable<EmailPreferenceDto> {
    return this.http.put<EmailPreferenceDto>(this.base, req);
  }
}
