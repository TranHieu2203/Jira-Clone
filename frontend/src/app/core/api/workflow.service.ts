import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

// 1=ToDo, 2=InProgress, 3=Done
export type StatusCategory = 1 | 2 | 3;

export interface WorkflowStatus {
  id: string;
  name: string;
  key: string;
  category: StatusCategory;
  color?: string | null;
  order: number;
  isFinal: boolean;
}

export interface TransitionStep {
  id: string;
  typeKey: string;
  configJson: string;
  order: number;
}

export interface WorkflowTransition {
  id: string;
  fromStatusId?: string | null;
  toStatusId: string;
  name: string;
  screenId?: string | null;
  isAutomatic: boolean;
  rules: TransitionStep[];
  validators: TransitionStep[];
  postFunctions: TransitionStep[];
}

export interface Workflow {
  id: string;
  projectId?: string | null;
  name: string;
  key: string;
  description?: string | null;
  isTemplate: boolean;
  isActive: boolean;
  initialStatusId: string;
  statuses: WorkflowStatus[];
  transitions: WorkflowTransition[];
}

export interface AvailableTransition {
  id: string;
  name: string;
  toStatusId: string;
  toStatusName: string;
  screenId?: string | null;
}

export interface CreateWorkflowRequest {
  projectId: string | null;
  name: string;
  key: string;
  description?: string | null;
  isTemplate: boolean;
}

export interface UpdateWorkflowRequest {
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface AddStatusRequest {
  name: string;
  key: string;
  category: number;
  color?: string | null;
  order?: number | null;
}

export interface AddTransitionRequest {
  fromStatusId?: string | null;
  toStatusId: string;
  name: string;
  screenId?: string | null;
  isAutomatic: boolean;
}

@Injectable({ providedIn: 'root' })
export class WorkflowApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/workflows`;
  private readonly transitionBase = `${this.cfg.apiBaseUrl}/v1/transitions`;

  getById(id: string): Observable<Workflow> {
    return this.http.get<Workflow>(`${this.base}/${id}`);
  }

  listByProject(projectId: string): Observable<Workflow[]> {
    return this.http.get<Workflow[]>(`${this.base}/by-project/${projectId}`);
  }

  listTemplates(): Observable<Workflow[]> {
    return this.http.get<Workflow[]>(`${this.base}/templates`);
  }

  getAvailableTransitions(
    projectId: string, issueTypeId: string, currentStatusId: string, currentUserId: string
  ): Observable<AvailableTransition[]> {
    const url = `${this.transitionBase}/available?projectId=${projectId}&issueTypeId=${issueTypeId}&currentStatusId=${currentStatusId}&currentUserId=${currentUserId}`;
    return this.http.get<AvailableTransition[]>(url);
  }

  create(request: CreateWorkflowRequest): Observable<Workflow> {
    return this.http.post<Workflow>(this.base, request);
  }

  update(id: string, request: UpdateWorkflowRequest): Observable<Workflow> {
    return this.http.put<Workflow>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  addStatus(workflowId: string, request: AddStatusRequest): Observable<Workflow> {
    return this.http.post<Workflow>(`${this.base}/${workflowId}/statuses`, request);
  }

  removeStatus(workflowId: string, statusId: string): Observable<Workflow> {
    return this.http.delete<Workflow>(`${this.base}/${workflowId}/statuses/${statusId}`);
  }

  setInitialStatus(workflowId: string, statusId: string): Observable<Workflow> {
    return this.http.put<Workflow>(`${this.base}/${workflowId}/initial-status/${statusId}`, {});
  }

  addTransition(workflowId: string, request: AddTransitionRequest): Observable<Workflow> {
    return this.http.post<Workflow>(`${this.base}/${workflowId}/transitions`, request);
  }

  removeTransition(workflowId: string, transitionId: string): Observable<Workflow> {
    return this.http.delete<Workflow>(`${this.base}/${workflowId}/transitions/${transitionId}`);
  }
}
