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
  // @mention: trigger keyword đã match + filtered metadata để render popup ở host.
  const [mention, setMention] = useState<{ trigger: string; query: string; items: MetadataDto[]; activeIdx: number } | null>(null);
  const mentionPopupRef = useRef<HTMLDivElement>(null);

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
          autostart: ['asc.{F0124567-1234-4321-9876-ABCDEF012360}'],
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
      const m = d as { type: string; trigger?: string };
      if (m.type === 'mention-show') {
        // User gõ '@' trong editor → plugin detect → host mở popup native style.
        // trigger = '@' (ký tự user vừa gõ). Popup search input auto focus →
        // user gõ filter tiếp + Up/Down/Enter để pick.
        openMentionPopup(String(m.trigger || '@'));
        return;
      }
      if (m.type === 'mention-hide') {
        // KHÔNG auto-close khi plugin hide event — user có thể đang gõ trong popup search.
        // Chỉ hide nếu popup đang trong trạng thái mention-from-@ (trigger không empty).
        return;
      }
    }
    window.addEventListener('message', onMsg);
    return () => window.removeEventListener('message', onMsg);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [metadataItems]);

  /** Mở popup chèn metadata. trigger='' khi gọi từ button/hotkey, trigger='@' khi từ plugin. */
  function openMentionPopup(trigger: string = '') {
    const filtered = (metadataItems ?? []).slice(0, 8);
    setMention({ trigger, query: '', items: filtered, activeIdx: 0 });
  }

  /** Update filter trong popup khi user gõ vào search input. */
  function updateMentionQuery(query: string) {
    const q = query.toUpperCase();
    const filtered = (metadataItems ?? [])
      .filter(
        (it) =>
          (it.value || '').toUpperCase().includes(q) ||
          (it.label || '').toUpperCase().includes(q)
      )
      .slice(0, 8);
    setMention((m) => (m ? { ...m, query, items: filtered, activeIdx: 0 } : null));
  }

  function pickMention(item: MetadataDto) {
    // eslint-disable-next-line no-console
    console.log('[FormMgmt] pickMention called: trigger=', mention?.trigger, 'item=', item?.value, 'pluginRef=', !!pluginWindowRef.current);
    const w = pluginWindowRef.current;
    if (!w) return;
    if (mention && mention.trigger && mention.trigger.charAt(0) === '@') {
      // eslint-disable-next-line no-console
      console.log('[FormMgmt] posting replace-trigger:', mention.trigger, '→', item.value);
      w.postMessage(
        { target: 'formmgmt-plugin', type: 'replace-trigger', trigger: mention.trigger, value: item.value },
        '*'
      );
    } else {
      // eslint-disable-next-line no-console
      console.log('[FormMgmt] posting insert:', item.value);
      postInsertToPlugin(item.value);
    }
    setMention(null);
    returnFocusToEditor();
  }

  // Auto-focus popup khi vừa mở. KEY ISSUE: browser KHÔNG tự transfer focus từ cross-origin
  // iframe (OnlyOffice canvas) sang host element. Phải force qua nhiều bước:
  //   1. iframe.blur() — bỏ focus khỏi iframe element
  //   2. setTimeout — đợi React render input + browser xử lý blur
  //   3. input.focus() — đặt focus vào popup search input
  useEffect(() => {
    if (!mention) return;
    const editorIframe = document.querySelector('iframe[name="frameEditor"]') as HTMLIFrameElement | null;
    editorIframe?.blur();
    if ((document.activeElement as HTMLElement | null)?.blur) {
      (document.activeElement as HTMLElement).blur();
    }
    const timer = window.setTimeout(() => {
      const input = document.querySelector('input[placeholder*="Tìm theo"]') as HTMLInputElement | null;
      input?.focus();
      // eslint-disable-next-line no-console
      console.log('[FormMgmt] focus shift → activeEl=', document.activeElement?.tagName, 'isInput=', document.activeElement === input);
    }, 80);
    return () => window.clearTimeout(timer);
  }, [mention?.trigger]);

  function returnFocusToEditor() {
    const w = pluginWindowRef.current;
    if (w) w.postMessage({ target: 'formmgmt-plugin', type: 'focus-editor' }, '*');
  }

  // Keyboard handler khi popup mở. Document-level keydown capture phase.
  useEffect(() => {
    if (!mention) return;
    function onKey(e: KeyboardEvent) {
      if (!mention) return;
      // eslint-disable-next-line no-console
      console.log('[FormMgmt] popup keydown:', e.key, 'shift=', e.shiftKey, 'activeEl=', document.activeElement?.tagName);
      if (e.key === 'ArrowDown' || (e.key === 'Tab' && !e.shiftKey)) {
        e.preventDefault();
        e.stopPropagation();
        setMention((m) => (m ? { ...m, activeIdx: (m.activeIdx + 1) % m.items.length } : null));
      } else if (e.key === 'ArrowUp' || (e.key === 'Tab' && e.shiftKey)) {
        e.preventDefault();
        e.stopPropagation();
        setMention((m) => (m ? { ...m, activeIdx: (m.activeIdx - 1 + m.items.length) % m.items.length } : null));
      } else if (e.key === 'Enter') {
        e.preventDefault();
        e.stopPropagation();
        const it = mention.items[mention.activeIdx];
        if (it) pickMention(it);
      } else if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        setMention(null);
        returnFocusToEditor();
      }
    }
    window.addEventListener('keydown', onKey, true);
    return () => window.removeEventListener('keydown', onKey, true);
  }, [mention]);

  // Hotkey toàn cục Ctrl+Shift+M / Cmd+Shift+M để mở popup. Chọn combo có Shift để tránh đụng
  // shortcut native của OnlyOffice (Ctrl+M có thể được editor dùng).
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && (e.key === 'M' || e.key === 'm')) {
        e.preventDefault();
        e.stopPropagation();
        openMentionPopup();
      }
    }
    window.addEventListener('keydown', onKey, true);
    return () => window.removeEventListener('keydown', onKey, true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [metadataItems]);

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

  // Drag-drop đã bị bỏ — OnlyOffice cross-iframe drop không reliable position theo toạ độ thả.
  // Chỉ giữ click-insert. (User confirmed bỏ feature này.)

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
              💡 Gõ <span className="font-mono font-semibold">@</span> trong doc, hoặc <kbd className="px-1 py-0.5 bg-ink-100 border border-ink-300 rounded font-mono text-[10px]">Ctrl+Shift+M</kbd>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button
            className="btn-ghost btn-sm"
            onClick={openMentionPopup}
            title="Mở popup chèn MERGEFIELD (Ctrl+Shift+M)"
          >
            📋 Chèn metadata <kbd className="ml-1 px-1 py-0.5 bg-ink-100 border border-ink-300 rounded font-mono text-[10px]">Ctrl+Shift+M</kbd>
          </button>
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
              id={`onlyoffice-editor-${template.id}-${reloadKey}`}
              documentServerUrl={DOC_SERVER_URL}
              config={editorConfig as any}
              events_onAppReady={(() => setEditorReady(true)) as any}
              onLoadComponentError={(code, desc) => console.error('[OnlyOffice] Load error', code, desc)}
            />
          )}
          {/* Popup chèn metadata. Mở qua button hoặc Ctrl+M. Focus tự nhảy về search input
              → user gõ filter + Up/Down/Enter/Esc đều work vì host page có focus. */}
          {mention && (
            <div
              ref={mentionPopupRef}
              className="absolute top-4 left-1/2 -translate-x-1/2 w-96 bg-white border border-ink-300 rounded-md shadow-lg overflow-hidden z-30"
            >
              <div className="px-3 py-2 bg-ink-50 border-b border-ink-200 flex items-center justify-between">
                <span className="text-xs font-semibold">Chèn MERGEFIELD vào cursor</span>
                <button
                  className="text-ink-400 hover:text-ink-700"
                  onClick={() => { setMention(null); returnFocusToEditor(); }}
                  title="Đóng (Esc)"
                >
                  ✕
                </button>
              </div>
              <div className="px-3 py-2 border-b border-ink-200">
                <input
                  autoFocus
                  type="text"
                  value={mention.query}
                  onChange={(e) => updateMentionQuery(e.target.value)}
                  placeholder="Tìm theo mã hoặc tên trường…"
                  className="w-full px-2 py-1.5 text-sm border border-ink-300 rounded focus:outline-none focus:ring-2 focus:ring-ink-700/20"
                />
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
                ↑↓ chọn · Enter chèn · Esc đóng
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
