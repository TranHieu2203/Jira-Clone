import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

// 1=Admin, 2=Member, 3=Viewer
export type ProjectRole = 1 | 2 | 3;
// 1=Scrum, 2=Kanban
export type ProjectType = 1 | 2;

export interface ProjectMember {
  userId: string;
  role: ProjectRole;
  joinedAt: string;
}

export interface IssueType {
  id: string;
  name: string;
  key: string;
  icon?: string | null;
  color?: string | null;
  order: number;
  isSubtask: boolean;
  isSystem: boolean;
}

export interface ProjectSummary {
  id: string;
  workspaceId: string;
  name: string;
  key: string;
  description?: string | null;
  avatarUrl?: string | null;
  leadId: string;
  type: ProjectType;
  isArchived: boolean;
  memberCount: number;
  issueTypeCount: number;
  createdAt: string;
}

export interface ProjectDetail extends Omit<ProjectSummary, 'memberCount' | 'issueTypeCount'> {
  members: ProjectMember[];
  issueTypes: IssueType[];
}

export interface CreateProjectRequest {
  workspaceId: string;
  name: string;
  key: string;
  leadId: string;
  type: ProjectType;
  description?: string | null;
}

export interface UpdateProjectRequest {
  name: string;
  description?: string | null;
  avatarUrl?: string | null;
}

/** Map detail → summary for workspace context / lists (counts from nested arrays). */
export function projectDetailToSummary(d: ProjectDetail): ProjectSummary {
  return {
    id: d.id,
    workspaceId: d.workspaceId,
    name: d.name,
    key: d.key,
    description: d.description ?? null,
    avatarUrl: d.avatarUrl ?? null,
    leadId: d.leadId,
    type: d.type,
    isArchived: d.isArchived,
    memberCount: d.members.length,
    issueTypeCount: d.issueTypes.length,
    createdAt: d.createdAt
  };
}

@Injectable({ providedIn: 'root' })
export class ProjectApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/projects`;

  listMine(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/mine`);
  }

  listByWorkspace(workspaceId: string): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/by-workspace/${workspaceId}`);
  }

  getById(id: string): Observable<ProjectDetail> {
    return this.http.get<ProjectDetail>(`${this.base}/${id}`);
  }

  /** Member-scoped: resolves project key across workspaces the user belongs to (409 if ambiguous). */
  getDetailForMemberByKey(key: string): Observable<ProjectDetail> {
    const k = encodeURIComponent(key.trim());
    return this.http.get<ProjectDetail>(`${this.base}/by-key/${k}`);
  }

  getByKey(workspaceId: string, key: string): Observable<ProjectDetail> {
    return this.http.get<ProjectDetail>(
      `${this.base}/by-key/${workspaceId}/${encodeURIComponent(key.trim())}`
    );
  }

  create(req: CreateProjectRequest): Observable<ProjectDetail> {
    return this.http.post<ProjectDetail>(this.base, req);
  }

  update(id: string, req: UpdateProjectRequest): Observable<ProjectDetail> {
    return this.http.put<ProjectDetail>(`${this.base}/${id}`, req);
  }

  archive(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/${id}/archive`, {});
  }

  unarchive(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/${id}/unarchive`, {});
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  addMember(id: string, userId: string, role: ProjectRole): Observable<ProjectDetail> {
    return this.http.post<ProjectDetail>(`${this.base}/${id}/members`, { userId, role });
  }

  removeMember(id: string, userId: string): Observable<ProjectDetail> {
    return this.http.delete<ProjectDetail>(`${this.base}/${id}/members/${userId}`);
  }
}
