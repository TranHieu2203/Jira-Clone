import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

// 1=Owner, 2=Admin, 3=Member
export type WorkspaceRole = 1 | 2 | 3;

export interface WorkspaceMember {
  userId: string;
  role: WorkspaceRole;
  joinedAt: string;
}

export interface Workspace {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  avatarUrl?: string | null;
  ownerId: string;
  memberCount: number;
  createdAt: string;
}

export interface WorkspaceDetail extends Omit<Workspace, 'memberCount'> {
  members: WorkspaceMember[];
}

export interface CreateWorkspaceRequest {
  name: string;
  slug: string;
  description?: string | null;
  avatarUrl?: string | null;
}

export interface UpdateWorkspaceRequest {
  name: string;
  description?: string | null;
  avatarUrl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/workspaces`;

  listMine(): Observable<Workspace[]> {
    return this.http.get<Workspace[]>(`${this.base}/mine`);
  }

  getById(id: string): Observable<WorkspaceDetail> {
    return this.http.get<WorkspaceDetail>(`${this.base}/${id}`);
  }

  getBySlug(slug: string): Observable<WorkspaceDetail> {
    return this.http.get<WorkspaceDetail>(`${this.base}/by-slug/${slug}`);
  }

  create(req: CreateWorkspaceRequest): Observable<WorkspaceDetail> {
    return this.http.post<WorkspaceDetail>(this.base, req);
  }

  update(id: string, req: UpdateWorkspaceRequest): Observable<WorkspaceDetail> {
    return this.http.put<WorkspaceDetail>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  addMember(id: string, userId: string, role: WorkspaceRole): Observable<WorkspaceDetail> {
    return this.http.post<WorkspaceDetail>(`${this.base}/${id}/members`, { userId, role });
  }

  removeMember(id: string, userId: string): Observable<WorkspaceDetail> {
    return this.http.delete<WorkspaceDetail>(`${this.base}/${id}/members/${userId}`);
  }

  changeRole(id: string, userId: string, role: WorkspaceRole): Observable<WorkspaceDetail> {
    return this.http.put<WorkspaceDetail>(`${this.base}/${id}/members/${userId}/role`, { role });
  }
}
