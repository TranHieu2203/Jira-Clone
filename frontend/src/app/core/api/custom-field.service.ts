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

@Injectable({ providedIn: 'root' })
export class CustomFieldApiService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/custom-fields`;
  private readonly valueBase = `${this.cfg.apiBaseUrl}/v1/issue-field-values`;

  list(): Observable<CustomField[]> {
    return this.http.get<CustomField[]>(this.base);
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
