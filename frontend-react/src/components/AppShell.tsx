import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';
import { FileTextIcon, ListIcon, LogOutIcon } from 'lucide-react';
import clsx from 'clsx';

export function AppShell() {
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const nav = useNavigate();

  const handleLogout = () => {
    clear();
    nav('/login', { replace: true });
  };

  return (
    <div className="min-h-screen flex">
      <aside className="w-60 shrink-0 border-r border-ink-200 bg-ink-50/40 flex flex-col">
        <div className="px-4 py-4 border-b border-ink-200">
          <h1 className="text-sm font-semibold tracking-wide">Form Management</h1>
          <p className="text-xs text-ink-500 mt-0.5">POC · OnlyOffice + .NET</p>
        </div>
        <nav className="flex-1 px-2 py-3 space-y-0.5">
          <NavItem to="/templates" icon={<FileTextIcon size={16} />} label="Biểu mẫu" />
          <NavItem to="/metadata" icon={<ListIcon size={16} />} label="Trường biểu mẫu" />
        </nav>
        <div className="px-3 py-3 border-t border-ink-200 text-xs space-y-2">
          {user && (
            <div className="text-ink-700">
              <div className="font-medium truncate">{user.displayName}</div>
              <div className="text-ink-500 truncate">@{user.userName}</div>
            </div>
          )}
          <button onClick={handleLogout} className="btn-ghost w-full justify-start gap-2 text-ink-700">
            <LogOutIcon size={14} /> Đăng xuất
          </button>
        </div>
      </aside>
      <main className="flex-1 min-w-0 bg-white">
        <Outlet />
      </main>
    </div>
  );
}

function NavItem({ to, icon, label }: { to: string; icon: React.ReactNode; label: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        clsx(
          'flex items-center gap-2 px-3 py-2 rounded-md text-sm transition-colors',
          isActive ? 'bg-ink-900 text-white' : 'text-ink-700 hover:bg-ink-100'
        )
      }
    >
      {icon}
      {label}
    </NavLink>
  );
}
