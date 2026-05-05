import { Injectable, Signal, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { IssueType, ProjectApiService } from './project.service';

/**
 * Cache IssueType per typeId (load qua project detail). Issue list/board map
 * issueTypeId sang name + color + key cho type pill mà không phải gọi BE từng row.
 *
 * Signal-reactive: `version()` increments mỗi khi có entry mới.
 */
@Injectable({ providedIn: 'root' })
export class IssueTypeCacheService {
  private readonly api = inject(ProjectApiService);

  private readonly cache = new Map<string, IssueType>();
  private readonly loadedProjects = new Set<string>();
  private readonly inflight = new Map<string, Promise<void>>();
  private readonly _version = signal(0);
  readonly version: Signal<number> = this._version.asReadonly();

  ensureProjectLoaded(projectId: string): Promise<void> {
    if (this.loadedProjects.has(projectId)) return Promise.resolve();
    const existing = this.inflight.get(projectId);
    if (existing) return existing;

    const promise = firstValueFrom(this.api.getById(projectId))
      .then((detail) => {
        if (detail.issueTypes.length > 0) {
          for (const t of detail.issueTypes) this.cache.set(t.id, t);
          this._version.update((v) => v + 1);
        }
        this.loadedProjects.add(projectId);
        this.inflight.delete(projectId);
      })
      .catch(() => {
        this.inflight.delete(projectId);
      });
    this.inflight.set(projectId, promise);
    return promise;
  }

  putMany(types: readonly IssueType[]): void {
    if (types.length === 0) return;
    for (const t of types) this.cache.set(t.id, t);
    this._version.update((v) => v + 1);
  }

  get(typeId: string): IssueType | null {
    return this.cache.get(typeId) ?? null;
  }

  nameOf(typeId: string): string | null {
    return this.cache.get(typeId)?.name ?? null;
  }

  colorOf(typeId: string): string | null {
    return this.cache.get(typeId)?.color ?? null;
  }

  has(typeId: string): boolean {
    return this.cache.has(typeId);
  }
}
