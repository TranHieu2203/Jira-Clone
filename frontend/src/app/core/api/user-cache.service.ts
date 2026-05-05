import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { UserApiService, UserSummary } from './user.service';

/**
 * Cache user lookup theo userId. Backlog/board hiển thị assignee name + initials
 * mà không phải gọi BE từng row. Dedupe các request inflight.
 */
@Injectable({ providedIn: 'root' })
export class UserCacheService {
  private readonly api = inject(UserApiService);
  private readonly cache = new Map<string, UserSummary>();
  private readonly inflight = new Map<string, Promise<UserSummary | null>>();

  get(userId: string): UserSummary | null {
    return this.cache.get(userId) ?? null;
  }

  displayNameOf(userId: string | null | undefined): string | null {
    if (!userId) return null;
    return this.cache.get(userId)?.displayName ?? null;
  }

  initialsOf(userId: string | null | undefined): string {
    if (!userId) return '?';
    const u = this.cache.get(userId);
    const src = (u?.displayName || u?.userName || userId).trim();
    if (!src) return '?';
    const parts = src.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return src.slice(0, 2).toUpperCase();
  }

  /** Bulk pre-fetch các userId chưa có trong cache. Trả về sau khi xong. */
  async ensureLoaded(userIds: ReadonlyArray<string | null | undefined>): Promise<void> {
    const need = new Set<string>();
    for (const id of userIds) {
      if (!id) continue;
      if (this.cache.has(id)) continue;
      need.add(id);
    }
    if (need.size === 0) return;
    const promises = [...need].map((id) => this.fetchOne(id));
    await Promise.all(promises);
  }

  private fetchOne(userId: string): Promise<UserSummary | null> {
    const existing = this.inflight.get(userId);
    if (existing) return existing;
    const p = firstValueFrom(this.api.getById(userId))
      .then((u) => {
        if (u && u.id) this.cache.set(u.id, u);
        this.inflight.delete(userId);
        return u ?? null;
      })
      .catch(() => {
        this.inflight.delete(userId);
        return null;
      });
    this.inflight.set(userId, p);
    return p;
  }
}
