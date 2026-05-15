import { api } from '@/lib/api';

export type PlaceholderPattern = 'dots' | 'underscores' | 'brackets' | 'guillemets';

export interface DetectedPlaceholder {
  text: string;
  pattern: PlaceholderPattern;
  charOffset: number;
}

export interface TemplateImportResult {
  placeholders: DetectedPlaceholder[];
  docxBase64: string;
}

export interface TemplateSummary {
  id: string;
  code: string;
  name: string;
  category: string | null;
  version: number;
  status: number;
  usedFieldsCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface TemplateDetail {
  id: string;
  code: string;
  name: string;
  category: string | null;
  usedFields: string[];
  version: number;
  status: number;
  hasOriginalDocx: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateTemplateRequest {
  code: string;
  name: string;
  category?: string | null;
  docxBase64: string;
  usedFields?: string[];
}

const base = '/v1/form-management/templates';

export const templateApi = {
  search: (keyword?: string) => {
    const q = keyword ? `?keyword=${encodeURIComponent(keyword)}` : '';
    return api.get<TemplateSummary[]>(`${base}${q}`);
  },
  getById: (id: string) => api.get<TemplateDetail>(`${base}/${id}`),
  create: (body: CreateTemplateRequest) => api.post<TemplateDetail>(base, body),
  importFromWord: (file: File) => {
    const fd = new FormData();
    fd.append('file', file);
    return api.post<TemplateImportResult>(`${base}/import`, fd);
  },
  /** Public URL OnlyOffice DocServer fetch DOCX bytes. KHÔNG cần auth. */
  fileUrl: (id: string) => `/api/v1/form-management/templates/${id}/file`,
  callbackUrl: (id: string) => `/api/v1/form-management/templates/${id}/callback`,
  /**
   * Yêu cầu BE proxy lên DocServer CommandService.ashx với c="forcesave" — DS sẽ
   * fire callback status=6 đến /callback giống như khi user bấm Save trong toolbar OO.
   * Response: { dsStatus, dsBody } trong đó dsBody parse được JSON dạng { error: N }.
   *   error 0 = accepted (callback sẽ fire async ~500-2000ms sau).
   *   error 4 = no changes (doc đã saved sạch, không cần fire).
   *   error 1 = key not found (docKey không có session active).
   */
  triggerSave: (id: string, docKey: string) =>
    api.post<{ dsStatus: number; dsBody: string }>(
      `${base}/${id}/trigger-save`,
      { docKey }
    ),
};
