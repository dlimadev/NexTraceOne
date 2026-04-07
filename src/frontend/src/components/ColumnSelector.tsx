import { useState, useRef, useEffect, useCallback } from 'react';
import { Columns } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

interface ColumnOption {
  key: string;
  label: string;
}

interface ColumnSelectorProps {
  context: string;
  availableColumns: ColumnOption[];
}

async function fetchPreferences() {
  const resp = await fetch('/api/v1/user-preferences');
  if (!resp.ok) throw new Error('Failed to fetch');
  return resp.json();
}

async function saveColumnPref(key: string, value: string): Promise<void> {
  const resp = await fetch('/api/v1/user-preferences', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key, value }),
  });
  if (!resp.ok) throw new Error('Failed to save');
}

export function ColumnSelector({ context, availableColumns }: ColumnSelectorProps) {
  const { t } = useTranslation('columnSelector');
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const popoverRef = useRef<HTMLDivElement>(null);
  const prefKey = `table.columns.${context}`;
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { data } = useQuery({ queryKey: ['user-preferences'], queryFn: fetchPreferences, staleTime: 60000 });
  const prefs: Array<{ key: string; value: string }> = data?.preferences ?? [];
  const stored = prefs.find(p => p.key === prefKey)?.value;
  const defaultKeys = availableColumns.map(c => c.key);

  let visibleKeys: string[];
  if (!stored) {
    visibleKeys = defaultKeys;
  } else {
    try {
      const parsed = JSON.parse(stored);
      visibleKeys = Array.isArray(parsed) ? parsed : defaultKeys;
    } catch {
      visibleKeys = defaultKeys;
    }
  }

  const saveMutation = useMutation({
    mutationFn: (keys: string[]) => saveColumnPref(prefKey, JSON.stringify(keys)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['user-preferences'] }),
  });

  const debouncedSave = useCallback((keys: string[]) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => saveMutation.mutate(keys), 500);
  }, [saveMutation]);

  const handleToggle = (key: string) => {
    const next = visibleKeys.includes(key)
      ? visibleKeys.filter(k => k !== key)
      : [...visibleKeys, key];
    debouncedSave(next);
  };

  const handleReset = () => {
    debouncedSave(defaultKeys);
  };

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (popoverRef.current && !popoverRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    if (open) document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  return (
    <div className="relative" ref={popoverRef}>
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="flex items-center gap-1 px-2 py-1 text-sm border rounded hover:bg-surface-2 transition-colors"
        title={t('title')}
      >
        <Columns size={14} />
        <span>{t('columns')}</span>
      </button>
      {open && (
        <div className="absolute right-0 mt-1 w-56 bg-surface border rounded shadow-lg z-50 p-2">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-muted">{t('title')}</span>
            <button
              type="button"
              onClick={handleReset}
              className="text-xs text-primary hover:underline"
            >
              {t('reset')}
            </button>
          </div>
          <div className="space-y-1 max-h-60 overflow-y-auto">
            {availableColumns.map(col => (
              <label key={col.key} className="flex items-center gap-2 cursor-pointer text-sm py-0.5">
                <input
                  type="checkbox"
                  checked={visibleKeys.includes(col.key)}
                  onChange={() => handleToggle(col.key)}
                  className="rounded"
                />
                <span>{col.label}</span>
              </label>
            ))}
          </div>
          {availableColumns.length === 0 && (
            <p className="text-xs text-muted">{t('noColumns')}</p>
          )}
        </div>
      )}
    </div>
  );
}

export default ColumnSelector;
