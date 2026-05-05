import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Chỉ user có role hệ thống Admin (JWT claim). */
export const adminRoleGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    void router.navigate(['/login']);
    return false;
  }
  const roles = auth.user()?.roles ?? [];
  if (!roles.includes('Admin')) {
    void router.navigate(['/workspaces']);
    return false;
  }
  return true;
};
