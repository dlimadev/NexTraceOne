import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Globe, RefreshCw, ShieldOff, Filter } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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
    if (entry.blocked) return 'bg-critical/10 text-critical';
    if (entry.eventType === 'NetworkViolation') return 'bg-warning/10 text-warning';
    return 'bg-success/10 text-success';
  }

  if (isLoading) return <div className="p-6 text-sm text-muted">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-critical">{t('error')}</div>;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Globe size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Filter bar */}
        <div className="flex items-center gap-3 p-4 bg-elevated border border-edge rounded-lg">
          <Filter size={16} className="text-faded" />
          <input
            type="text"
            value={destinationFilter}
            onChange={(e) => setDestinationFilter(e.target.value)}
            placeholder={t('filterPlaceholder')}
            className="flex-1 text-sm border border-edge rounded px-2 py-1 bg-canvas text-body focus:outline-none focus:ring-1 focus:ring-accent/50"
            onKeyDown={(e) => e.key === 'Enter' && applyFilter()}
          />
          <button
            onClick={applyFilter}
            className="px-3 py-1 text-sm bg-accent text-white rounded hover:bg-accent/90"
          >
            {t('filter')}
          </button>
          {params.destination && (
            <button
              onClick={() => { setDestinationFilter(''); setParams((p) => ({ ...p, destination: undefined, page: 1 })); }}
              className="px-3 py-1 text-sm border border-edge rounded hover:bg-elevated text-muted"
            >
              {t('clearFilter')}
            </button>
          )}
        </div>

        {/* Stats bar */}
        <div className="grid grid-cols-3 gap-4">
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="text-2xl font-bold text-heading">{data?.total ?? 0}</div>
            <div className="text-sm text-muted">{t('totalCalls')}</div>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="text-2xl font-bold text-critical">
              {data?.entries.filter((e) => e.blocked).length ?? 0}
            </div>
            <div className="text-sm text-muted">{t('blockedCalls')}</div>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="text-2xl font-bold text-warning">
              {data?.entries.filter((e) => e.eventType === 'NetworkViolation').length ?? 0}
            </div>
            <div className="text-sm text-muted">{t('violations')}</div>
          </div>
        </div>

        {/* Audit log table */}
        <div className="bg-card border border-edge rounded-lg overflow-hidden">
          <div className="px-4 py-3 border-b border-edge bg-elevated flex items-center gap-2">
            <ShieldOff size={16} className="text-faded" />
            <span className="text-sm font-medium text-body">{t('auditLogTitle')}</span>
          </div>
          {data?.entries.length === 0 ? (
            <div className="p-8 text-center text-sm text-muted">{t('noEntries')}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-elevated border-b border-edge">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.timestamp')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.destination')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.method')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.context')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.status')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.duration')}</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-muted">{t('col.result')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge/50">
                  {data?.entries.map((entry) => (
                    <tr key={entry.id} className="hover:bg-elevated">
                      <td className="px-4 py-2 text-xs text-muted whitespace-nowrap">
                        {new Date(entry.timestamp).toLocaleString()}
                      </td>
                      <td className="px-4 py-2 font-mono text-xs text-accent max-w-xs truncate">
                        {entry.destination}
                      </td>
                      <td className="px-4 py-2">
                        <span className="px-1.5 py-0.5 rounded text-xs font-medium bg-elevated text-body">
                          {entry.method}
                        </span>
                      </td>
                      <td className="px-4 py-2 text-xs text-body">{entry.context}</td>
                      <td className="px-4 py-2 text-xs text-muted">
                        {entry.responseStatus ?? '—'}
                      </td>
                      <td className="px-4 py-2 text-xs text-muted">
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
          <p className="text-xs text-faded italic">{data.simulatedNote}</p>
        )}
      </div>
    </PageContainer>
  );
}
