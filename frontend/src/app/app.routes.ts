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
        path: 'projects/:projectKey',
        loadComponent: () => import('@features/project/project-detail.page').then((m) => m.ProjectDetailPageComponent)
      },
      {
        path: 'projects/:projectKey/issues',
        loadComponent: () => import('@features/issue/issues.page').then((m) => m.IssuesPageComponent)
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
        path: 'products',
        loadComponent: () => import('@features/sample/products.page').then((m) => m.ProductsPageComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
