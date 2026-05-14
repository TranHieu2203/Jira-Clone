import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlusIcon, PencilIcon, Trash2Icon } from 'lucide-react';
import {
  METADATA_GROUPS,
  METADATA_TYPE_OPTIONS,
  MetadataType,
  type CreateMetadataRequest,
  type MetadataDto,
  type UpdateMetadataRequest,
  metadataApi,
} from '@/api/metadata';
import { Dialog } from '@/components/ui/Dialog';

export default function MetadataListPage() {
  const [search, setSearch] = useState('');
  const [groupFilter, setGroupFilter] = useState<string>('');
  const [editing, setEditing] = useState<MetadataDto | null>(null);
  const [creating, setCreating] = useState(false);
  const qc = useQueryClient();

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['metadata', search, groupFilter],
    queryFn: () => metadataApi.search(search || undefined, groupFilter || undefined),
  });

  const createMut = useMutation({
    mutationFn: metadataApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['metadata'] }); setCreating(false); },
  });
  const updateMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateMetadataRequest }) => metadataApi.update(id, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['metadata'] }); setEditing(null); },
  });
  const removeMut = useMutation({
    mutationFn: metadataApi.remove,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['metadata'] }),
  });

  return (
    <div className="p-6 space-y-4">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold">Trường biểu mẫu (Metadata)</h1>
          <p className="text-xs text-ink-500 mt-1">Quản lý field dùng làm MERGEFIELD trong template.</p>
        </div>
        <button className="btn-primary" onClick={() => setCreating(true)}>
          <PlusIcon size={14} /> Thêm metadata
        </button>
      </header>

      <div className="flex gap-2 flex-wrap">
        <input className="input max-w-xs" placeholder="Tìm theo mã/nhãn…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select className="input max-w-xs" value={groupFilter} onChange={(e) => setGroupFilter(e.target.value)}>
          <option value="">— Tất cả group —</option>
          {METADATA_GROUPS.map((g) => (
            <option key={g.value} value={g.value}>{g.value} — {g.label}</option>
          ))}
        </select>
      </div>

      {isLoading ? (
        <p className="text-sm text-ink-500">Đang tải…</p>
      ) : items.length === 0 ? (
        <p className="text-sm text-ink-500">Chưa có metadata.</p>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-ink-50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium w-40">Mã</th>
                <th className="px-3 py-2 font-medium">Nhãn</th>
                <th className="px-3 py-2 font-medium w-24">Kiểu</th>
                <th className="px-3 py-2 font-medium w-16">Group</th>
                <th className="px-3 py-2 font-medium">Mô tả</th>
                <th className="px-3 py-2 w-28"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink-100">
              {items.map((m) => (
                <tr key={m.id} className="hover:bg-ink-50/60">
                  <td className="px-3 py-2 font-mono text-xs">{m.value}</td>
                  <td className="px-3 py-2">{m.label}</td>
                  <td className="px-3 py-2 text-xs">
                    {METADATA_TYPE_OPTIONS.find((o) => o.value === m.type)?.label}
                  </td>
                  <td className="px-3 py-2 text-xs font-mono">{m.fieldGroup ?? '—'}</td>
                  <td className="px-3 py-2 text-xs text-ink-600 truncate max-w-xs">{m.description ?? '—'}</td>
                  <td className="px-3 py-2 text-right">
                    <button className="btn-ghost btn-sm" onClick={() => setEditing(m)} aria-label="Sửa">
                      <PencilIcon size={14} />
                    </button>
                    <button
                      className="btn-ghost btn-sm text-danger"
                      onClick={() => { if (confirm(`Xóa metadata ${m.value}?`)) removeMut.mutate(m.id); }}
                      aria-label="Xóa"
                    >
                      <Trash2Icon size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={creating} onClose={() => setCreating(false)} title="Thêm metadata">
        <MetadataForm
          onSubmit={(body) => createMut.mutate(body as CreateMetadataRequest)}
          isSubmitting={createMut.isPending}
          mode="create"
        />
      </Dialog>

      <Dialog open={!!editing} onClose={() => setEditing(null)} title={`Sửa: ${editing?.value}`}>
        {editing && (
          <MetadataForm
            initial={editing}
            mode="edit"
            onSubmit={(body) => updateMut.mutate({ id: editing.id, body: body as UpdateMetadataRequest })}
            isSubmitting={updateMut.isPending}
          />
        )}
      </Dialog>
    </div>
  );
}

interface FormProps {
  initial?: MetadataDto;
  mode: 'create' | 'edit';
  onSubmit: (body: CreateMetadataRequest | UpdateMetadataRequest) => void;
  isSubmitting: boolean;
}

function MetadataForm({ initial, mode, onSubmit, isSubmitting }: FormProps) {
  const [value, setValue] = useState(initial?.value ?? '');
  const [label, setLabel] = useState(initial?.label ?? '');
  const [type, setType] = useState<MetadataType>(initial?.type ?? MetadataType.Text);
  const [description, setDescription] = useState(initial?.description ?? '');

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (mode === 'create') {
      onSubmit({ value: value.trim().toUpperCase(), label: label.trim(), type, description: description.trim() || null });
    } else {
      onSubmit({ label: label.trim(), type, description: description.trim() || null });
    }
  }

  return (
    <form onSubmit={submit} className="space-y-3">
      <div>
        <label className="label">Mã <span className="text-danger">*</span></label>
        <input
          className="input font-mono"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          disabled={mode === 'edit'}
          placeholder="BSO_HD"
          required
        />
        {mode === 'create' && <p className="text-xs text-ink-500 mt-1">HOA / số / _, bắt đầu bằng chữ. Group tự suy từ ký tự đầu.</p>}
      </div>
      <div>
        <label className="label">Nhãn <span className="text-danger">*</span></label>
        <input className="input" value={label} onChange={(e) => setLabel(e.target.value)} required />
      </div>
      <div>
        <label className="label">Kiểu</label>
        <select className="input" value={type} onChange={(e) => setType(Number(e.target.value) as MetadataType)}>
          {METADATA_TYPE_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>
      <div>
        <label className="label">Mô tả</label>
        <textarea className="input" rows={2} value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>
      <div className="flex justify-end gap-2 pt-2">
        <button type="submit" className="btn-primary" disabled={isSubmitting || !label.trim() || (mode === 'create' && !value.trim())}>
          {isSubmitting ? 'Đang lưu…' : 'Lưu'}
        </button>
      </div>
    </form>
  );
}
