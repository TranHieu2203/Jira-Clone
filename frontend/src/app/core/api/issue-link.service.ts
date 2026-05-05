import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

/**
 * Loại link Jira-style. Số là enum integer ở BE (xem `IssueLink.Domain.IssueLinkType`).
 */
export enum IssueLinkType {
  RelatesTo = 1,
  Blocks = 10,
  Duplicates = 20,
  Clones = 30,
  Causes = 40,
}

export interface IssueLinkDto {
  id: string;
  sourceIssueId: string;
  targetIssueId: string;
  linkType: IssueLinkType;
  linkTypeKey: string;
  createdAt: string;
}

export interface IssueLinksForIssueDto {
  issueId: string;
  outgoing: IssueLinkDto[];
  incoming: IssueLinkDto[];
}

export interface CreateIssueLinkRequest {
  sourceIssueId: string;
  targetIssueId: string;
  linkType: IssueLinkType;
}

@Injectable({ providedIn: 'root' })
export class IssueLinkApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/issue-links`;

  listByIssue(issueId: string): Observable<IssueLinksForIssueDto> {
    return this.http.get<IssueLinksForIssueDto>(`${this.base}/by-issue/${issueId}`);
  }

  create(request: CreateIssueLinkRequest): Observable<IssueLinkDto> {
    return this.http.post<IssueLinkDto>(this.base, request);
  }

  delete(linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${linkId}`);
  }
}
