// Centralized API client — wraps fetch + interceptor pattern.
// All endpoints proxy qua /api (vite.config.ts) → BE localhost:5000 trong dev.

import { useAuthStore } from '@/stores/auth';

const API_BASE = '/api';

export class ApiError extends Error {
  constructor(public status: number, public code: string | null, public messageKey: string | null, public traceId: string | null) {
    super(messageKey ?? code ?? `HTTP ${status}`);
  }
}

interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  messageKey: string | null;
  messageArgs: Record<string, unknown> | null;
  errors: { code: string; messageKey: string; field?: string }[] | null;
  traceId: string;
  timestamp: string;
}

/**
 * Core request helper. Auth token tự attach từ store. Unwrap `ApiResponse<T>` → emit `data` hoặc throw.
 */
export async function apiRequest<T>(
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (!(init.body instanceof FormData) && init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  const token = useAuthStore.getState().accessToken;
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const resp = await fetch(`${API_BASE}${path}`, { ...init, headers });
  const text = await resp.text();
  let parsed: ApiResponse<T> | T;
  try { parsed = text ? JSON.parse(text) : ({} as T); }
  catch { throw new ApiError(resp.status, null, null, resp.headers.get('x-trace-id')); }

  if (typeof parsed === 'object' && parsed !== null && 'success' in parsed) {
    const r = parsed as ApiResponse<T>;
    if (!r.success) {
      throw new ApiError(resp.status, r.errors?.[0]?.code ?? null, r.messageKey ?? r.errors?.[0]?.messageKey ?? null, r.traceId);
    }
    return r.data as T;
  }
  if (!resp.ok) throw new ApiError(resp.status, null, null, resp.headers.get('x-trace-id'));
  return parsed as T;
}

export const api = {
  get: <T>(path: string) => apiRequest<T>(path),
  post: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, { method: 'POST', body: body instanceof FormData ? body : body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, { method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => apiRequest<T>(path, { method: 'DELETE' }),
};
