import { XIcon } from 'lucide-react';
import { useEffect } from 'react';

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  className?: string;
}

export function Dialog({ open, onClose, title, children, className = 'max-w-lg' }: DialogProps) {
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  if (!open) return null;
  return (
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-50 grid place-items-center bg-black/40 px-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className={`card w-full ${className} max-h-[90vh] overflow-hidden flex flex-col`}>
        {title && (
          <header className="flex items-center justify-between px-4 py-3 border-b border-ink-200">
            <h2 className="text-sm font-semibold">{title}</h2>
            <button className="btn-ghost p-1" onClick={onClose} aria-label="Đóng">
              <XIcon size={16} />
            </button>
          </header>
        )}
        <div className="overflow-y-auto p-4">{children}</div>
      </div>
    </div>
  );
}
