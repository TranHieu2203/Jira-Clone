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
  /** BE Phase 6 trả rỗng — FE load file qua Syncfusion client-side. Phase 7 sẽ điền SFDT. */
  sfdtContent: string;
  placeholders: DetectedPlaceholder[];
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
}
