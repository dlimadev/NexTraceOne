import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  GitBranch,
  RefreshCw,
  XCircle,
  CheckCircle,
  AlertTriangle,
  TrendingUp,
  TrendingDown,
  Activity,
} from 'lucide-react';
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <GitBranch size={24} className="text-violet-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <SummaryCard label={t('totalRollouts')} value={rollouts.length} color="text-slate-900" />
        <SummaryCard label={t('activeRollouts')} value={activeCount} color="text-blue-600" />
        <SummaryCard label={t('promoted')} value={promotedCount} color="text-green-600" />
        <SummaryCard label={t('rolledBack')} value={rolledBackCount} color="text-red-600" />
      </div>

      {/* Filter */}
      <div className="flex items-center gap-3">
        <span className="text-sm text-slate-600">{t('filterByStatus')}</span>
        {['all', 'Active', 'Promoted', 'RolledBack', 'Paused'].map((s) => (
          <button
            key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-xs rounded-full border font-medium transition-colors ${
              statusFilter === s
                ? 'bg-violet-600 text-white border-violet-600'
                : 'border-slate-300 text-slate-600 hover:bg-slate-50'
            }`}
          >
            {s === 'all' ? t('all') : t(`status.${s}`)}
          </button>
        ))}
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

      {data && filtered.length === 0 && (
        <div className="flex items-center justify-center h-32 text-slate-400 text-sm">
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
  );
}

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="bg-white border border-slate-200 rounded-lg p-4">
      <p className="text-xs text-slate-500">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${color}`}>{value}</p>
    </div>
  );
}

function CanaryRolloutCard({ rollout, t }: { rollout: CanaryRolloutEntry; t: (k: string) => string }) {
  const statusColors: Record<string, string> = {
    Active: 'bg-blue-50 text-blue-700',
    Promoted: 'bg-green-50 text-green-700',
    RolledBack: 'bg-red-50 text-red-700',
    Paused: 'bg-amber-50 text-amber-700',
  };

  return (
    <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h3 className="font-semibold text-slate-900">{rollout.serviceName}</h3>
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                statusColors[rollout.status] ?? 'bg-slate-100 text-slate-700'
              }`}
            >
              {t(`status.${rollout.status}`)}
            </span>
          </div>
          <p className="text-sm text-slate-500 mt-1">
            {rollout.canaryVersion} ← {rollout.stableVersion}
          </p>
        </div>
        <div className="text-right text-sm">
          <p className="text-slate-900 font-medium">{rollout.trafficPercentage}%</p>
          <p className="text-slate-400 text-xs">{t('canaryTraffic')}</p>
        </div>
      </div>

      {/* Progress bar */}
      <div>
        <div className="flex justify-between text-xs text-slate-400 mb-1">
          <span>{t('stable')}</span>
          <span>{t('canary')}</span>
        </div>
        <div className="w-full bg-slate-100 rounded-full h-2">
          <div
            className="bg-violet-500 rounded-full h-2 transition-all"
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

      <div className="flex items-center justify-between text-xs text-slate-400">
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
    <div className="bg-slate-50 rounded-lg p-3">
      <p className="text-xs text-slate-500">{label}</p>
      <div className="flex items-center gap-1 mt-1">
        <p className={`text-sm font-semibold ${worse ? 'text-red-600' : 'text-slate-900'}`}>
          {value}
        </p>
        {worse ? (
          <TrendingUp size={12} className="text-red-500" />
        ) : (
          <TrendingDown size={12} className="text-green-500" />
        )}
      </div>
      <p className="text-xs text-slate-400">vs {baseline}</p>
    </div>
  );
}
