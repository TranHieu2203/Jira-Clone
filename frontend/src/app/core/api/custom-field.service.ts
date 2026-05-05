import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export type CustomFieldType =
  | 1 | 2     // Text, TextArea
  | 3 | 4     // Number, Decimal
  | 5 | 6     // Date, DateTime
  | 10 | 11 | 12  // Select, MultiSelect, Cascading
  | 20 | 21       // User, UserMulti
  | 30 | 31 | 32  // Checkbox, Url, Label
  | 99;

export interface CustomFieldOption {
  id: string;
  parentOptionId?: string | null;
  value: string;
  label: string;
  order: number;
  isDisabled: boolean;
}

export interface CustomFieldContext {
  id: string;
  name: string;
  isGlobal: boolean;
  isRequired: boolean;
  defaultValueJson?: string | null;
  projectIds: string[];
  issueTypeIds: string[];
  /** Thứ tự hiển thị trên form (screen layout); BE đã sort resolve theo field. */
  displayOrder: number;
}

export interface CustomField {
  id: string;
  key: string;
  name: string;
  description?: string | null;
  type: CustomFieldType;
  isSystem: boolean;
  isSearchable: boolean;
  configJson: string;
  options: CustomFieldOption[];
  contexts: CustomFieldContext[];
  createdAt: string;
}

export interface IssueFieldValue {
  customFieldId: string;
  fieldKey: string;
  type: CustomFieldType;
  value: unknown;
}

export interface CreateCustomFieldRequest {
  key: string;
  name: string;
  type: number;
  description?: string | null;
  isSearchable: boolean;
  configJson?: string | null;
}

export interface UpdateCustomFieldRequest {
  name: string;
  description?: string | null;
  isSearchable: boolean;
  configJson?: string | null;
}

export interface AddOptionRequest {
  value: string;
  label: string;
  parentOptionId?: string | null;
  order?: number | null;
}

export interface UpdateOptionRequest {
  value: string;
  label: string;
  order: number;
}

export interface AddContextRequest {
  name: string;
  isGlobal: boolean;
  isRequired: boolean;
  defaultValueJson?: string | null;
  projectIds?: string[] | null;
  issueTypeIds?: string[] | null;
  displayOrder?: number | null;
}

@Injectable({ providedIn: 'root' })
export class CustomFieldApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/custom-fields`;
  private readonly valueBase = `${this.cfg.apiBaseUrl}/v1/issue-field-values`;
  private readonly demoBase = `${this.cfg.apiBaseUrl}/v1/custom-fields/demo`;

  list(): Observable<CustomField[]> {
    return this.http.get<CustomField[]>(this.base);
  }

  getById(id: string): Observable<CustomField> {
    return this.http.get<CustomField>(`${this.base}/${id}`);
  }

  create(request: CreateCustomFieldRequest): Observable<CustomField> {
    return this.http.post<CustomField>(this.base, request);
  }

  update(id: string, request: UpdateCustomFieldRequest): Observable<CustomField> {
    return this.http.put<CustomField>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  addOption(fieldId: string, request: AddOptionRequest): Observable<CustomField> {
    return this.http.post<CustomField>(`${this.base}/${fieldId}/options`, request);
  }

  updateOption(fieldId: string, optionId: string, request: UpdateOptionRequest): Observable<CustomField> {
    return this.http.put<CustomField>(`${this.base}/${fieldId}/options/${optionId}`, request);
  }

  removeOption(fieldId: string, optionId: string): Observable<CustomField> {
    return this.http.delete<CustomField>(`${this.base}/${fieldId}/options/${optionId}`);
  }

  addContext(fieldId: string, request: AddContextRequest): Observable<CustomField> {
    return this.http.post<CustomField>(`${this.base}/${fieldId}/contexts`, request);
  }

  removeContext(fieldId: string, contextId: string): Observable<CustomField> {
    return this.http.delete<CustomField>(`${this.base}/${fieldId}/contexts/${contextId}`);
  }

  bindDemoContextsToProject(projectId: string): Observable<unknown> {
    return this.http.post(`${this.demoBase}/bind-project/${projectId}`, {});
  }

  resolve(projectId: string, issueTypeId: string): Observable<CustomField[]> {
    return this.http.get<CustomField[]>(`${this.base}/resolve?projectId=${projectId}&issueTypeId=${issueTypeId}`);
  }

  listValues(issueId: string): Observable<IssueFieldValue[]> {
    return this.http.get<IssueFieldValue[]>(`${this.valueBase}/${issueId}`);
  }

  setValues(issueId: string, projectId: string, issueTypeId: string, values: { customFieldId: string; value: unknown }[]): Observable<unknown> {
    return this.http.put(this.valueBase, { issueId, projectId, issueTypeId, values });
  }
}
