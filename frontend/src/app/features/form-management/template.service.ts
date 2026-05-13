import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config';

export type PlaceholderPattern = 'dots' | 'underscores' | 'brackets' | 'guillemets';

export interface DetectedPlaceholder {
  text: string;
  pattern: PlaceholderPattern;
  charOffset: number;
}

export interface TemplateImportResult {
  /** SFDT JSON — FE truyền vào documentEditor.open(). */
  sfdtContent: string;
  placeholders: DetectedPlaceholder[];
  /** DOCX gốc encoded base64 — giữ trong component state, gửi khi save template để mail-merge sau preserve formatting/watermark. */
  docxBase64?: string;
  /** Text watermark extracted từ DocIO — render CSS overlay vì Syncfusion DocumentEditor không hiển thị. */
  watermarkText?: string | null;
}

export interface CreateTemplateRequest {
  code: string;
  name: string;
  category?: string | null;
  sfdtContent: string;
  usedFields?: string[];
  /** DOCX gốc base64 (từ TemplateImportResult.docxBase64). BE persist để mail-merge keep watermark. */
  docxBase64?: string | null;
}

export interface TemplateDetail {
  id: string;
  code: string;
  name: string;
  category: string | null;
  sfdtContent: string;
  usedFields: string[];
  version: number;
  status: number;
  hasOriginalDocx: boolean;
  createdAt: string;
  updatedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/form-management/templates`;

  /** Upload .docx / Word 2003 .xml để detect placeholders ở BE. */
  importFromWord(file: File): Observable<TemplateImportResult> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<TemplateImportResult>(`${this.base}/import`, fd);
  }

  create(body: CreateTemplateRequest): Observable<TemplateDetail> {
    return this.http.post<TemplateDetail>(this.base, body);
  }
}
