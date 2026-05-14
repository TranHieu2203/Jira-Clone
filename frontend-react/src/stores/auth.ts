import { create } from 'zustand';

interface User {
  id: string;
  userName: string;
  displayName: string;
  roles: string[];
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: User | null;
  setSession: (tokens: { accessToken: string; refreshToken: string }, user: User) => void;
  clear: () => void;
}

const TOKEN_KEY = 'fm.access';
const REFRESH_KEY = 'fm.refresh';
const USER_KEY = 'fm.user';

const initialUser = (): User | null => {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try { return JSON.parse(raw); } catch { return null; }
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: localStorage.getItem(TOKEN_KEY),
  refreshToken: localStorage.getItem(REFRESH_KEY),
  user: initialUser(),
  setSession: (tokens, user) => {
    localStorage.setItem(TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    set({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken, user });
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    set({ accessToken: null, refreshToken: null, user: null });
  },
}));
