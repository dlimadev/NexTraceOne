import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Database,
  Clock,
  Users,
  HardDrive,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Activity,
} from 'lucide-react';
import { platformAdminApi } from '../api/platformAdmin';

export function DatabaseHealthPage() {
  const { t } = useTranslation('databaseHealth');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['database-health'],
    queryFn: platformAdminApi.getDatabaseHealth,
    refetchInterval: 60_000,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          {t('refresh')}
        </button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {data && !data.available && (
        <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
          <XCircle size={20} className="text-red-500 mt-0.5" />
          <div>
            <p className="font-medium text-red-800">{t('unavailable')}</p>
            {data.error && <p className="text-sm text-red-600 mt-1">{data.error}</p>}
          </div>
        </div>
      )}

      {data?.available && (
        <>
          {/* Key Metrics */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <MetricCard
              icon={<Database size={18} className="text-indigo-500" />}
              label={t('version')}
              value={data.version ?? '-'}
            />
            <MetricCard
              icon={<Clock size={18} className="text-slate-500" />}
              label={t('uptime')}
              value={formatUptime(data.uptimeMinutes)}
            />
            <MetricCard
              icon={<Users size={18} className="text-purple-500" />}
              label={t('connections')}
              value={`${data.activeConnections} / ${data.maxConnections}`}
              alert={data.activeConnections / data.maxConnections > 0.8}
            />
            <MetricCard
              icon={<HardDrive size={18} className="text-emerald-500" />}
              label={t('totalSize')}
              value={`${data.totalSizeGb.toFixed(2)} GB`}
            />
          </div>

          {/* Schema Sizes */}
          <section>
            <h2 className="text-base font-medium text-slate-800 mb-3">{t('schemasTitle')}</h2>
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colSchema')}</th>
                    <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colSize')}</th>
                    <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colTables')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.schemas.map((s) => (
                    <tr key={s.schema}>
                      <td className="px-4 py-3 font-mono text-xs text-slate-700">{s.schema}</td>
                      <td className="px-4 py-3 text-right text-xs text-slate-600">{s.sizeGb.toFixed(3)} GB</td>
                      <td className="px-4 py-3 text-right text-xs text-slate-500">{s.tableCount}</td>
                    </tr>
                  ))}
                  {data.schemas.length === 0 && (
                    <tr>
                      <td colSpan={3} className="px-4 py-6 text-center text-xs text-slate-400">
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
              <h2 className="text-base font-medium text-slate-800 mb-3 flex items-center gap-2">
                <AlertTriangle size={16} className="text-amber-500" />
                {t('bloatTitle')}
              </h2>
              <div className="bg-white border border-amber-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-amber-50 border-b border-amber-200">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-amber-800">{t('colTable')}</th>
                      <th className="text-right px-4 py-3 font-medium text-amber-800">{t('colBloat')}</th>
                      <th className="text-center px-4 py-3 font-medium text-amber-800">{t('colSeverity')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-amber-100">
                    {data.bloatSignals.map((b) => (
                      <tr key={`${b.schema}.${b.table}`}>
                        <td className="px-4 py-3 font-mono text-xs text-slate-700">
                          {b.schema}.{b.table}
                        </td>
                        <td className="px-4 py-3 text-right text-xs text-slate-600">{b.bloatPct.toFixed(1)}%</td>
                        <td className="px-4 py-3 text-center">
                          <SeverityBadge severity={b.severity} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="mt-2 text-xs text-slate-400">{t('bloatHint')}</p>
            </section>
          )}

          {/* Slow Queries */}
          {data.slowQueries.length > 0 && (
            <section>
              <h2 className="text-base font-medium text-slate-800 mb-3 flex items-center gap-2">
                <Activity size={16} className="text-orange-500" />
                {t('slowQueriesTitle')} ({data.slowQueryCount})
              </h2>
              <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colQuery')}</th>
                      <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colMeanMs')}</th>
                      <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colCalls')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {data.slowQueries.map((q, idx) => (
                      <tr key={idx}>
                        <td className="px-4 py-3 font-mono text-xs text-slate-700 max-w-xs truncate" title={q.queryPreview}>
                          {q.queryPreview}
                        </td>
                        <td className="px-4 py-3 text-right text-xs font-medium text-orange-600">{q.meanMs} ms</td>
                        <td className="px-4 py-3 text-right text-xs text-slate-500">{q.calls.toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="mt-2 text-xs text-slate-400">{t('slowQueriesHint')}</p>
            </section>
          )}

          {/* No issues state */}
          {data.bloatSignals.length === 0 && data.slowQueries.length === 0 && (
            <div className="flex items-center gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-700 text-sm">
              <CheckCircle size={18} />
              {t('healthy')}
            </div>
          )}

          <p className="text-xs text-slate-400">
            {t('checkedAt')}: {new Date(data.checkedAt).toLocaleString()}
          </p>
        </>
      )}
    </div>
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
    <div className={`bg-white border rounded-lg p-4 flex items-start gap-3 ${alert ? 'border-amber-300' : 'border-slate-200'}`}>
      <div className="mt-0.5">{icon}</div>
      <div>
        <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
        <p className={`text-sm font-semibold mt-0.5 ${alert ? 'text-amber-600' : 'text-slate-800'}`}>{value}</p>
      </div>
    </div>
  );
}

function SeverityBadge({ severity }: { severity: string }) {
  const cls = {
    High:   'bg-red-100 text-red-700',
    Medium: 'bg-amber-100 text-amber-700',
    Low:    'bg-slate-100 text-slate-600',
  }[severity] ?? 'bg-slate-100 text-slate-600';
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
