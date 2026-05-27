import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  GitBranch,
  RefreshCw,
  XCircle,
  TrendingUp,
  TrendingDown,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type CanaryRolloutEntry } from '../api/platformAdmin';

export function CanaryDashboardPage() {
  const { t } = useTranslation('canaryDashboard');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['canary-dashboard'],
    queryFn: platformAdminApi.getCanaryDashboard,
  });

  const rollouts = data?.rollouts ?? [];
  const filtered =
    statusFilter === 'all'
      ? rollouts
      : rollouts.filter((r) => r.status === statusFilter);

  const activeCount = rollouts.filter((r) => r.status === 'Active').length;
  const promotedCount = rollouts.filter((r) => r.status === 'Promoted').length;
  const rolledBackCount = rollouts.filter((r) => r.status === 'RolledBack').length;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<GitBranch size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Summary cards */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <SummaryCard label={t('totalRollouts')} value={rollouts.length} color="text-heading" />
          <SummaryCard label={t('activeRollouts')} value={activeCount} color="text-accent" />
          <SummaryCard label={t('promoted')} value={promotedCount} color="text-success" />
          <SummaryCard label={t('rolledBack')} value={rolledBackCount} color="text-critical" />
        </div>

        {/* Filter */}
        <div className="flex items-center gap-3 flex-wrap">
          <span className="text-sm text-muted">{t('filterByStatus')}</span>
          {['all', 'Active', 'Promoted', 'RolledBack', 'Paused'].map((s) => (
            <button
              key={s}
              onClick={() => setStatusFilter(s)}
              className={`px-3 py-1.5 text-xs rounded-full border font-medium transition-colors ${
                statusFilter === s
                  ? 'bg-accent text-white border-accent'
                  : 'border-edge text-muted hover:bg-elevated'
              }`}
            >
              {s === 'all' ? t('all') : t(`status.${s}`)}
            </button>
          ))}
        </div>

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

        {data && filtered.length === 0 && (
          <div className="flex items-center justify-center h-32 text-faded text-sm">
            {t('noRollouts')}
          </div>
        )}

        {data && filtered.length > 0 && (
          <div className="space-y-3">
            {filtered.map((rollout) => (
              <CanaryRolloutCard key={rollout.id} rollout={rollout} t={t} />
            ))}
          </div>
        )}
      </div>
    </PageContainer>
  );
}

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="bg-card border border-edge rounded-lg p-4">
      <p className="text-xs text-muted">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${color}`}>{value}</p>
    </div>
  );
}

function CanaryRolloutCard({ rollout, t }: { rollout: CanaryRolloutEntry; t: (k: string) => string }) {
  const statusColors: Record<string, string> = {
    Active: 'bg-accent/10 text-accent',
    Promoted: 'bg-success/10 text-success',
    RolledBack: 'bg-critical/10 text-critical',
    Paused: 'bg-warning/10 text-warning',
  };

  return (
    <div className="bg-card border border-edge rounded-lg p-5 space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h3 className="font-semibold text-heading">{rollout.serviceName}</h3>
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                statusColors[rollout.status] ?? 'bg-elevated text-body'
              }`}
            >
              {t(`status.${rollout.status}`)}
            </span>
          </div>
          <p className="text-sm text-muted mt-1">
            {rollout.canaryVersion} ← {rollout.stableVersion}
          </p>
        </div>
        <div className="text-right text-sm">
          <p className="text-heading font-medium">{rollout.trafficPercentage}%</p>
          <p className="text-faded text-xs">{t('canaryTraffic')}</p>
        </div>
      </div>

      {/* Progress bar */}
      <div>
        <div className="flex justify-between text-xs text-faded mb-1">
          <span>{t('stable')}</span>
          <span>{t('canary')}</span>
        </div>
        <div className="w-full bg-elevated rounded-full h-2">
          <div
            className="bg-accent rounded-full h-2 transition-all"
            style={{ width: `${rollout.trafficPercentage}%` }}
          />
        </div>
      </div>

      {/* Metrics */}
      <div className="grid grid-cols-3 gap-4">
        <MetricBadge
          label={t('errorRate')}
          value={`${rollout.canaryErrorRate.toFixed(2)}%`}
          baseline={`${rollout.stableErrorRate.toFixed(2)}%`}
          worse={rollout.canaryErrorRate > rollout.stableErrorRate}
        />
        <MetricBadge
          label={t('p99Latency')}
          value={`${rollout.canaryP99LatencyMs}ms`}
          baseline={`${rollout.stableP99LatencyMs}ms`}
          worse={rollout.canaryP99LatencyMs > rollout.stableP99LatencyMs * 1.1}
        />
        <MetricBadge
          label={t('throughput')}
          value={`${rollout.canaryRps} rps`}
          baseline={`${rollout.stableRps} rps`}
          worse={false}
        />
      </div>

      <div className="flex items-center justify-between text-xs text-faded">
        <span>{t('environment')}: {rollout.environment}</span>
        <span>{t('startedAt')}: {new Date(rollout.startedAt).toLocaleString()}</span>
      </div>
    </div>
  );
}

function MetricBadge({
  label,
  value,
  baseline,
  worse,
}: {
  label: string;
  value: string;
  baseline: string;
  worse: boolean;
}) {
  return (
    <div className="bg-elevated rounded-lg p-3">
      <p className="text-xs text-muted">{label}</p>
      <div className="flex items-center gap-1 mt-1">
        <p className={`text-sm font-semibold ${worse ? 'text-critical' : 'text-heading'}`}>
          {value}
        </p>
        {worse ? (
          <TrendingUp size={12} className="text-critical" />
        ) : (
          <TrendingDown size={12} className="text-success" />
        )}
      </div>
      <p className="text-xs text-faded">vs {baseline}</p>
    </div>
  );
}
