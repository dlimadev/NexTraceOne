import { useState, useRef, useEffect } from 'react';
import { Bookmark, Trash2, ChevronDown } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

interface SavedViewItem {
  id: string;
  userId: string;
  context: string;
  name: string;
  description?: string;
  filtersJson?: string;
  isShared: boolean;
  isOwn: boolean;
  sortOrder: number;
  createdAt: string;
}

interface SavedViewsResponse {
  items: SavedViewItem[];
}

interface SavedViewSelectorProps {
  context: string;
  currentFilters: Record<string, unknown>;
  onApply: (filters: Record<string, unknown>) => void;
}

async function fetchSavedViews(context: string): Promise<SavedViewsResponse> {
  const resp = await fetch(`/api/v1/user-saved-views?context=${encodeURIComponent(context)}`);
  if (!resp.ok) throw new Error('Failed to fetch saved views');
  return resp.json();
}

async function createSavedView(context: string, name: string, filtersJson: string, isShared: boolean): Promise<SavedViewItem> {
  const resp = await fetch('/api/v1/user-saved-views', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ context, name, filtersJson, isShared }),
  });
  if (!resp.ok) throw new Error('Failed to create saved view');
  return resp.json();
}

async function deleteSavedView(id: string): Promise<void> {
  const resp = await fetch(`/api/v1/user-saved-views/${id}`, { method: 'DELETE' });
  if (!resp.ok) throw new Error('Failed to delete saved view');
}

export function SavedViewSelector({ context, currentFilters, onApply }: SavedViewSelectorProps) {
  const { t } = useTranslation('savedViews');
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [showSaveModal, setShowSaveModal] = useState(false);
  const [newViewName, setNewViewName] = useState('');
  const [newViewShared, setNewViewShared] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { data } = useQuery({
    queryKey: ['saved-views', context],
    queryFn: () => fetchSavedViews(context),
  });

  const createMutation = useMutation({
    mutationFn: () => createSavedView(context, newViewName, JSON.stringify(currentFilters), newViewShared),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['saved-views', context] });
      setShowSaveModal(false);
      setNewViewName('');
      setNewViewShared(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteSavedView(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saved-views', context] }),
  });

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    if (open) document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  const ownViews = data?.items?.filter(v => v.isOwn) ?? [];
  const sharedViews = data?.items?.filter(v => !v.isOwn && v.isShared) ?? [];

  const handleApply = (view: SavedViewItem) => {
    try {
      const filters = JSON.parse(view.filtersJson ?? '{}') as Record<string, unknown>;
      onApply(filters);
    } catch {
      onApply({});
    }
    setOpen(false);
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="flex items-center gap-1 px-3 py-1.5 text-sm border rounded hover:bg-surface-2 transition-colors"
      >
        <Bookmark size={14} />
        <span>{t('title')}</span>
        <ChevronDown size={12} />
      </button>

      {open && (
        <div className="absolute right-0 mt-1 w-64 bg-surface border rounded shadow-lg z-50 py-1">
          <button
            type="button"
            className="w-full text-left px-3 py-2 text-sm hover:bg-surface-2"
            onClick={() => { onApply({}); setOpen(false); }}
          >
            {t('default')}
          </button>

          {ownViews.map(view => (
            <div key={view.id} className="flex items-center justify-between px-3 py-1 hover:bg-surface-2 group">
              <button
                type="button"
                className="flex-1 text-left text-sm truncate"
                onClick={() => handleApply(view)}
              >
                {view.name}
              </button>
              <button
                type="button"
                onClick={() => deleteMutation.mutate(view.id)}
                className="opacity-0 group-hover:opacity-100 text-critical hover:text-critical/80 ml-2"
                title={t('delete')}
              >
                <Trash2 size={12} />
              </button>
            </div>
          ))}

          {sharedViews.length > 0 && (
            <>
              <div className="px-3 py-1 text-xs text-muted border-t mt-1 pt-2">
                {t('shared')}
              </div>
              {sharedViews.map(view => (
                <button
                  key={view.id}
                  type="button"
                  className="w-full text-left px-3 py-1 text-sm hover:bg-surface-2"
                  onClick={() => handleApply(view)}
                >
                  {view.name}
                </button>
              ))}
            </>
          )}

          {ownViews.length === 0 && sharedViews.length === 0 && (
            <p className="px-3 py-2 text-xs text-muted">{t('empty')}</p>
          )}

          <div className="border-t mt-1">
            <button
              type="button"
              className="w-full text-left px-3 py-2 text-sm text-primary hover:bg-surface-2"
              onClick={() => { setShowSaveModal(true); setOpen(false); }}
            >
              {t('save')}
            </button>
          </div>
        </div>
      )}

      {showSaveModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-surface rounded-lg shadow-xl p-6 w-80">
            <h3 className="font-semibold mb-4">{t('saveModal.title')}</h3>
            <div className="mb-3">
              <label className="block text-sm font-medium mb-1">{t('saveModal.name')}</label>
              <input
                type="text"
                value={newViewName}
                onChange={e => setNewViewName(e.target.value)}
                placeholder={t('saveModal.namePlaceholder')}
                className="w-full px-3 py-1.5 text-sm border rounded"
                autoFocus
              />
            </div>
            <label className="flex items-center gap-2 mb-4 text-sm">
              <input
                type="checkbox"
                checked={newViewShared}
                onChange={e => setNewViewShared(e.target.checked)}
                className="rounded"
              />
              {t('saveModal.share')}
            </label>
            <div className="flex gap-2 justify-end">
              <button
                type="button"
                onClick={() => setShowSaveModal(false)}
                className="px-3 py-1.5 text-sm border rounded hover:bg-surface-2"
              >
                {t('saveModal.cancel')}
              </button>
              <button
                type="button"
                onClick={() => createMutation.mutate()}
                disabled={!newViewName.trim() || createMutation.isPending}
                className="px-3 py-1.5 text-sm bg-primary text-white rounded disabled:opacity-50"
              >
                {t('saveModal.confirm')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SavedViewSelector;
