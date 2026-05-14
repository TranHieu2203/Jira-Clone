import { useAuthStore } from '@/stores/auth';

export interface CreateSubmissionRequest {
  templateId: string;
  data: Record<string, unknown>;
  exportFormat: number; // 2 = Docx, 3 = Pdf (ExportFormat enum BE)
}

/**
 * Mail-merge endpoint trả file binary trực tiếp (không phải ApiResponse JSON). Dùng fetch raw.
 */
export async function submitAndExport(req: CreateSubmissionRequest): Promise<Blob> {
  const token = useAuthStore.getState().accessToken;
  const r = await fetch('/api/v1/form-management/submissions/submit-and-export', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(req),
  });
  if (!r.ok) {
    let msg = `HTTP ${r.status}`;
    try {
      const j = await r.json();
      msg = j?.messageKey ?? j?.errors?.[0]?.messageKey ?? msg;
    } catch { /* not JSON */ }
    throw new Error(msg);
  }
  return r.blob();
}
