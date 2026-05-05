import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { ApiException, ApiResponse } from '@shared/models/api-response';
import { NotificationService } from '../notification/notification.service';
import { AuthService } from '../auth/auth.service';

const TRACE_HEADER = 'X-Trace-Id';
const LANG_STORAGE_KEY = 'app.lang';

export const traceIdInterceptor: HttpInterceptorFn = (req, next) => {
  const lang = (localStorage.getItem(LANG_STORAGE_KEY) as 'vi' | 'en' | null)
    ?? (document.documentElement.lang as 'vi' | 'en' | '')
    ?? 'vi';
  return next(req.clone({ setHeaders: { 'Accept-Language': lang || 'vi' } }));
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  try {
    const auth = inject(AuthService);
    const token = auth.accessToken();
    if (!token) return next(req);
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  } catch (e) {
    console.error('[authInterceptor] error:', e);
    return next(req);
  }
};

export const apiResponseInterceptor: HttpInterceptorFn = (req, next) => {
  const notif = inject(NotificationService);
  return next(req).pipe(
    map((event) => {
      try {
        if (event instanceof HttpResponse && isApiResponse(event.body)) {
          const body = event.body;
          if (body.success) {
            if (body.messageKey) {
              notif.success(body.messageKey, body.messageArgs ?? undefined);
            }
            return event.clone({ body: body.data });
          }
          throw new ApiException(
            body.messageKey,
            body.errors ?? [],
            body.traceId,
            event.status
          );
        }
      } catch (e) {
        if (e instanceof ApiException) throw e;
        console.error('[apiResponseInterceptor] error:', e);
      }
      return event;
    })
  );
};

function shouldLogoutOn401(reqUrl: string): boolean {
  // Sai mật khẩu /login → không logout (user chưa authenticated); chỉ hiện ErrorDialog.
  return !reqUrl.includes('/auth/login');
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notif = inject(NotificationService);
  const auth = inject(AuthService);
  return next(req).pipe(
    catchError((err: unknown) => {
      try {
        if (err instanceof ApiException) {
          if (err.status === 401 && shouldLogoutOn401(req.url)) auth.logout();
          notif.error({
            messageKey: err.messageKey,
            errors: err.errors,
            traceId: err.traceId,
            status: err.status
          });
          return throwError(() => err);
        }
        if (err instanceof HttpErrorResponse) {
          const body = isApiResponse(err.error) ? err.error : null;
          const traceId = err.headers.get(TRACE_HEADER) ?? body?.traceId ?? '-';
          if (err.status === 401 && shouldLogoutOn401(req.url)) auth.logout();
          notif.error({
            messageKey: body?.messageKey ?? mapStatusKey(err.status),
            errors: body?.errors ?? null,
            traceId,
            status: err.status
          });
        } else {
          notif.error({ messageKey: 'system.unexpected', traceId: '-', errors: null });
        }
      } catch (e) {
        console.error('[errorInterceptor] error in handler:', e);
      }
      return throwError(() => err);
    })
  );
};

function isApiResponse(value: unknown): value is ApiResponse<unknown> {
  return typeof value === 'object' && value !== null
    && 'success' in (value as Record<string, unknown>)
    && 'traceId' in (value as Record<string, unknown>);
}

function mapStatusKey(status: number): string {
  switch (status) {
    case 0: return 'system.unexpected';
    case 400: return 'validation.failed';
    case 401: return 'auth.unauthorized';
    case 403: return 'auth.unauthorized';
    case 404: return 'system.unexpected';
    case 409: return 'system.unexpected';
    default: return 'system.unexpected';
  }
}
