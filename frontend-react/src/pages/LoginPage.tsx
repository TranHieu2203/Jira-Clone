import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '@/api/auth';
import { useAuthStore } from '@/stores/auth';

export default function LoginPage() {
  const setSession = useAuthStore((s) => s.setSession);
  const nav = useNavigate();
  const [userName, setUserName] = useState('admin');
  const [password, setPassword] = useState('Admin@123');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const r = await login(userName, password);
      setSession(
        { accessToken: r.accessToken, refreshToken: r.refreshToken },
        { id: r.userId, userName: r.userName, displayName: r.displayName, roles: r.roles }
      );
      nav('/templates', { replace: true });
    } catch (err: any) {
      setError(err?.messageKey ?? err?.message ?? 'Đăng nhập thất bại');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen grid place-items-center bg-ink-50/40 px-4">
      <form onSubmit={submit} className="card w-full max-w-sm p-6 space-y-4">
        <div>
          <h1 className="text-lg font-semibold">Đăng nhập</h1>
          <p className="text-xs text-ink-500 mt-1">Form Management POC</p>
        </div>
        <div>
          <label className="label">Tên đăng nhập</label>
          <input className="input" value={userName} onChange={(e) => setUserName(e.target.value)} autoFocus />
        </div>
        <div>
          <label className="label">Mật khẩu</label>
          <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        {error && <div className="text-xs text-danger">{error}</div>}
        <button type="submit" className="btn-primary w-full" disabled={loading}>
          {loading ? 'Đang xử lý…' : 'Đăng nhập'}
        </button>
      </form>
    </div>
  );
}
