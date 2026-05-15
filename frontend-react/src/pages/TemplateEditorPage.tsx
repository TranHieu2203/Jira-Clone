import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { DocumentEditor } from '@onlyoffice/document-editor-react';
import { ArrowLeftIcon, DownloadIcon, RefreshCwIcon, SaveIcon, SendIcon } from 'lucide-react';
import { templateApi, type TemplateDetail } from '@/api/template';
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
  const queryClient = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const editorWrapperRef = useRef<HTMLDivElement>(null);
  const pluginWindowRef = useRef<Window | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [editorReady, setEditorReady] = useState(false);
  const [pluginReady, setPluginReady] = useState(false);
  // Autosave: mặc định OFF (tránh popup error periodic). User có thể bật bằng checkbox.
  // Persist trong localStorage để giữ preference qua sessions.
  const [autosaveOn, setAutosaveOn] = useState<boolean>(() => {
    try { return localStorage.getItem('fm.autosave') === '1'; } catch { return false; }
  });
  const [saveBusy, setSaveBusy] = useState(false);
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

  // KHÔNG include autosaveOn trong docKey — nếu include, toggle sẽ làm DocServer thấy 2 session
  // khác nhau → cache content không share → user thấy "data khác nhau" giữa on/off.
  // Customization.autosave thay đổi chỉ áp dụng khi editor RELOAD thủ công (click Reload button).
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
        // autosave: theo state user toggle. forcesave: true → cho phép trigger save thủ công
        // (Ctrl+S trong editor hoặc serviceCommand từ host) qua callback status=6.
        customization: { autosave: autosaveOn, forcesave: true, chat: false, comments: false },
        plugins: {
          autostart: ['asc.{F0124567-1234-4321-9876-ABCDEF01236D}'],
          pluginsData: [PLUGIN_CONFIG_URL],
        },
      },
    };
  }, [template, docKey, user, autosaveOn]);

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
    // Hai approach song song để chắc chắn focus về editor:
    //   1. postMessage plugin gọi executeMethod('SetEditorFocus') — OnlyOffice native
    //   2. focus() trực tiếp iframe element trên host — fallback nếu (1) fail
    const w = pluginWindowRef.current;
    if (w) w.postMessage({ target: 'formmgmt-plugin', type: 'focus-editor' }, '*');
    const iframe = document.querySelector('iframe[name="frameEditor"]') as HTMLIFrameElement | null;
    iframe?.focus();
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

  /**
   * Background poller: detect Office Save (user bấm Save trong toolbar OnlyOffice → DS tự fire
   * callback status=6 → BE wrap + persist + version bump). FE không có signal trực tiếp →
   * poll /templates/{id} mỗi 3s, nếu version cao hơn version đang render → gọi
   * docEditor.refreshFile() để reload bytes mới (đã wrapped MERGEFIELD) vào editor IN-PLACE.
   *
   * Skip khi saveBusy (đang chạy saveTemplate handler — handler đó tự refreshFile rồi).
   */
  useEffect(() => {
    if (!template?.id) return;
    const tplId = template.id;
    let lastSeenVersion = template.version;
    const interval = window.setInterval(async () => {
      if (saveBusy) return;
      try {
        const fresh = await templateApi.getById(tplId);
        if (fresh.version > lastSeenVersion) {
          // eslint-disable-next-line no-console
          console.log(`[FormMgmt] poller detected version bump ${lastSeenVersion} → ${fresh.version} (Office Save?)`);
          lastSeenVersion = fresh.version;
          queryClient.setQueryData(['template', tplId], fresh);

          const editorId = `onlyoffice-editor-${tplId}-${reloadKey}`;
          const inst = (window as unknown as { DocEditor?: { instances?: Record<string, unknown> } }).DocEditor?.instances?.[editorId] as
            | { refreshFile?: (config: unknown) => void }
            | undefined;
          if (inst?.refreshFile) {
            inst.refreshFile({
              document: {
                fileType: 'docx',
                key: `${fresh.id}-v${fresh.version}-${reloadKey}`,
                title: `${fresh.code} — ${fresh.name}.docx`,
                url: `${API_BASE_FOR_DOCSERVER}/api/v1/form-management/templates/${fresh.id}/file`,
              },
            });
          }
        }
      } catch {
        // silent — network blip, retry next tick
      }
    }, 3000);
    return () => window.clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [template?.id, reloadKey, saveBusy]);

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
    // Sau insert, focus về editor để user tiếp tục gõ ngay phía sau «VALUE».
    // setTimeout đợi PasteText hoàn tất + cursor positioned by OnlyOffice.
    setTimeout(returnFocusToEditor, 50);
  }

  function toggleAutosave(next: boolean) {
    // Chỉ cập nhật state + persist. docKey KHÔNG đổi nên DocServer session giữ nguyên, content không mất.
    // Setting autosave trong OnlyOffice chỉ áp dụng khi editor mount mới — user cần bấm Reload
    // hoặc F5 để config mới active. Hiển thị hint qua title button.
    setAutosaveOn(next);
    try { localStorage.setItem('fm.autosave', next ? '1' : '0'); } catch { /* */ }
  }

  /** Save template — chạy CÙNG flow như khi user bấm Save trong toolbar OnlyOffice:
   *  1. POST /trigger-save → BE proxy lên DS CommandService.ashx { c:"forcesave", key:docKey }.
   *  2. DS nhận → flush cached doc → fire callback status=6 tới /callback → BE Wrap «...»
   *     thành MERGEFIELD + ExtractUsedFields + persist DB version bump.
   *  3. FE poll /templates/{id} mỗi 250ms (max 6s) để detect version bump.
   *  4. Khi version bump → gọi docEditor.refreshFile() reload bytes IN-PLACE — KHÔNG unmount
   *     React component (tránh removeChild crash + popup "Đã thay đổi phiên bản").
   *
   *  Office Save toolbar button (DS internal) cũng đi qua /callback flow này → background
   *  poller (xem useEffect bên dưới) cũng auto refreshFile khi version bump từ Office Save. */
  async function saveTemplate() {
    if (!template) return;
    setSaveBusy(true);
    const editorId = `onlyoffice-editor-${template.id}-${reloadKey}`;
    const inst = (window as unknown as { DocEditor?: { instances?: Record<string, unknown> } }).DocEditor?.instances?.[editorId] as
      | { refreshFile?: (config: unknown) => void; serviceCommand?: (cmd: string, data?: unknown) => void }
      | undefined;
    const startVersion = template.version;
    const docKey = `${template.id}-v${template.version}-${reloadKey}`;
    try {
      // Step 1: trigger DS force-save (BE proxy CommandService).
      const resp = await templateApi.triggerSave(template.id, docKey);
      // eslint-disable-next-line no-console
      console.log('[FormMgmt] trigger-save resp:', resp);

      // Step 2: poll for version bump. Callback fires async on DS side (~500-2000ms).
      let bumpedTemplate: TemplateDetail | null = null;
      for (let i = 0; i < 30; i++) {
        await new Promise((r) => setTimeout(r, 200));
        try {
          const fresh = await templateApi.getById(template.id);
          if (fresh.version > startVersion) {
            bumpedTemplate = fresh;
            // Sync React Query cache với fresh data → các hook khác (vd sidebar count) auto re-render.
            queryClient.setQueryData(['template', template.id], fresh);
            break;
          }
        } catch {
          // network blip, retry
        }
      }

      if (!bumpedTemplate) {
        // eslint-disable-next-line no-console
        console.warn('[FormMgmt] save timed out — no version bump trong 6s. Có thể DS không có changes để save (error:4).');
        return;
      }

      // Step 3: reload editor IN-PLACE bằng OO refreshFile API. KHÔNG remount → không crash.
      const newDocKey = `${bumpedTemplate.id}-v${bumpedTemplate.version}-${reloadKey}`;
      const newConfig = {
        document: {
          fileType: 'docx',
          key: newDocKey,
          title: `${bumpedTemplate.code} — ${bumpedTemplate.name}.docx`,
          url: `${API_BASE_FOR_DOCSERVER}/api/v1/form-management/templates/${bumpedTemplate.id}/file`,
        },
        editorConfig: {
          callbackUrl: `${API_BASE_FOR_DOCSERVER}/api/v1/form-management/templates/${bumpedTemplate.id}/callback`,
        },
      };
      if (inst?.refreshFile) {
        inst.refreshFile(newConfig);
        // eslint-disable-next-line no-console
        console.log('[FormMgmt] save complete: refreshFile() called với key=', newDocKey);
      } else {
        // eslint-disable-next-line no-console
        console.warn('[FormMgmt] refreshFile API không có — DS version cũ. User cần F5.');
      }
    } catch (e) {
      // eslint-disable-next-line no-console
      console.error('[FormMgmt] saveTemplate failed:', e);
    } finally {
      setSaveBusy(false);
    }
  }

  /** Download template DOCX hiện đang lưu ở BE. KHÔNG bao gồm edits chưa save trong editor —
   *  để có version mới nhất, click "Lưu template" trước rồi download. */
  async function downloadTemplate() {
    if (!template) return;
    try {
      const res = await fetch(`/api/v1/form-management/templates/${template.id}/file`);
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${template.code}.docx`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (e) {
      // eslint-disable-next-line no-console
      console.error('[FormMgmt] download template fail:', e);
      alert('Tải template thất bại — kiểm tra console log.');
    }
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
          </div>
        </div>
        <div className="flex items-center gap-2">
          
          <label
            className="flex items-center gap-1 text-xs select-none cursor-pointer"
            title="Bật để OnlyOffice tự save định kỳ. Đổi setting xong cần bấm Reload để editor áp dụng."
          >
            <input
              type="checkbox"
              checked={autosaveOn}
              onChange={(e) => toggleAutosave(e.target.checked)}
              className="cursor-pointer"
            />
            Autosave
          </label>
          <button
            className="btn-ghost btn-sm"
            onClick={saveTemplate}
            disabled={saveBusy}
            title="Lưu template về server (Ctrl+S trong editor cũng work)"
          >
            <SaveIcon size={14} /> {saveBusy ? 'Đang lưu…' : 'Lưu template'}
          </button>
          <button className="btn-ghost btn-sm" onClick={downloadTemplate} title="Tải template DOCX về máy">
            <DownloadIcon size={14} /> Tải template
          </button>
          <button
            className="btn-ghost btn-sm"
            onClick={() => window.location.reload()}
            title="Reload editor với bytes mới nhất từ server (full page reload — tránh canvas trắng do OO wrapper bug)"
          >
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
              // KHÔNG dùng `key` prop — sẽ trigger React unmount → OnlyOffice library internal
              // cleanup xung đột với React DOM removal → crash `removeChild`. Chỉ thay `id` prop:
              // OnlyOffice React wrapper detect id change + destroy/recreate iframe internally
              // (đã chứng minh work với nút Reload).
              id={`onlyoffice-editor-${template.id}-${reloadKey}`}
              documentServerUrl={DOC_SERVER_URL}
              config={editorConfig as any}
              events_onAppReady={(() => setEditorReady(true)) as any}
              onLoadComponentError={(code, desc) => console.error('[OnlyOffice] Load error', code, desc)}
            />
          )}
          {saveBusy && (
            <div className="absolute inset-0 flex items-center justify-center bg-white/80 z-20 pointer-events-auto">
              <div className="flex flex-col items-center gap-3 text-sm text-ink-700">
                <div className="h-8 w-8 border-2 border-ink-300 border-t-ink-700 rounded-full animate-spin" />
                <div>Đang lưu tài liệu...</div>
              </div>
            </div>
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
