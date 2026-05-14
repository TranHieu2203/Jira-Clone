import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlusIcon, FileTextIcon } from 'lucide-react';
import { templateApi, type TemplateSummary } from '@/api/template';
import { Dialog } from '@/components/ui/Dialog';
import { CreateTemplateForm } from '@/components/CreateTemplateForm';

export default function TemplateListPage() {
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const nav = useNavigate();
  const qc = useQueryClient();

  const { data: templates = [], isLoading } = useQuery({
    queryKey: ['templates', search],
    queryFn: () => templateApi.search(search || undefined),
  });

  const createMut = useMutation({
    mutationFn: templateApi.create,
    onSuccess: (tpl) => {
      qc.invalidateQueries({ queryKey: ['templates'] });
      setShowCreate(false);
      nav(`/templates/${tpl.id}/edit`);
    },
  });

  return (
    <div className="p-6 space-y-4">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold">Biểu mẫu</h1>
          <p className="text-xs text-ink-500 mt-1">Template hợp đồng / biểu mẫu — chỉnh sửa qua OnlyOffice editor.</p>
        </div>
        <button className="btn-primary" onClick={() => setShowCreate(true)}>
          <PlusIcon size={14} /> Tạo mới
        </button>
      </header>

      <div className="flex gap-2 items-center">
        <input
          className="input max-w-sm"
          placeholder="Tìm theo mã hoặc tên…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {isLoading ? (
        <p className="text-sm text-ink-500">Đang tải…</p>
      ) : templates.length === 0 ? (
        <p className="text-sm text-ink-500">Chưa có template nào.</p>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-ink-50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium w-32">Mã</th>
                <th className="px-3 py-2 font-medium">Tên</th>
                <th className="px-3 py-2 font-medium w-28">Trạng thái</th>
                <th className="px-3 py-2 font-medium w-20 text-right">Version</th>
                <th className="px-3 py-2 font-medium w-24 text-right">Field</th>
                <th className="px-3 py-2 w-16"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink-100">
              {templates.map((t) => (
                <TemplateRow key={t.id} t={t} onOpen={() => nav(`/templates/${t.id}/edit`)} />
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={showCreate} onClose={() => setShowCreate(false)} title="Tạo template mới">
        <CreateTemplateForm
          onSubmit={(req) => createMut.mutate(req)}
          isSubmitting={createMut.isPending}
          error={createMut.isError ? (createMut.error as Error).message : null}
        />
      </Dialog>
    </div>
  );
}

function TemplateRow({ t, onOpen }: { t: TemplateSummary; onOpen: () => void }) {
  const statusText = t.status === 1 ? 'Nháp' : t.status === 2 ? 'Phát hành' : 'Lưu trữ';
  return (
    <tr className="hover:bg-ink-50/60 cursor-pointer" onClick={onOpen}>
      <td className="px-3 py-2 font-mono text-xs">{t.code}</td>
      <td className="px-3 py-2">
        <div className="flex items-center gap-2">
          <FileTextIcon size={14} className="text-ink-400" />
          {t.name}
        </div>
      </td>
      <td className="px-3 py-2 text-xs">{statusText}</td>
      <td className="px-3 py-2 text-right text-xs text-ink-500">v{t.version}</td>
      <td className="px-3 py-2 text-right text-xs text-ink-500">{t.usedFieldsCount}</td>
      <td className="px-3 py-2 text-right">
        <button className="btn-ghost btn-sm" onClick={(e) => { e.stopPropagation(); onOpen(); }}>Mở</button>
      </td>
    </tr>
  );
}
