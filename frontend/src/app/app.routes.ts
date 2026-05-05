import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';
import { adminRoleGuard } from '@core/auth/admin-role.guard';
import { AppShellComponent } from '@core/layout/app-shell.component';
import { projectSettingsDetailResolver } from '@features/project/project-settings.resolver';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('@features/identity/login.page').then((m) => m.LoginPageComponent)
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'workspaces' },
      {
        path: 'workspaces',
        loadComponent: () => import('@features/workspace/workspaces.page').then((m) => m.WorkspacesPageComponent)
      },
      {
        path: 'workspaces/:slug',
        loadComponent: () => import('@features/workspace/workspace-detail.page').then((m) => m.WorkspaceDetailPageComponent)
      },
      {
        path: 'projects',
        loadComponent: () => import('@features/project/projects.page').then((m) => m.ProjectsPageComponent)
      },
      {
        path: 'projects/:projectKey/board',
        loadComponent: () => import('@features/project/board.page').then((m) => m.BoardPageComponent)
      },
      {
        path: 'projects/:projectKey/backlog',
        loadComponent: () => import('@features/project/backlog.page').then((m) => m.BacklogPageComponent)
      },
      {
        path: 'projects/:projectKey/issues',
        loadComponent: () => import('@features/issue/issues.page').then((m) => m.IssuesPageComponent),
        data: { issueListVariant: 'issues' }
      },
      {
        path: 'projects/:projectKey/reports',
        loadComponent: () =>
          import('@features/project/project-reports.page').then((m) => m.ProjectReportsPageComponent)
      },
      {
        path: 'projects/:projectKey/settings',
        resolve: { projectDetail: projectSettingsDetailResolver },
        loadComponent: () =>
          import('@features/project/project-settings-shell.page').then((m) => m.ProjectSettingsShellComponent),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('@features/project/project-settings-overview.page').then(
                (m) => m.ProjectSettingsOverviewPageComponent
              )
          },
          {
            path: 'workflow',
            loadComponent: () =>
              import('@features/project/workflow-editor.page').then((m) => m.WorkflowEditorPageComponent)
          },
          {
            path: 'fields',
            loadComponent: () =>
              import('@features/project/custom-fields-admin.page').then((m) => m.CustomFieldsAdminPageComponent)
          },
          {
            path: 'members',
            loadComponent: () =>
              import('@features/project/project-members-admin.page').then((m) => m.ProjectMembersAdminPageComponent)
          }
        ]
      },
      {
        path: 'projects/:projectKey',
        loadComponent: () => import('@features/project/project-detail.page').then((m) => m.ProjectDetailPageComponent)
      },
      {
        path: 'issues',
        loadComponent: () => import('@features/issue/issues.page').then((m) => m.IssuesPageComponent),
        data: { issueListVariant: 'my' }
      },
      {
        path: 'issues/:issueKey',
        loadComponent: () => import('@features/issue/issue-detail.page').then((m) => m.IssueDetailPageComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('@features/identity/profile.page').then((m) => m.ProfilePageComponent)
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('@features/settings/app-settings.page').then((m) => m.AppSettingsPageComponent)
      },
      {
        path: 'admin',
        canActivate: [adminRoleGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'email-templates' },
          {
            path: 'email-templates',
            loadComponent: () =>
              import('@features/admin/email-templates-admin.page').then((m) => m.EmailTemplatesAdminPageComponent)
          },
          {
            path: 'email-logs',
            loadComponent: () =>
              import('@features/admin/email-logs-admin.page').then((m) => m.EmailLogsAdminPageComponent)
          },
          {
            path: 'audit',
            loadComponent: () =>
              import('@features/admin/audit-admin.page').then((m) => m.AuditAdminPageComponent)
          }
        ]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
