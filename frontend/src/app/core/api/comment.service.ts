import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList } from '@shared/models/api-response';
import { APP_CONFIG } from '@core/config/app-config';

export interface Comment {
  id: string;
  issueId: string;
  authorId: string;
  body: string;
  mentions: string[];
  isEdited: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateCommentRequest {
  issueId: string;
  body: string;
}

export interface UpdateCommentRequest {
  body: string;
}

@Injectable({ providedIn: 'root' })
export class CommentApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/comments`;

  listByIssue(issueId: string, pageIndex = 1, pageSize = 50): Observable<PagedList<Comment>> {
    const params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', pageSize);
    return this.http.get<PagedList<Comment>>(`${this.base}/by-issue/${issueId}`, { params });
  }

  create(req: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(this.base, req);
  }

  update(id: string, req: UpdateCommentRequest): Observable<Comment> {
    return this.http.put<Comment>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
