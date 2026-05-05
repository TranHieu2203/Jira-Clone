import { Injectable, Signal, inject, signal } from '@angular/core';
import { WorkflowApiService, WorkflowStatus } from './workflow.service';

/**
 * Cache workflow statuses theo statusId. Issue list/detail map currentStatusId
 * sang name + category mà không phải gọi BE từng row.
 *
 * MVP: cache vô hạn trong session. Khi designer sửa workflow → user reload page
 * sẽ refresh. Sau này có thể thêm TTL.
 *
 * Signal-reactive: `version()` increments mỗi khi có entry mới → consumers wrap
 * `cache.nameOf(id)` trong `computed(() => { cache.version(); ... })` để auto-update.
 */
@Injectable({ providedIn: 'root' })
export class StatusCacheService {
  private readonly api = inject(WorkflowApiService);

  private readonly cache = new Map<string, { name: string; key: string; category: number; color: string | null | undefined }>();
  private readonly loadedProjects = new Set<string>();
  private readonly inflight = new Map<string, Promise<void>>();
  private readonly _version = signal(0);
  /** Tick signal — đăng ký trong computed để re-eval khi cache fill thêm entry. */
  readonly version: Signal<number> = this._version.asReadonly();

  ensureProjectLoaded(projectId: string): Promise<void> {
    if (this.loadedProjects.has(projectId)) return Promise.resolve();
    const existing = this.inflight.get(projectId);
    if (existing) return existing;

    const promise = new Promise<void>((resolve) => {
      this.api.listByProject(projectId).subscribe({
        next: (workflows) => {
          for (const wf of workflows) {
            for (const s of wf.statuses) this.put(s);
          }
          this.loadedProjects.add(projectId);
          this.inflight.delete(projectId);
          resolve();
        },
        error: () => {
          this.inflight.delete(projectId);
          resolve();
        }
      });
    });
    this.inflight.set(projectId, promise);
    return promise;
  }

  put(status: WorkflowStatus): void {
    this.cache.set(status.id, {
      name: status.name,
      key: status.key,
      category: status.category,
      color: status.color
    });
    this._version.update((v) => v + 1);
  }

  putMany(statuses: readonly WorkflowStatus[]): void {
    if (statuses.length === 0) return;
    for (const s of statuses) {
      this.cache.set(s.id, { name: s.name, key: s.key, category: s.category, color: s.color });
    }
    this._version.update((v) => v + 1);
  }

  nameOf(statusId: string): string | null {
    return this.cache.get(statusId)?.name ?? null;
  }

  categoryOf(statusId: string): number | null {
    return this.cache.get(statusId)?.category ?? null;
  }

  colorOf(statusId: string): string | null | undefined {
    return this.cache.get(statusId)?.color;
  }

  has(statusId: string): boolean {
    return this.cache.has(statusId);
  }
}
