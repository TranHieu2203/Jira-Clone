export interface ApiError {
  code: string;
  messageKey: string;
  field?: string | null;
  args?: Record<string, unknown> | null;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  messageKey: string | null;
  messageArgs: Record<string, unknown> | null;
  errors: ApiError[] | null;
  traceId: string;
  timestamp: string;
}

export interface PagedList<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export class ApiException extends Error {
  override readonly name = 'ApiException';
  constructor(
    public readonly messageKey: string | null,
    public readonly errors: ApiError[],
    public readonly traceId: string,
    public readonly status: number
  ) {
    super(messageKey ?? 'system.unexpected');
  }
}
