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
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';

const STATUS_STYLE: Record<AlertFiringStatus, { bg: string; text: string; icon: React.ReactNode }> = {
  Firing: {
    bg: 'bg-critical/10',
    text: 'text-critical',
    icon: <AlertTriangle size={13} className="text-critical" />,
  },
  Resolved: {
    bg: 'bg-success/10',
    text: 'text-success',
    icon: <CheckCircle size={13} className="text-success" />,
  },
  Silenced: {
    bg: 'bg-elevated',
    text: 'text-muted',
    icon: <VolumeX size={13} className="text-faded" />,
  },
};

const SEVERITY_COLOR: Record<string, string> = {
  Critical: 'bg-critical text-on-accent', /* text-on-accent: intentional on filled bg */
  High: 'bg-warning text-on-accent', /* text-on-accent: intentional on filled bg */
  Medium: 'bg-warning/20 text-warning',
  Low: 'bg-accent/60 text-on-accent', /* text-on-accent: intentional on filled bg */
  Info: 'bg-elevated text-muted',
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
    <tr className="hover:bg-elevated transition-colors">
      <td className="px-4 py-3">
        <div className="text-sm font-medium text-heading">{alert.alertRuleName}</div>
        {alert.serviceName && (
          <div className="text-xs text-faded mt-0.5">{alert.serviceName}</div>
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
          className={`text-xs px-2 py-0.5 rounded-full font-semibold ${SEVERITY_COLOR[alert.severity] ?? 'bg-elevated text-muted'}`}
        >
          {alert.severity}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-muted max-w-xs truncate">
        {alert.conditionSummary}
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5 text-xs text-muted">
          <Clock size={12} />
          {new Date(alert.firedAt).toLocaleString()}
        </div>
        {alert.resolvedAt && (
          <div className="text-xs text-faded mt-0.5">
            {t('resolvedAt')}: {new Date(alert.resolvedAt).toLocaleString()}
          </div>
        )}
      </td>
      <td className="px-4 py-3">
        {alert.status === 'Firing' && (
          <div className="flex gap-2">
            <Button
              variant="ghost"
              size="xs"
              onClick={() => onResolve(alert.id, 'resolve')}
              className="bg-success/10 text-success border border-success/20 hover:bg-success/20"
            >
              {t('resolve')}
            </Button>
            <Button
              variant="ghost"
              size="xs"
              onClick={() => onResolve(alert.id, 'silence')}
              className="bg-elevated text-muted border border-edge hover:bg-hover"
            >
              {t('silence')}
            </Button>
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
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Bell size={20} />}
          actions={
            <Button
              variant="ghost"
              onClick={() => refetch()}
              disabled={isFetching}
              className="flex items-center gap-2"
              size="sm"
            >
              <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
              {t('refresh')}
            </Button>
          }
        />

        {isError && (
          <div className="bg-critical/10 border border-critical/20 text-critical rounded-lg p-4 text-sm">
            {t('loadError')}
          </div>
        )}

        {/* Summary counters */}
        {data && (
          <div className="grid grid-cols-3 gap-4">
            <div
              className={`bg-card border rounded-md p-4 cursor-pointer transition-all ${statusFilter === 'Firing' ? 'border-critical/40 shadow-sm' : 'border-edge hover:border-edge-strong'}`}
              onClick={() => setStatusFilter(statusFilter === 'Firing' ? undefined : 'Firing')}
            >
              <div className="text-sm text-muted">{t('firing')}</div>
              <div className="text-2xl font-bold text-critical">{data.firingCount}</div>
            </div>
            <div
              className={`bg-card border rounded-md p-4 cursor-pointer transition-all ${statusFilter === 'Resolved' ? 'border-success/40 shadow-sm' : 'border-edge hover:border-edge-strong'}`}
              onClick={() => setStatusFilter(statusFilter === 'Resolved' ? undefined : 'Resolved')}
            >
              <div className="text-sm text-muted">{t('resolved')}</div>
              <div className="text-2xl font-bold text-success">{data.resolvedCount}</div>
            </div>
            <div
              className={`bg-card border rounded-md p-4 cursor-pointer transition-all ${statusFilter === 'Silenced' ? 'border-edge-strong shadow-sm' : 'border-edge hover:border-edge-strong'}`}
              onClick={() => setStatusFilter(statusFilter === 'Silenced' ? undefined : 'Silenced')}
            >
              <div className="text-sm text-muted">{t('silenced')}</div>
              <div className="text-2xl font-bold text-muted">{data.silencedCount}</div>
            </div>
          </div>
        )}

        {/* Filters */}
        <div className="flex items-center gap-3">
          <Filter size={14} className="text-faded" />
          <span className="text-sm text-muted">{t('periodDays')}:</span>
          {[1, 7, 30, 90].map((d) => (
            <Button
              key={d}
              variant="ghost"
              size="xs"
              onClick={() => setDays(d)}
              className={`rounded-full border ${
                days === d
                  ? 'bg-accent text-on-accent border-accent' /* text-on-accent: intentional on filled bg */
                  : 'border-edge text-muted hover:bg-elevated'
              }`}
            >
              {d}d
            </Button>
          ))}
          {statusFilter && (
            <Button
              variant="ghost"
              size="xs"
              onClick={() => setStatusFilter(undefined)}
              className="text-muted underline ml-2"
            >
              {t('clearFilter')}
            </Button>
          )}
        </div>

        {/* Table */}
        <div className="bg-card border border-edge rounded-md overflow-hidden">
          {isLoading ? (
            <div className="p-8 text-center text-faded text-sm">{t('loading')}</div>
          ) : alerts.length === 0 && !isLoading ? (
            <EmptyState
              icon={<Bell size={24} />}
              title={t('noAlerts', 'No alerts')}
              description={t('noAlertsDescription', 'No alerts found for the selected period and filters.')}
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-elevated border-b border-edge">
                  <tr>
                    {['rule', 'status', 'severity', 'condition', 'firedAt', 'actions'].map((col) => (
                      <th key={col} className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                        {t(`col.${col}`)}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge/50">
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
    </PageContainer>
  );
}
