import { Injectable, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { APP_CONFIG } from '@core/config/app-config';

/** Payload từ hub — khớp camelCase từ server. */
export interface BoardRealtimePayload {
  action: string;
  issueId: string;
  issueKey: string;
}

export interface IssueThreadRealtimePayload {
  action: string;
  commentId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceHubService {
  private readonly cfg = inject(APP_CONFIG);

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  private readonly boardHandlers = new Set<(payload: BoardRealtimePayload) => void>();
  private readonly issueHandlers = new Set<(payload: IssueThreadRealtimePayload) => void>();

  /**
   * Derive hub URL từ APP_CONFIG.apiBaseUrl thay vì `window.location.origin`.
   * - Prod: `apiBaseUrl = '/api'` → strip `/api` → `''` → `/hubs/workspace` (cùng origin với SPA)
   * - Dev (ng serve): `apiBaseUrl = 'http://localhost:5000/api'` → strip `/api` → `http://localhost:5000`
   *   → `http://localhost:5000/hubs/workspace`. Trước đó dùng `window.origin` = `:4200`
   *   gây 404 vì ng serve không proxy /hubs/* sang BE.
   */
  private hubUrl(): string {
    const base = this.cfg.apiBaseUrl;
    // Strip trailing /api (or any /api/) to get the API origin/host root.
    const root = base.replace(/\/api\/?$/, '');
    return `${root}/hubs/workspace`;
  }

  private accessToken(): string {
    return localStorage.getItem('app.access') ?? '';
  }

  private async ensureStarted(): Promise<HubConnection> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return this.connection;
    }
    if (this.startPromise) {
      await this.startPromise;
      return this.connection!;
    }

    const conn = new HubConnectionBuilder()
      .withUrl(this.hubUrl(), {
        accessTokenFactory: () => this.accessToken(),
        withCredentials: true
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection = conn;
    this.startPromise = conn
      .start()
      .then(() => undefined)
      .finally(() => {
        this.startPromise = null;
      });

    await this.startPromise;
    return conn;
  }

  async joinProject(projectId: string): Promise<void> {
    if (!this.accessToken()) return;
    try {
      const conn = await this.ensureStarted();
      await conn.invoke('JoinProject', projectId);
    } catch {
      /* hub down — polling vẫn chạy */
    }
  }

  async leaveProject(projectId: string): Promise<void> {
    try {
      if (this.connection?.state === HubConnectionState.Connected) {
        await this.connection.invoke('LeaveProject', projectId);
      }
    } catch {
      /* ignore */
    }
  }

  async joinIssue(issueId: string): Promise<void> {
    if (!this.accessToken()) return;
    try {
      const conn = await this.ensureStarted();
      await conn.invoke('JoinIssue', issueId);
    } catch {
      /* ignore */
    }
  }

  async leaveIssue(issueId: string): Promise<void> {
    try {
      if (this.connection?.state === HubConnectionState.Connected) {
        await this.connection.invoke('LeaveIssue', issueId);
      }
    } catch {
      /* ignore */
    }
  }

  addBoardListener(handler: (payload: BoardRealtimePayload) => void): void {
    this.boardHandlers.add(handler);
    void this.ensureStarted().then((conn) => {
      if (!this.boardHandlers.has(handler)) return;
      conn.on('BoardEvent', handler);
    });
  }

  removeBoardListener(handler: (payload: BoardRealtimePayload) => void): void {
    this.boardHandlers.delete(handler);
    this.connection?.off('BoardEvent', handler);
  }

  addIssueListener(handler: (payload: IssueThreadRealtimePayload) => void): void {
    this.issueHandlers.add(handler);
    void this.ensureStarted().then((conn) => {
      if (!this.issueHandlers.has(handler)) return;
      conn.on('IssueEvent', handler);
    });
  }

  removeIssueListener(handler: (payload: IssueThreadRealtimePayload) => void): void {
    this.issueHandlers.delete(handler);
    this.connection?.off('IssueEvent', handler);
  }

  /**
   * Gọi khi logout để đóng WebSocket.
   * Không xóa Set handler — component destroy sẽ off theo ref; sau login connection tạo lại.
   */
  async disconnect(): Promise<void> {
    try {
      if (this.connection) {
        await this.connection.stop();
      }
    } finally {
      this.connection = null;
    }
  }
}
