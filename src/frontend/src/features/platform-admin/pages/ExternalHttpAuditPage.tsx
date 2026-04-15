import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Globe, RefreshCw, ShieldOff, Filter } from 'lucide-react';
import { platformAdminApi, type ExternalHttpAuditParams } from '../api/platformAdmin';

export function ExternalHttpAuditPage() {
  const { t } = useTranslation('externalHttpAudit');
  const [params, setParams] = useState<ExternalHttpAuditParams>({ page: 1, pageSize: 20 });
  const [destinationFilter, setDestinationFilter] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['external-http-audit', params],
    queryFn: () => platformAdminApi.getExternalHttpAudit(params),
  });

  function applyFilter() {
    setParams((p) => ({ ...p, destination: destinationFilter || undefined, page: 1 }));
  }

  function badgeClass(entry: { blocked: boolean; eventType: string }) {
    if (entry.blocked) return 'bg-red-100 text-red-800';
    if (entry.eventType === 'NetworkViolation') return 'bg-orange-100 text-orange-800';
    return 'bg-green-100 text-green-800';
  }

  if (isLoading) return <div className="p-6 text-sm text-gray-500">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-red-500">{t('error')}</div>;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Globe size={24} className="text-blue-600" />
          <div>
            <h1 className="text-xl font-semibold text-gray-900">{t('title')}</h1>
            <p className="text-sm text-gray-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-3 py-1.5 text-sm border rounded-md hover:bg-gray-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Filter bar */}
      <div className="flex items-center gap-3 p-4 bg-gray-50 border rounded-lg">
        <Filter size={16} className="text-gray-400" />
        <input
          type="text"
          value={destinationFilter}
          onChange={(e) => setDestinationFilter(e.target.value)}
          placeholder={t('filterPlaceholder')}
          className="flex-1 text-sm border rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-blue-400"
          onKeyDown={(e) => e.key === 'Enter' && applyFilter()}
        />
        <button
          onClick={applyFilter}
          className="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          {t('filter')}
        </button>
        {params.destination && (
          <button
            onClick={() => { setDestinationFilter(''); setParams((p) => ({ ...p, destination: undefined, page: 1 })); }}
            className="px-3 py-1 text-sm border rounded hover:bg-gray-100"
          >
            {t('clearFilter')}
          </button>
        )}
      </div>

      {/* Stats bar */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-white border rounded-lg p-4">
          <div className="text-2xl font-bold text-gray-900">{data?.total ?? 0}</div>
          <div className="text-sm text-gray-500">{t('totalCalls')}</div>
        </div>
        <div className="bg-white border rounded-lg p-4">
          <div className="text-2xl font-bold text-red-600">
            {data?.entries.filter((e) => e.blocked).length ?? 0}
          </div>
          <div className="text-sm text-gray-500">{t('blockedCalls')}</div>
        </div>
        <div className="bg-white border rounded-lg p-4">
          <div className="text-2xl font-bold text-orange-600">
            {data?.entries.filter((e) => e.eventType === 'NetworkViolation').length ?? 0}
          </div>
          <div className="text-sm text-gray-500">{t('violations')}</div>
        </div>
      </div>

      {/* Audit log table */}
      <div className="bg-white border rounded-lg overflow-hidden">
        <div className="px-4 py-3 border-b bg-gray-50 flex items-center gap-2">
          <ShieldOff size={16} className="text-gray-400" />
          <span className="text-sm font-medium text-gray-700">{t('auditLogTitle')}</span>
        </div>
        {data?.entries.length === 0 ? (
          <div className="p-8 text-center text-sm text-gray-500">{t('noEntries')}</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.timestamp')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.destination')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.method')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.context')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.status')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.duration')}</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">{t('col.result')}</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {data?.entries.map((entry) => (
                  <tr key={entry.id} className="hover:bg-gray-50">
                    <td className="px-4 py-2 text-xs text-gray-500 whitespace-nowrap">
                      {new Date(entry.timestamp).toLocaleString()}
                    </td>
                    <td className="px-4 py-2 font-mono text-xs text-blue-700 max-w-xs truncate">
                      {entry.destination}
                    </td>
                    <td className="px-4 py-2">
                      <span className="px-1.5 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">
                        {entry.method}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-xs text-gray-700">{entry.context}</td>
                    <td className="px-4 py-2 text-xs text-gray-500">
                      {entry.responseStatus ?? '—'}
                    </td>
                    <td className="px-4 py-2 text-xs text-gray-500">
                      {entry.durationMs != null ? `${entry.durationMs} ms` : '—'}
                    </td>
                    <td className="px-4 py-2">
                      <span className={`px-1.5 py-0.5 rounded text-xs font-medium ${badgeClass(entry)}`}>
                        {entry.blocked ? t('blocked') : t('allowed')}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data?.simulatedNote && (
        <p className="text-xs text-gray-400 italic">{data.simulatedNote}</p>
      )}
    </div>
  );
}
