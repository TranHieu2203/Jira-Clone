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
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      {
        path: 'products',
        loadComponent: () => import('@features/sample/products.page').then((m) => m.ProductsPageComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
