import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Database,
  Clock,
  Users,
  HardDrive,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Activity,
  RefreshCw,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi } from '../api/platformAdmin';

export function DatabaseHealthPage() {
  const { t } = useTranslation('databaseHealth');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['database-health'],
    queryFn: platformAdminApi.getDatabaseHealth,
    refetchInterval: 60_000,
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          actions={
            <Button variant="primary" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && !data.available && (
          <div className="flex items-start gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg">
            <XCircle size={20} className="text-critical mt-0.5" />
            <div>
              <p className="font-medium text-critical">{t('unavailable')}</p>
              {data.error && <p className="text-sm text-critical/80 mt-1">{data.error}</p>}
            </div>
          </div>
        )}

        {data?.available && (
          <>
            {/* Key Metrics */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard
                icon={<Database size={18} className="text-accent" />}
                label={t('version')}
                value={data.version ?? '-'}
              />
              <MetricCard
                icon={<Clock size={18} className="text-muted" />}
                label={t('uptime')}
                value={formatUptime(data.uptimeMinutes)}
              />
              <MetricCard
                icon={<Users size={18} className="text-accent" />}
                label={t('connections')}
                value={`${data.activeConnections} / ${data.maxConnections}`}
                alert={data.activeConnections / data.maxConnections > 0.8}
              />
              <MetricCard
                icon={<HardDrive size={18} className="text-success" />}
                label={t('totalSize')}
                value={`${data.totalSizeGb.toFixed(2)} GB`}
              />
            </div>

            {/* Schema Sizes */}
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('schemasTitle')}</h2>
              <div className="bg-card border border-edge rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-elevated border-b border-edge">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-muted">{t('colSchema')}</th>
                      <th className="text-right px-4 py-3 font-medium text-muted">{t('colSize')}</th>
                      <th className="text-right px-4 py-3 font-medium text-muted">{t('colTables')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge/50">
                    {data.schemas.map((s) => (
                      <tr key={s.schema}>
                        <td className="px-4 py-3 font-mono text-xs text-body">{s.schema}</td>
                        <td className="px-4 py-3 text-right text-xs text-muted">{s.sizeGb.toFixed(3)} GB</td>
                        <td className="px-4 py-3 text-right text-xs text-muted">{s.tableCount}</td>
                      </tr>
                    ))}
                    {data.schemas.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-4 py-6 text-center text-xs text-faded">
                          {t('noSchemas')}
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </section>

            {/* Bloat Signals */}
            {data.bloatSignals.length > 0 && (
              <section>
                <h2 className="text-base font-medium text-heading mb-3 flex items-center gap-2">
                  <AlertTriangle size={16} className="text-warning" />
                  {t('bloatTitle')}
                </h2>
                <div className="bg-card border border-warning/20 rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-warning/10 border-b border-warning/20">
                      <tr>
                        <th className="text-left px-4 py-3 font-medium text-warning">{t('colTable')}</th>
                        <th className="text-right px-4 py-3 font-medium text-warning">{t('colBloat')}</th>
                        <th className="text-center px-4 py-3 font-medium text-warning">{t('colSeverity')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-warning/10">
                      {data.bloatSignals.map((b) => (
                        <tr key={`${b.schema}.${b.table}`}>
                          <td className="px-4 py-3 font-mono text-xs text-body">
                            {b.schema}.{b.table}
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-muted">{b.bloatPct.toFixed(1)}%</td>
                          <td className="px-4 py-3 text-center">
                            <SeverityBadge severity={b.severity} />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <p className="mt-2 text-xs text-faded">{t('bloatHint')}</p>
              </section>
            )}

            {/* Slow Queries */}
            {data.slowQueries.length > 0 && (
              <section>
                <h2 className="text-base font-medium text-heading mb-3 flex items-center gap-2">
                  <Activity size={16} className="text-warning" />
                  {t('slowQueriesTitle')} ({data.slowQueryCount})
                </h2>
                <div className="bg-card border border-edge rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-elevated border-b border-edge">
                      <tr>
                        <th className="text-left px-4 py-3 font-medium text-muted">{t('colQuery')}</th>
                        <th className="text-right px-4 py-3 font-medium text-muted">{t('colMeanMs')}</th>
                        <th className="text-right px-4 py-3 font-medium text-muted">{t('colCalls')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge/50">
                      {data.slowQueries.map((q, idx) => (
                        <tr key={idx}>
                          <td className="px-4 py-3 font-mono text-xs text-body max-w-xs truncate" title={q.queryPreview}>
                            {q.queryPreview}
                          </td>
                          <td className="px-4 py-3 text-right text-xs font-medium text-warning">{q.meanMs} ms</td>
                          <td className="px-4 py-3 text-right text-xs text-muted">{q.calls.toLocaleString()}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <p className="mt-2 text-xs text-faded">{t('slowQueriesHint')}</p>
              </section>
            )}

            {/* No issues state */}
            {data.bloatSignals.length === 0 && data.slowQueries.length === 0 && (
              <div className="flex items-center gap-3 p-4 bg-success/10 border border-success/20 rounded-lg text-success text-sm">
                <CheckCircle2 size={18} />
                {t('healthy')}
              </div>
            )}

            <p className="text-xs text-faded">
              {t('checkedAt')}: {new Date(data.checkedAt).toLocaleString()}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function MetricCard({
  icon,
  label,
  value,
  alert = false,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  alert?: boolean;
}) {
  return (
    <div className={`bg-card border rounded-lg p-4 flex items-start gap-3 ${alert ? 'border-warning/40' : 'border-edge'}`}>
      <div className="mt-0.5">{icon}</div>
      <div>
        <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
        <p className={`text-sm font-semibold mt-0.5 ${alert ? 'text-warning' : 'text-heading'}`}>{value}</p>
      </div>
    </div>
  );
}

function SeverityBadge({ severity }: { severity: string }) {
  const cls = {
    High:   'bg-critical/10 text-critical',
    Medium: 'bg-warning/10 text-warning',
    Low:    'bg-elevated text-muted',
  }[severity] ?? 'bg-elevated text-muted';
  return (
    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>{severity}</span>
  );
}

function formatUptime(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h < 24) return `${h}h ${m}m`;
  const d = Math.floor(h / 24);
  const rh = h % 24;
  return `${d}d ${rh}h`;
}
