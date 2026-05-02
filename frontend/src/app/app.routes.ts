import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';
import { AppShellComponent } from '@core/layout/app-shell.component';

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
        loadComponent: () => import('@features/issue/issues.page').then((m) => m.IssuesPageComponent),
        data: { issueListVariant: 'backlog' }
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
        loadComponent: () =>
          import('@features/project/project-settings.page').then((m) => m.ProjectSettingsPageComponent)
      },
      {
        path: 'projects/:projectKey',
        loadComponent: () => import('@features/project/project-detail.page').then((m) => m.ProjectDetailPageComponent)
      },
      {
        path: 'issues',
        loadComponent: () => import('@features/issue/issues.page').then((m) => m.IssuesPageComponent)
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
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
