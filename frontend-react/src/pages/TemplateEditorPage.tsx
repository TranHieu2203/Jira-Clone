import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { DocumentEditor } from '@onlyoffice/document-editor-react';
import { ArrowLeftIcon, RefreshCwIcon, SendIcon } from 'lucide-react';
import { templateApi } from '@/api/template';
import { metadataApi, type MetadataDto } from '@/api/metadata';
import { useAuthStore } from '@/stores/auth';
import { MetadataSidebar } from '@/components/MetadataSidebar';

const DOC_SERVER_URL = import.meta.env.VITE_DOCSERVER_URL ?? 'http://localhost:8080/';
const API_BASE_FOR_DOCSERVER = import.meta.env.VITE_API_FOR_DOCSERVER ?? 'http://api:8080';
const PLUGIN_CONFIG_URL =
  import.meta.env.VITE_OO_PLUGIN_URL ?? `${window.location.origin}/plugins/formmgmt/config.json`;

/**
 * OnlyOffice plugin bridge wiring:
 *  - Plugin được load qua editorConfig.plugins.pluginsData (DocServer fetch config.json).
 *  - Plugin chạy iframe same-origin host (cùng :3000) → postMessage 2 chiều qua e.source.
 *  - Click sidebar / drop / @mention đều route qua plugin để insert tại cursor bằng Builder API.
 *  - Hết phần plugin, BE mail-merge vẫn detect cả real MERGEFIELD lẫn plain text «VALUE» (regex).
 */
export default function TemplateEditorPage() {
  const { id } = useParams<{ id: string }>();
  const nav = useNavigate();
  const user = useAuthStore((s) => s.user);
  const editorWrapperRef = useRef<HTMLDivElement>(null);
  const pluginWindowRef = useRef<Window | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [editorReady, setEditorReady] = useState(false);
  const [pluginReady, setPluginReady] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  // @mention: trigger keyword đã match + filtered metadata để render popup ở host.
  const [mention, setMention] = useState<{ trigger: string; query: string; items: MetadataDto[]; activeIdx: number } | null>(null);

  const { data: template, isLoading } = useQuery({
    queryKey: ['template', id],
    queryFn: () => templateApi.getById(id!),
    enabled: !!id,
  });
  const { data: metadataItems = [] } = useQuery({
    queryKey: ['metadata-all'],
    queryFn: () => metadataApi.search(),
  });

  const docKey = useMemo(
    () => (template ? `${template.id}-v${template.version}-${reloadKey}` : null),
    [template, reloadKey]
  );

  const editorConfig = useMemo(() => {
    if (!template || !docKey) return null;
    return {
      documentType: 'word',
      document: {
        fileType: 'docx',
        key: docKey,
        title: `${template.code} — ${template.name}.docx`,
        url: `${API_BASE_FOR_DOCSERVER}/api/v1/form-management/templates/${template.id}/file`,
        permissions: { edit: true, download: true, print: true, fillForms: true },
      },
      editorConfig: {
        callbackUrl: `${API_BASE_FOR_DOCSERVER}/api/v1/form-management/templates/${template.id}/callback`,
        user: { id: user?.id ?? 'anon', name: user?.displayName ?? 'Anonymous' },
        lang: 'vi',
        customization: { autosave: true, forcesave: true, chat: false, comments: false },
        plugins: {
          autostart: ['asc.{F0124567-1234-4321-9876-ABCDEF012350}'],
          pluginsData: [PLUGIN_CONFIG_URL],
        },
      },
    };
  }, [template, docKey, user]);

  // ============ Host ↔ Plugin postMessage bridge ============
  useEffect(() => {
    function onMsg(e: MessageEvent) {
      const d = e.data as { src?: string; type?: string; text?: string } | undefined;
      if (!d || d.src !== 'formmgmt-plugin') return;
      if (d.type === 'ready') {
        // e.source phải là Window khác host (plugin iframe). Tránh save host window vào ref
        // (xảy ra khi synthetic dispatch từ host page làm test).
        if (e.source && e.source !== window) {
          pluginWindowRef.current = e.source as Window;
          setPluginReady(true);
          // eslint-disable-next-line no-console
          console.log('[FormMgmt] plugin ready, src window saved');
          postMetadataToPlugin();
        }
        return;
      }
      if (d.type === 'request-metadata') {
        postMetadataToPlugin();
        return;
      }
      // Mention-probe path đã thay bằng OnlyOffice native InputHelper trong plugin — không cần ở host.
    }
    window.addEventListener('message', onMsg);
    return () => window.removeEventListener('message', onMsg);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [metadataItems]);

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  function _unused_handleMentionProbe(text: string) {
    // Tìm trigger '@KEYWORD' BẤT CỨ ĐÂU trong doc (cursor có thể ở giữa). Yêu cầu:
    //  - '@' ở đầu chuỗi HOẶC đứng sau ký tự không phải word (tránh email foo@bar.com).
    //  - Lấy match CUỐI CÙNG — heuristic: nó nhiều khả năng là chỗ user vừa gõ.
    const re = /(^|[^A-Za-z0-9_])@([A-Za-z0-9_]*)/g;
    let last: RegExpExecArray | null = null;
    let m: RegExpExecArray | null;
    while ((m = re.exec(text)) !== null) last = m;
    if (!last) {
      if (mention) setMention(null);
      return;
    }
    const keyword = last[2] || '';
    const trigger = '@' + keyword;
    const query = keyword.toUpperCase();
    const filtered = (metadataItems ?? [])
      .filter(
        (it) =>
          (it.value || '').toUpperCase().includes(query) ||
          (it.label || '').toUpperCase().includes(query)
      )
      .slice(0, 8);
    if (filtered.length === 0) {
      if (mention) setMention(null);
      return;
    }
    setMention({ trigger, query, items: filtered, activeIdx: 0 });
  }

  function pickMention(item: MetadataDto) {
    const w = pluginWindowRef.current;
    if (!w || !mention) return;
    w.postMessage(
      {
        target: 'formmgmt-plugin',
        type: 'replace-trigger',
        trigger: mention.trigger,
        value: item.value,
      },
      '*'
    );
    setMention(null);
  }

  function postMetadataToPlugin() {
    const w = pluginWindowRef.current;
    if (!w) return;
    w.postMessage(
      {
        target: 'formmgmt-plugin',
        type: 'metadata-list',
        items: (metadataItems ?? []).map((m) => ({ value: m.value, label: m.label })),
      },
      '*'
    );
  }

  function postInsertToPlugin(value: string) {
    const w = pluginWindowRef.current;
    if (!w) {
      // eslint-disable-next-line no-console
      console.warn('[FormMgmt] plugin chưa ready — bỏ qua insert', value);
      return;
    }
    w.postMessage({ target: 'formmgmt-plugin', type: 'insert', value }, '*');
  }

  function insertMergeField(m: MetadataDto) {
    postInsertToPlugin(m.value);
  }

  // ============ Drag-drop overlay ============
  // Vấn đề: drop trên iframe OnlyOffice (cross-origin) bypass React's onDrop. Browser fires native
  // drop event INSIDE iframe, host page không nhận được.
  // Giải pháp: trong khi user đang drag từ sidebar (có MIME riêng), bật overlay div phủ lên iframe
  // với pointer-events:auto → overlay nuốt dragover/drop trước khi iframe nhận → insert via plugin.
  useEffect(() => {
    function onDragStart(e: DragEvent) {
      const types = e.dataTransfer?.types;
      if (types && Array.from(types).includes('application/x-form-mgmt-metadata')) {
        setDragActive(true);
      }
    }
    function onDragEnd() {
      setDragActive(false);
    }
    // Bubble phase: listener chạy SAU khi sidebar's onDragStart đã call setData() → types đã có MIME.
    window.addEventListener('dragstart', onDragStart, false);
    window.addEventListener('dragend', onDragEnd, false);
    window.addEventListener('drop', onDragEnd, false);
    return () => {
      window.removeEventListener('dragstart', onDragStart, false);
      window.removeEventListener('dragend', onDragEnd, false);
      window.removeEventListener('drop', onDragEnd, false);
    };
  }, []);

  // Overlay capture drop + route qua plugin với cursor đã track. OnlyOffice cross-origin canvas
  // không reliably accept text/plain drops từ external origin (browser shows not-allowed),
  // nên ta tự handle drop trên overlay và insert tại vị trí cursor đã track gần nhất.
  // Trade-off: drop không bám đúng toạ độ thả; nhưng nhất quán với click-insert.
  function onOverlayDragOver(e: React.DragEvent) {
    if (e.dataTransfer.types.includes('application/x-form-mgmt-metadata')) {
      e.preventDefault();
      e.dataTransfer.dropEffect = 'copy';
    }
  }
  function onOverlayDrop(e: React.DragEvent) {
    e.preventDefault();
    setDragActive(false);
    const raw = e.dataTransfer.getData('application/x-form-mgmt-metadata');
    if (!raw) return;
    try {
      const data = JSON.parse(raw) as { value: string };
      if (data.value) postInsertToPlugin(data.value);
    } catch {
      /* */
    }
  }

  useEffect(() => {
    if (editorReady) {
      // eslint-disable-next-line no-console
      console.log('[OnlyOffice] editor app ready');
    }
  }, [editorReady]);

  if (isLoading) return <div className="p-6 text-sm text-ink-500">Đang tải template…</div>;
  if (!template) return <div className="p-6 text-sm text-danger">Không tìm thấy template.</div>;

  return (
    <div className="h-screen flex flex-col">
      <header className="flex items-center justify-between px-4 py-2 border-b border-ink-200 bg-white">
        <div className="flex items-center gap-3">
          <button className="btn-ghost btn-sm" onClick={() => nav('/templates')}>
            <ArrowLeftIcon size={14} /> Quay lại
          </button>
          <div>
            <div className="text-sm font-semibold">{template.name}</div>
            <div className="text-xs text-ink-500 font-mono">
              {template.code} · v{template.version}
              <span className={`ml-2 ${pluginReady ? 'text-success' : 'text-ink-400'}`}>
                {pluginReady ? '● plugin ready' : '○ plugin loading…'}
              </span>
            </div>
            <div className="text-[11px] text-ink-500 mt-0.5">
              💡 Click vào vị trí muốn chèn trong doc <span className="font-semibold">trước</span>, rồi click/drag metadata
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button className="btn-ghost btn-sm" onClick={() => setReloadKey((k) => k + 1)}>
            <RefreshCwIcon size={14} /> Reload
          </button>
          <button className="btn-primary btn-sm" onClick={() => nav(`/templates/${template.id}/submit`)}>
            <SendIcon size={14} /> Mail-merge
          </button>
        </div>
      </header>
      <div className="flex-1 min-h-0 flex">
        <MetadataSidebar onInsert={insertMergeField} />
        <div className="flex-1 min-w-0 relative" ref={editorWrapperRef}>
          {editorConfig && (
            <DocumentEditor
              id={`onlyoffice-editor-${template.id}`}
              documentServerUrl={DOC_SERVER_URL}
              config={editorConfig as any}
              events_onAppReady={(() => setEditorReady(true)) as any}
              onLoadComponentError={(code, desc) => console.error('[OnlyOffice] Load error', code, desc)}
            />
          )}
          {/* @mention native popup giờ chạy bởi OnlyOffice InputHelper trong canvas — KHÔNG cần host overlay.
              Mention state vẫn giữ làm legacy fallback nếu InputHelper không khả dụng. */}
          {false && mention && (
            <div className="absolute bottom-6 right-6 w-72 bg-white border border-ink-300 rounded-md shadow-lg overflow-hidden z-30">
              <div className="px-3 py-1.5 text-[11px] bg-ink-50 border-b border-ink-200 flex items-center justify-between">
                <span>
                  Gợi ý cho <span className="font-mono font-semibold">{mention.trigger}</span>
                </span>
                <button
                  className="text-ink-400 hover:text-ink-700"
                  onClick={() => setMention(null)}
                  title="Đóng (Esc)"
                >
                  ✕
                </button>
              </div>
              <ul className="max-h-72 overflow-y-auto">
                {mention.items.map((it, idx) => (
                  <li key={it.id}>
                    <button
                      type="button"
                      className={
                        'w-full text-left px-3 py-1.5 flex items-start gap-2 hover:bg-ink-50 ' +
                        (idx === mention.activeIdx ? 'bg-ink-100' : '')
                      }
                      onClick={() => pickMention(it)}
                    >
                      <div className="min-w-0 flex-1">
                        <div className="text-xs font-mono font-semibold truncate">{it.value}</div>
                        <div className="text-[11px] text-ink-500 truncate">{it.label}</div>
                      </div>
                    </button>
                  </li>
                ))}
              </ul>
              <div className="px-3 py-1 text-[10px] text-ink-400 border-t border-ink-100">
                Click để chèn — sẽ thay {mention.trigger} bằng «VALUE»
              </div>
            </div>
          )}
          {/* Overlay capture drop. Insert tại vị trí cursor cuối cùng đã track. */}
          {dragActive && (
            <div
              className="absolute inset-0 border-2 border-dashed border-ink-700 bg-ink-900/5"
              onDragOver={onOverlayDragOver}
              onDrop={onOverlayDrop}
            >
              <div className="absolute top-3 left-1/2 -translate-x-1/2 text-xs font-semibold bg-white px-3 py-1.5 rounded shadow border border-ink-200">
                Thả để chèn tại vị trí con trỏ cuối cùng trong doc
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
