import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X, Plus, Tag } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import client from '../api/client';

interface TagSummary {
  tagId: string;
  key: string;
  value: string;
}

interface Props {
  entityType: string;
  entityId: string;
  readOnly?: boolean;
}

function useTags(tenantId: string, entityType: string, entityId: string) {
  return useQuery({
    queryKey: ['tags', tenantId, entityType, entityId],
    queryFn: () =>
      client
        .get<{ items: TagSummary[] }>('/api/v1/tags', { params: { tenantId, entityType, entityId } })
        .then((r) => r.data.items),
    staleTime: 30_000,
    enabled: !!tenantId && !!entityType && !!entityId,
  });
}

const TAG_KEY_COLORS: Record<string, string> = {
  'team': 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
  'cost-center': 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
  'business-unit': 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300',
  'squad': 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300',
  'tier': 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
};

function getTagColor(key: string) {
  return TAG_KEY_COLORS[key.toLowerCase()] ??
    'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
}

/** Componente reutilizável para editar tags de uma entidade. */
export function TagEditor({ entityType, entityId, readOnly = false }: Props) {
  const { t } = useTranslation();
  const { user, tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const createdBy = user?.email ?? user?.id ?? '';
  const qc = useQueryClient();

  const { data: tags = [] } = useTags(tenantId, entityType, entityId);

  const [inputValue, setInputValue] = useState('');
  const [showInput, setShowInput] = useState(false);

  const addTag = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) =>
      client.post('/api/v1/tags', { tenantId, entityType, entityId, key, value, createdBy }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tags', tenantId, entityType, entityId] });
      setInputValue('');
      setShowInput(false);
    },
  });

  const removeTag = useMutation({
    mutationFn: (tagId: string) =>
      client.delete(`/api/v1/tags/${tagId}`, { params: { tenantId } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tags', tenantId, entityType, entityId] }),
  });

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && inputValue.trim()) {
      const raw = inputValue.trim();
      const colonIdx = raw.indexOf(':');
      const key = colonIdx > 0 ? raw.slice(0, colonIdx).trim() : 'tag';
      const value = colonIdx > 0 ? raw.slice(colonIdx + 1).trim() : raw;
      if (key && value) addTag.mutate({ key, value });
    }
    if (e.key === 'Escape') { setShowInput(false); setInputValue(''); }
  };

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <Tag className="w-3.5 h-3.5 text-gray-400 shrink-0" />
      {tags.map((tag) => (
        <span
          key={tag.tagId}
          className={`inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full font-medium ${getTagColor(tag.key)}`}
        >
          <span className="opacity-70">{tag.key}:</span>{tag.value}
          {!readOnly && (
            <button
              onClick={() => removeTag.mutate(tag.tagId)}
              className="ml-0.5 opacity-60 hover:opacity-100"
              aria-label={t('tags.remove')}
            >
              <X className="w-3 h-3" />
            </button>
          )}
        </span>
      ))}
      {!readOnly && (
        showInput ? (
          <input
            autoFocus
            type="text"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onKeyDown={handleKeyDown}
            onBlur={() => { if (!inputValue) { setShowInput(false); } }}
            placeholder={t('tags.placeholder')}
            className="text-xs border border-gray-200 dark:border-gray-600 rounded-full px-2 py-0.5 bg-transparent text-gray-700 dark:text-gray-300 w-36 focus:outline-none focus:ring-1 focus:ring-blue-400"
          />
        ) : (
          <button
            onClick={() => setShowInput(true)}
            className="inline-flex items-center gap-0.5 text-xs text-gray-400 hover:text-blue-500 transition-colors"
            aria-label={t('tags.add')}
          >
            <Plus className="w-3 h-3" />
            {t('tags.add')}
          </button>
        )
      )}
    </div>
  );
}
