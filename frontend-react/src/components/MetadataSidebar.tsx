import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { GripVerticalIcon, SearchIcon } from 'lucide-react';
import { METADATA_GROUPS, metadataApi, type MetadataDto } from '@/api/metadata';
import clsx from 'clsx';

interface Props {
  onInsert: (m: MetadataDto) => void;
}

export function MetadataSidebar({ onInsert }: Props) {
  const [keyword, setKeyword] = useState('');
  const [openGroups, setOpenGroups] = useState<Set<string>>(new Set(['B', 'C']));

  const { data: items = [] } = useQuery({
    queryKey: ['metadata-all'],
    queryFn: () => metadataApi.search(),
  });

  const groupLabel = useMemo(() => Object.fromEntries(METADATA_GROUPS.map((g) => [g.value, g.label])), []);

  const filtered = useMemo(() => {
    const kw = keyword.trim().toUpperCase();
    return kw ? items.filter((m) => m.value.includes(kw) || m.label.toUpperCase().includes(kw)) : items;
  }, [items, keyword]);

  const grouped = useMemo(() => {
    const map = new Map<string, MetadataDto[]>();
    for (const m of filtered) {
      const k = m.fieldGroup ?? '?';
      const arr = map.get(k) ?? [];
      arr.push(m);
      map.set(k, arr);
    }
    const order = METADATA_GROUPS.map((g) => g.value);
    return [...map.entries()].sort(([a], [b]) => {
      const ia = order.indexOf(a), ib = order.indexOf(b);
      if (ia === -1 && ib === -1) return a.localeCompare(b);
      if (ia === -1) return 1;
      if (ib === -1) return -1;
      return ia - ib;
    });
  }, [filtered]);

  function toggle(key: string) {
    const next = new Set(openGroups);
    if (next.has(key)) next.delete(key); else next.add(key);
    setOpenGroups(next);
  }

  // Drag-drop: payload chứa value để OnlyOffice editor (qua bridge postMessage) insert tại drop point.
  function onDragStart(e: React.DragEvent, m: MetadataDto) {
    e.dataTransfer.setData('application/x-form-mgmt-metadata', JSON.stringify({ value: m.value, label: m.label }));
    e.dataTransfer.setData('text/plain', `«${m.value}»`);
    e.dataTransfer.effectAllowed = 'copy';
  }

  return (
    <aside className="w-72 shrink-0 border-r border-ink-200 bg-ink-50/40 flex flex-col overflow-hidden">
      <header className="px-3 py-2 border-b border-ink-200">
        <div className="text-xs font-semibold">Metadata</div>
        <p className="text-[11px] text-ink-500 mt-0.5">Click để chèn MERGEFIELD vào cursor.</p>
      </header>
      <div className="p-2 border-b border-ink-200">
        <div className="relative">
          <SearchIcon size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-ink-400" />
          <input
            className="input pl-7 text-xs py-1.5"
            placeholder="Tìm trường…"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-1">
        {grouped.length === 0 ? (
          <p className="text-xs text-ink-500 p-3 text-center">Không có field nào.</p>
        ) : (
          grouped.map(([key, list]) => (
            <div key={key} className="mb-1">
              <button
                type="button"
                className="w-full flex items-center gap-2 px-2 py-1.5 rounded text-xs font-medium hover:bg-ink-100"
                onClick={() => toggle(key)}
              >
                <span className="text-ink-500 w-3 text-[10px]">{openGroups.has(key) ? '▾' : '▸'}</span>
                <span className="font-mono font-bold">{key}</span>
                <span className="flex-1 text-left text-ink-600 truncate">{groupLabel[key] ?? ''}</span>
                <span className="text-[10px] text-ink-500 bg-ink-200/60 rounded-full px-1.5">{list.length}</span>
              </button>
              {openGroups.has(key) && (
                <ul className="pl-3 mt-0.5">
                  {list.map((m) => (
                    <li key={m.id}>
                      <button
                        type="button"
                        draggable
                        onDragStart={(e) => onDragStart(e, m)}
                        // KHÔNG preventDefault mousedown vì sẽ chặn drag-init. Plugin PasteText
                        // xử lý cursor native — không cần giữ iframe focus.
                        onClick={() => onInsert(m)}
                        className={clsx(
                          'w-full text-left px-2 py-1.5 rounded flex items-start gap-1.5',
                          'hover:bg-white hover:border-ink-200 border border-transparent cursor-grab active:cursor-grabbing'
                        )}
                        title={m.description ?? m.label}
                      >
                        <GripVerticalIcon size={12} className="text-ink-300 mt-0.5 shrink-0" />
                        <div className="min-w-0 flex-1">
                          <div className="text-[11px] font-mono font-semibold truncate">{m.value}</div>
                          <div className="text-[11px] text-ink-500 truncate">{m.label}</div>
                        </div>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ))
        )}
      </div>
    </aside>
  );
}
