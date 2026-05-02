import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList } from '@shared/models/api-response';
import { APP_CONFIG } from '@core/config/app-config';

export interface IssueAttachmentSummary {
  id: string;
  issueId: string;
  uploadedByUserId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AttachmentApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);

  private base(issueId: string): string {
    return `${this.cfg.apiBaseUrl}/v1/issues/${issueId}/attachments`;
  }

  listByIssue(issueId: string, pageIndex = 1, pageSize = 50): Observable<PagedList<IssueAttachmentSummary>> {
    const params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', pageSize);
    return this.http.get<PagedList<IssueAttachmentSummary>>(this.base(issueId), { params });
  }

  upload(issueId: string, file: File): Observable<IssueAttachmentSummary> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<IssueAttachmentSummary>(this.base(issueId), fd);
  }

  delete(issueId: string, attachmentId: string): Observable<unknown> {
    return this.http.delete(`${this.base(issueId)}/${attachmentId}`);
  }

  downloadBlob(issueId: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${this.base(issueId)}/${attachmentId}/file`, { responseType: 'blob' });
  }
}
