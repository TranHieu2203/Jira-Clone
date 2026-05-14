import { api } from '@/lib/api';

export interface LoginResponse {
  userId: string;
  userName: string;
  displayName: string;
  roles: string[];
  accessToken: string;
  refreshToken: string;
}

export function login(userName: string, password: string) {
  return api.post<LoginResponse>('/v1/auth/login', { userName, password });
}
