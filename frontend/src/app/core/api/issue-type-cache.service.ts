import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { IssueType, ProjectApiService } from './project.service';

/**
 * Cache IssueType per typeId (load qua project detail). Issue list/board map
 * issueTypeId sang name + color + key cho type pill mà không phải gọi BE từng row.
 *
 * Single fetch per project — issue types thuộc project, ID cố định nên cache vô hạn
 * trong session là an toàn.
 */
@Injectable({ providedIn: 'root' })
export class IssueTypeCacheService {
  private readonly api = inject(ProjectApiService);

  private readonly cache = new Map<string, IssueType>();
  private readonly loadedProjects = new Set<string>();
  private readonly inflight = new Map<string, Promise<void>>();

  ensureProjectLoaded(projectId: string): Promise<void> {
    if (this.loadedProjects.has(projectId)) return Promise.resolve();
    const existing = this.inflight.get(projectId);
    if (existing) return existing;

    const promise = firstValueFrom(this.api.getById(projectId))
      .then((detail) => {
        for (const t of detail.issueTypes) this.cache.set(t.id, t);
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
    for (const t of types) this.cache.set(t.id, t);
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
