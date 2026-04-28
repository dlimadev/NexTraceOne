import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Bell,
  RefreshCw,
  CheckCircle,
  VolumeX,
  AlertTriangle,
  Clock,
  Filter,
} from 'lucide-react';
import { saasApi, type AlertFiringRecordDto, type AlertFiringStatus } from '../api/saasApi';

const STATUS_STYLE: Record<AlertFiringStatus, { bg: string; text: string; icon: React.ReactNode }> = {
  Firing: {
    bg: 'bg-red-100',
    text: 'text-red-700',
    icon: <AlertTriangle size={13} className="text-red-500" />,
  },
  Resolved: {
    bg: 'bg-green-100',
    text: 'text-green-700',
    icon: <CheckCircle size={13} className="text-green-500" />,
  },
  Silenced: {
    bg: 'bg-slate-100',
    text: 'text-slate-600',
    icon: <VolumeX size={13} className="text-slate-400" />,
  },
};

const SEVERITY_COLOR: Record<string, string> = {
  Critical: 'bg-red-600 text-white',
  High: 'bg-orange-500 text-white',
  Medium: 'bg-amber-400 text-slate-800',
  Low: 'bg-blue-400 text-white',
  Info: 'bg-slate-200 text-slate-700',
};

function AlertRow({
  alert,
  onResolve,
}: {
  alert: AlertFiringRecordDto;
  onResolve: (id: string, action: 'resolve' | 'silence') => void;
}) {
  const { t } = useTranslation('saasAlerts');
  const style = STATUS_STYLE[alert.status];

  return (
    <tr className="hover:bg-slate-50 transition-colors">
      <td className="px-4 py-3">
        <div className="text-sm font-medium text-slate-800">{alert.alertRuleName}</div>
        {alert.serviceName && (
          <div className="text-xs text-slate-400 mt-0.5">{alert.serviceName}</div>
        )}
      </td>
      <td className="px-4 py-3">
        <span
          className={`inline-flex items-center gap-1.5 text-xs px-2 py-1 rounded-full font-medium ${style.bg} ${style.text}`}
        >
          {style.icon}
          {alert.status}
        </span>
      </td>
      <td className="px-4 py-3">
        <span
          className={`text-xs px-2 py-0.5 rounded-full font-semibold ${SEVERITY_COLOR[alert.severity] ?? 'bg-slate-100 text-slate-600'}`}
        >
          {alert.severity}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-slate-600 max-w-xs truncate">
        {alert.conditionSummary}
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5 text-xs text-slate-500">
          <Clock size={12} />
          {new Date(alert.firedAt).toLocaleString()}
        </div>
        {alert.resolvedAt && (
          <div className="text-xs text-slate-400 mt-0.5">
            {t('resolvedAt')}: {new Date(alert.resolvedAt).toLocaleString()}
          </div>
        )}
      </td>
      <td className="px-4 py-3">
        {alert.status === 'Firing' && (
          <div className="flex gap-2">
            <button
              onClick={() => onResolve(alert.id, 'resolve')}
              className="text-xs px-2 py-1 rounded bg-green-50 text-green-700 border border-green-200 hover:bg-green-100 transition-colors"
            >
              {t('resolve')}
            </button>
            <button
              onClick={() => onResolve(alert.id, 'silence')}
              className="text-xs px-2 py-1 rounded bg-slate-50 text-slate-600 border border-slate-200 hover:bg-slate-100 transition-colors"
            >
              {t('silence')}
            </button>
          </div>
        )}
      </td>
    </tr>
  );
}

export function AlertsPage() {
  const { t } = useTranslation('saasAlerts');
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<AlertFiringStatus | undefined>(undefined);
  const [days, setDays] = useState(7);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['saas-alerts', statusFilter, days],
    queryFn: () => saasApi.listAlerts({ status: statusFilter, days }),
  });

  const resolveMutation = useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'resolve' | 'silence' }) =>
      saasApi.resolveAlert(id, { action }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saas-alerts'] }),
  });

  const alerts = data?.items ?? [];

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="text-sm text-slate-500 mt-1">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg px-3 py-2 transition-colors"
        >
          <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
          {t('refresh')}
        </button>
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-4 text-sm">
          {t('loadError')}
        </div>
      )}

      {/* Summary counters */}
      {data && (
        <div className="grid grid-cols-3 gap-4">
          <div
            className={`bg-white border rounded-xl p-4 cursor-pointer transition-all ${statusFilter === 'Firing' ? 'border-red-400 shadow-sm' : 'border-slate-200 hover:border-slate-300'}`}
            onClick={() => setStatusFilter(statusFilter === 'Firing' ? undefined : 'Firing')}
          >
            <div className="text-sm text-slate-500">{t('firing')}</div>
            <div className="text-2xl font-bold text-red-600">{data.firingCount}</div>
          </div>
          <div
            className={`bg-white border rounded-xl p-4 cursor-pointer transition-all ${statusFilter === 'Resolved' ? 'border-green-400 shadow-sm' : 'border-slate-200 hover:border-slate-300'}`}
            onClick={() => setStatusFilter(statusFilter === 'Resolved' ? undefined : 'Resolved')}
          >
            <div className="text-sm text-slate-500">{t('resolved')}</div>
            <div className="text-2xl font-bold text-green-600">{data.resolvedCount}</div>
          </div>
          <div
            className={`bg-white border rounded-xl p-4 cursor-pointer transition-all ${statusFilter === 'Silenced' ? 'border-slate-400 shadow-sm' : 'border-slate-200 hover:border-slate-300'}`}
            onClick={() => setStatusFilter(statusFilter === 'Silenced' ? undefined : 'Silenced')}
          >
            <div className="text-sm text-slate-500">{t('silenced')}</div>
            <div className="text-2xl font-bold text-slate-600">{data.silencedCount}</div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="flex items-center gap-3">
        <Filter size={14} className="text-slate-400" />
        <span className="text-sm text-slate-600">{t('periodDays')}:</span>
        {[1, 7, 30, 90].map((d) => (
          <button
            key={d}
            onClick={() => setDays(d)}
            className={`text-sm px-3 py-1 rounded-full border transition-colors ${
              days === d
                ? 'bg-blue-600 text-white border-blue-600'
                : 'border-slate-200 text-slate-600 hover:bg-slate-50'
            }`}
          >
            {d}d
          </button>
        ))}
        {statusFilter && (
          <button
            onClick={() => setStatusFilter(undefined)}
            className="text-xs text-slate-500 underline ml-2"
          >
            {t('clearFilter')}
          </button>
        )}
      </div>

      {/* Table */}
      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-slate-400 text-sm">{t('loading')}</div>
        ) : alerts.length === 0 ? (
          <div className="p-12 text-center">
            <Bell size={40} className="mx-auto text-slate-300 mb-3" />
            <p className="text-slate-500 text-sm">{t('noAlerts')}</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {['rule', 'status', 'severity', 'condition', 'firedAt', 'actions'].map((col) => (
                    <th key={col} className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">
                      {t(`col.${col}`)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {alerts.map((alert) => (
                  <AlertRow
                    key={alert.id}
                    alert={alert}
                    onResolve={(id, action) => resolveMutation.mutate({ id, action })}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
