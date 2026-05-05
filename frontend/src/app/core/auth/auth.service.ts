import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { WorkspaceHubService } from '@core/realtime/workspace-hub.service';

const TOKEN_KEY = 'app.access';
const REFRESH_KEY = 'app.refresh';
const USER_KEY = 'app.user';

export interface AuthResponse {
  userId: string;
  userName: string;
  displayName: string;
  roles: string[];
  accessToken: string;
  refreshToken: string;
  accessExpiresAt: string;
}

export interface CurrentUser {
  id: string;
  userName: string;
  displayName: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly cfg = inject(APP_CONFIG);
  private readonly workspaceHub = inject(WorkspaceHubService);

  private readonly accessTokenSig = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly userSig = signal<CurrentUser | null>(this.readUser());

  readonly accessToken = this.accessTokenSig.asReadonly();
  readonly user = this.userSig.asReadonly();
  readonly isAuthenticated = computed(() => !!this.accessTokenSig());

  // The apiResponseInterceptor unwraps {success,data,...} → body.data,
  // so this returns AuthResponse directly (or throws ApiException on failure).
  login(userName: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.cfg.apiBaseUrl}/v1/auth/login`, { userName, password })
      .pipe(tap((data) => this.persist(data)));
  }

  logout(): void {
    void this.workspaceHub.disconnect();
    const refresh = localStorage.getItem(REFRESH_KEY);
    if (refresh) {
      this.http.post(`${this.cfg.apiBaseUrl}/v1/auth/logout`, { refreshToken: refresh })
        .subscribe({ next: () => {}, error: () => {} });
    }
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.accessTokenSig.set(null);
    this.userSig.set(null);
    this.router.navigate(['/login']);
  }

  private persist(data: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, data.accessToken);
    localStorage.setItem(REFRESH_KEY, data.refreshToken);
    const u: CurrentUser = {
      id: data.userId,
      userName: data.userName,
      displayName: data.displayName,
      email: '',
      roles: data.roles
    };
    localStorage.setItem(USER_KEY, JSON.stringify(u));
    this.accessTokenSig.set(data.accessToken);
    this.userSig.set(u);
  }

  private readUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as CurrentUser; } catch { return null; }
  }
}
