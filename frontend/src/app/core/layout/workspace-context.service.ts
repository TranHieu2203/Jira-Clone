import { Injectable, signal } from '@angular/core';
import { ProjectSummary } from '@core/api/project.service';

/**
 * State chia sẻ giữa router + sidebar: workspace + project hiện tại.
 * Set bởi route resolver / page; sidebar đọc qua signal để render contextual nav.
 * Type minimal để chấp nhận cả Workspace lẫn WorkspaceDetail.
 */
export interface WorkspaceContextSummary {
  id: string;
  name: string;
  slug: string;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceContextService {
  private readonly workspaceSig = signal<WorkspaceContextSummary | null>(null);
  private readonly projectSig = signal<ProjectSummary | null>(null);

  readonly workspace = this.workspaceSig.asReadonly();
  readonly project = this.projectSig.asReadonly();

  setWorkspace(ws: WorkspaceContextSummary | null): void {
    this.workspaceSig.set(ws);
  }

  setProject(p: ProjectSummary | null): void {
    this.projectSig.set(p);
  }

  clear(): void {
    this.workspaceSig.set(null);
    this.projectSig.set(null);
  }
}
