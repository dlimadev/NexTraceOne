import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye, EyeOff } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';
import { useAuth } from '../contexts/AuthContext';

type NotifyLevel = 'all' | 'critical' | 'none';
type EntityType = 'service' | 'contract' | 'change' | 'incident' | 'runbook';

interface WatchItem {
  watchId: string;
  entityType: string;
  entityId: string;
  notifyLevel: string;
  createdAt: string;
}

interface WatchesResponse {
  items: WatchItem[];
  totalCount: number;
}

interface Props {
  entityType: EntityType;
  entityId: string;
  size?: 'sm' | 'md';
}

function useWatchStatus(entityType: EntityType, entityId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['watches', entityType, entityId],
    queryFn: () =>
      client
        .get<WatchesResponse>('/api/v1/watches', { params: { entityType } })
        .then((r) => {
          const found = r.data.items.find((w) => w.entityId === entityId);
          return found
            ? { isWatching: true, watchId: found.watchId, notifyLevel: found.notifyLevel as NotifyLevel }
            : { isWatching: false, watchId: undefined, notifyLevel: 'all' as NotifyLevel };
        }),
    enabled,
    staleTime: 30_000,
  });
}

/** Botão para seguir/deixar de seguir uma entidade na watch list. */
export function WatchButton({ entityType, entityId, size = 'md' }: Props) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAuth();
  const qc = useQueryClient();
  const [showMenu, setShowMenu] = useState(false);

  const { data } = useWatchStatus(entityType, entityId, isAuthenticated);
  const isWatching = data?.isWatching ?? false;

  const watch = useMutation({
    mutationFn: (notifyLevel: NotifyLevel) =>
      client.post('/api/v1/watches', { entityType, entityId, notifyLevel }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['watches', entityType, entityId] });
    },
  });

  const unwatch = useMutation({
    mutationFn: (watchId: string) => client.delete(`/api/v1/watches/${watchId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['watches', entityType, entityId] });
    },
  });

  const sizeClass = size === 'sm' ? 'text-xs px-2 py-1' : 'text-sm px-3 py-1.5';

  if (!isAuthenticated) return null;

  return (
    <div className="relative inline-block">
      <button
        onClick={() => {
          if (isWatching) {
            if (data?.watchId) unwatch.mutate(data.watchId);
          } else {
            setShowMenu((v) => !v);
          }
        }}
        className={`inline-flex items-center gap-1.5 border rounded-md transition-colors font-medium ${sizeClass} ${
          isWatching
            ? 'border-blue-300 bg-blue-50 text-blue-700 dark:border-blue-600 dark:bg-blue-900/20 dark:text-blue-300'
            : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300 hover:text-blue-600'
        }`}
        aria-label={isWatching ? t('watch.unwatch') : t('watch.watch')}
      >
        {isWatching ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
        {isWatching ? t('watch.watching') : t('watch.watch')}
      </button>

      {showMenu && !isWatching && (
        <div className="absolute top-full left-0 mt-1 z-20 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg py-1 min-w-[180px]">
          {(['all', 'critical', 'none'] as NotifyLevel[]).map((level) => (
            <button
              key={level}
              onClick={() => {
                watch.mutate(level);
                setShowMenu(false);
              }}
              className="w-full text-left text-sm px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300"
            >
              {t(`watch.level.${level}`)}
            </button>
          ))}
          <button
            onClick={() => setShowMenu(false)}
            className="w-full text-left text-xs px-4 py-1.5 text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800"
          >
            {t('common.cancel')}
          </button>
        </div>
      )}
    </div>
  );
}
