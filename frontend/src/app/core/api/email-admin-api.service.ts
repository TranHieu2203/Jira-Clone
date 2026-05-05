import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { PagedList } from '@shared/models/api-response';

export interface EmailTemplateRow {
  id: string;
  key: string;
  name: string;
  subjectTemplate: string;
  htmlBodyTemplate: string;
  textBodyTemplate: string | null;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertEmailTemplateBody {
  key: string;
  name: string;
  subjectTemplate: string;
  htmlBodyTemplate: string;
  textBodyTemplate: string | null;
  isEnabled: boolean;
}

export interface EmailLogRow {
  id: string;
  templateKey: string;
  toEmail: string;
  status: number;
  provider: string;
  providerMessageId: string | null;
  subjectRendered: string;
  bodyPreview: string;
  error: string | null;
  createdAt: string;
  sentAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class EmailAdminApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);

  private readonly templatesBase = `${this.cfg.apiBaseUrl}/v1/admin/email-templates`;
  private readonly logsBase = `${this.cfg.apiBaseUrl}/v1/admin/email-logs`;

  listTemplates(pageIndex = 1, pageSize = 50, q?: string): Observable<PagedList<EmailTemplateRow>> {
    let params = new HttpParams().set('pageIndex', String(pageIndex)).set('pageSize', String(pageSize));
    if (q?.trim()) params = params.set('q', q.trim());
    return this.http.get<PagedList<EmailTemplateRow>>(this.templatesBase, { params });
  }

  getTemplate(key: string): Observable<EmailTemplateRow> {
    return this.http.get<EmailTemplateRow>(`${this.templatesBase}/${encodeURIComponent(key)}`);
  }

  upsertTemplate(body: UpsertEmailTemplateBody): Observable<EmailTemplateRow> {
    return this.http.put<EmailTemplateRow>(this.templatesBase, body);
  }

  listLogs(
    pageIndex = 1,
    pageSize = 50,
    templateKey?: string,
    toEmail?: string,
    status?: string
  ): Observable<PagedList<EmailLogRow>> {
    let params = new HttpParams().set('pageIndex', String(pageIndex)).set('pageSize', String(pageSize));
    if (templateKey?.trim()) params = params.set('templateKey', templateKey.trim());
    if (toEmail?.trim()) params = params.set('toEmail', toEmail.trim());
    if (status?.trim()) params = params.set('status', status.trim());
    return this.http.get<PagedList<EmailLogRow>>(this.logsBase, { params });
  }
}
