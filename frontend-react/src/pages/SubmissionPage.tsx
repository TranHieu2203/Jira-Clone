import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeftIcon, DownloadIcon } from 'lucide-react';
import { templateApi } from '@/api/template';
import { metadataApi, METADATA_GROUPS, MetadataType } from '@/api/metadata';
import { submitAndExport } from '@/api/submission';

/**
 * Mail-merge data entry: auto-generate form từ template.usedFields, group theo prefix.
 * Submit → BE mail-merge → return DOCX blob → trigger download.
 */
export default function SubmissionPage() {
  const { id } = useParams<{ id: string }>();
  const nav = useNavigate();
  const [values, setValues] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: template } = useQuery({
    queryKey: ['template', id],
    queryFn: () => templateApi.getById(id!),
    enabled: !!id,
  });
  const { data: allMetadata = [] } = useQuery({
    queryKey: ['metadata-all'],
    queryFn: () => metadataApi.search(),
  });

  // Field cần fill: template.usedFields ∩ metadata library. Map metadata cho mỗi field code.
  const fields = useMemo(() => {
    if (!template) return [];
    const meta = new Map(allMetadata.map((m) => [m.value, m]));
    return template.usedFields
      .map((code) => meta.get(code) ?? { id: code, value: code, label: code, type: MetadataType.Text, fieldGroup: code[0], description: null, validationJson: null, createdAt: '' })
      .sort((a, b) => (a.fieldGroup ?? '').localeCompare(b.fieldGroup ?? '') || a.value.localeCompare(b.value));
  }, [template, allMetadata]);

  const grouped = useMemo(() => {
    const map = new Map<string, typeof fields>();
    for (const f of fields) {
      const k = f.fieldGroup ?? '?';
      const arr = (map.get(k) ?? []) as typeof fields;
      arr.push(f);
      map.set(k, arr);
    }
    return [...map.entries()];
  }, [fields]);

  const groupLabel = useMemo(() => Object.fromEntries(METADATA_GROUPS.map((g) => [g.value, g.label])), []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!template) return;
    setSubmitting(true);
    setError(null);
    try {
      const blob = await submitAndExport({
        templateId: template.id,
        data: values,
        exportFormat: 2, // Docx
      });
      // Trigger download.
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${template.code}-merged.docx`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err: any) {
      setError(err?.message ?? 'Export thất bại');
    } finally {
      setSubmitting(false);
    }
  }

  if (!template) return <div className="p-6 text-sm text-ink-500">Đang tải…</div>;

  return (
    <div className="p-6 space-y-4 max-w-3xl">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <button className="btn-ghost btn-sm" onClick={() => nav(-1)}>
            <ArrowLeftIcon size={14} /> Quay lại
          </button>
          <div>
            <h1 className="text-lg font-semibold">Nhập data: {template.name}</h1>
            <p className="text-xs text-ink-500 font-mono">{template.code}</p>
          </div>
        </div>
      </header>

      <form onSubmit={handleSubmit} className="space-y-6">
        {grouped.length === 0 ? (
          <p className="text-sm text-ink-500">
            Template chưa có field nào. Quay lại editor để insert MERGEFIELD từ sidebar metadata.
          </p>
        ) : (
          grouped.map(([key, list]) => (
            <section key={key} className="card p-4">
              <header className="mb-3">
                <h2 className="text-sm font-semibold">
                  <span className="font-mono">{key}</span>
                  <span className="text-ink-500 font-normal ml-2">— {groupLabel[key] ?? 'Khác'}</span>
                </h2>
              </header>
              <div className="grid grid-cols-2 gap-3">
                {list.map((f) => (
                  <FieldInput
                    key={f.value}
                    field={f}
                    value={values[f.value] ?? ''}
                    onChange={(v) => setValues((prev) => ({ ...prev, [f.value]: v }))}
                  />
                ))}
              </div>
            </section>
          ))
        )}

        {error && <div className="text-sm text-danger">{error}</div>}

        <div className="flex justify-end gap-2">
          <button type="submit" className="btn-primary" disabled={submitting || fields.length === 0}>
            <DownloadIcon size={14} /> {submitting ? 'Đang merge…' : 'Mail-merge + tải DOCX'}
          </button>
        </div>
      </form>
    </div>
  );
}

interface FieldInputProps {
  field: { value: string; label: string; type: MetadataType };
  value: string;
  onChange: (v: string) => void;
}

function FieldInput({ field, value, onChange }: FieldInputProps) {
  const inputType =
    field.type === MetadataType.Number ? 'number' :
    field.type === MetadataType.Date ? 'date' :
    field.type === MetadataType.Currency ? 'number' :
    'text';

  return (
    <div className={field.type === MetadataType.Textarea ? 'col-span-2' : ''}>
      <label className="label">
        <span className="font-mono text-[10px] text-ink-500 mr-1">{field.value}</span>
        {field.label}
      </label>
      {field.type === MetadataType.Textarea ? (
        <textarea className="input" rows={3} value={value} onChange={(e) => onChange(e.target.value)} />
      ) : (
        <input className="input" type={inputType} value={value} onChange={(e) => onChange(e.target.value)} />
      )}
    </div>
  );
}
