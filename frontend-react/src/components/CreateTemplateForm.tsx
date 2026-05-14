import { useState } from 'react';
import { UploadCloudIcon } from 'lucide-react';
import { templateApi, type CreateTemplateRequest, type DetectedPlaceholder } from '@/api/template';

interface Props {
  onSubmit: (req: CreateTemplateRequest) => void;
  isSubmitting: boolean;
  error: string | null;
}

export function CreateTemplateForm({ onSubmit, isSubmitting, error }: Props) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [importing, setImporting] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);
  const [placeholders, setPlaceholders] = useState<DetectedPlaceholder[]>([]);
  const [docxBase64, setDocxBase64] = useState<string | null>(null);

  /**
   * Khi user chọn file → upload BE để detect placeholders + nhận lại base64. Base64 giữ trong state,
   * chỉ gửi đi khi user submit "Tạo" (cho phép họ điền code/name trước khi commit).
   */
  async function handleFileChange(f: File | null) {
    setFile(f);
    setPlaceholders([]);
    setDocxBase64(null);
    setImportError(null);
    if (!f) return;
    if (!f.name.toLowerCase().endsWith('.docx')) {
      setImportError('Chỉ hỗ trợ file .docx');
      return;
    }
    setImporting(true);
    try {
      const r = await templateApi.importFromWord(f);
      setPlaceholders(r.placeholders);
      setDocxBase64(r.docxBase64);
      // Auto-suggest code từ filename (xóa ext, normalize uppercase + underscore).
      if (!code) {
        const auto = f.name.replace(/\.docx$/i, '').toUpperCase().replace(/[^A-Z0-9_-]/g, '_').slice(0, 50);
        setCode(auto);
      }
      if (!name) setName(f.name.replace(/\.docx$/i, ''));
    } catch (e: any) {
      setImportError(e?.messageKey ?? e?.message ?? 'Import thất bại');
    } finally {
      setImporting(false);
    }
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!docxBase64) {
      setImportError('Chọn file DOCX trước');
      return;
    }
    onSubmit({
      code: code.trim().toUpperCase(),
      name: name.trim(),
      category: category.trim() || null,
      docxBase64,
      usedFields: placeholders.filter((p) => p.pattern === 'guillemets').map((p) => p.text.replace(/[«»]/g, '')),
    });
  }

  const canSubmit = !!docxBase64 && code.trim() && name.trim() && !isSubmitting && !importing;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="label">File DOCX</label>
        <label className="block border-2 border-dashed border-ink-200 rounded-md p-4 text-center cursor-pointer hover:bg-ink-50">
          <UploadCloudIcon className="mx-auto text-ink-400 mb-1" size={20} />
          <span className="text-sm">{file ? file.name : 'Click hoặc kéo-thả file .docx'}</span>
          <input
            type="file"
            accept=".docx"
            className="hidden"
            onChange={(e) => handleFileChange(e.target.files?.[0] ?? null)}
          />
        </label>
        {importing && <p className="text-xs text-ink-500 mt-1">Đang phân tích placeholder…</p>}
        {importError && <p className="text-xs text-danger mt-1">{importError}</p>}
        {placeholders.length > 0 && (
          <p className="text-xs text-ink-600 mt-1">
            Detected <strong>{placeholders.length}</strong> placeholder ({placeholders.filter(p => p.pattern === 'guillemets').length} MERGEFIELD).
          </p>
        )}
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="label">Mã <span className="text-danger">*</span></label>
          <input className="input font-mono" value={code} onChange={(e) => setCode(e.target.value)} placeholder="HD_BH_2026" required />
        </div>
        <div>
          <label className="label">Tên <span className="text-danger">*</span></label>
          <input className="input" value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
      </div>
      <div>
        <label className="label">Phân loại</label>
        <input className="input" value={category} onChange={(e) => setCategory(e.target.value)} placeholder="Vd: Hợp đồng bảo hiểm" />
      </div>
      {error && <div className="text-xs text-danger">{error}</div>}
      <div className="flex justify-end gap-2 pt-2">
        <button type="submit" className="btn-primary" disabled={!canSubmit}>
          {isSubmitting ? 'Đang tạo…' : 'Tạo template'}
        </button>
      </div>
    </form>
  );
}
