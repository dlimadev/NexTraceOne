/**
 * StatWidget — exibe um único valor KPI com indicador de tendência e coloração por threshold.
 * Suporta 7 métricas configuráveis via config.metric.
 * Dados via endpoints existentes do NexTraceOne conforme a métrica seleccionada.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import { timeRangeToDays } from './WidgetRegistry';
import type { WidgetProps, StatMetric } from './WidgetRegistry';
import client from '../../../api/client';

// ── Metric extraction helpers ──────────────────────────────────────────────

interface StatResult {
  value: number;
  unit: string;
  trend: 'up' | 'down' | 'stable';
  /** Number in [0,1] — 0=critical, 0.5=warn, 1=good */
  healthScore: number;
}

function useStatData(metric: StatMetric | null | undefined, timeRange: string, serviceId?: string | null, teamId?: string | null) {
  const days = timeRangeToDays(timeRange);
  const activeMetric = metric ?? 'incidents-open';

  return useQuery({
    queryKey: ['widget-stat', activeMetric, serviceId, teamId, timeRange],
    queryFn: async (): Promise<StatResult> => {
      switch (activeMetric) {
        case 'incidents-open': {
          const r = await client.get<{ totalCount: number; critical: number }>('/operations/incidents', {
            params: { status: 'open', periodDays: days, serviceId: serviceId ?? undefined, teamId: teamId ?? undefined, pageSize: 1 },
          });
          const v = r.data.totalCount ?? 0;
          return { value: v, unit: '', trend: 'stable', healthScore: v === 0 ? 1 : v < 3 ? 0.5 : 0 };
        }
        case 'alerts-critical': {
          const r = await client.get<{ critical: number }>('/governance/alerts/summary', {
            params: { periodDays: days, serviceId: serviceId ?? undefined, teamId: teamId ?? undefined },
          });
          const v = r.data.critical ?? 0;
          return { value: v, unit: '', trend: 'stable', healthScore: v === 0 ? 1 : v < 2 ? 0.5 : 0 };
        }
        case 'alerts-total': {
          const r = await client.get<{ total: number }>('/governance/alerts/summary', {
            params: { periodDays: days, serviceId: serviceId ?? undefined, teamId: teamId ?? undefined },
          });
          const v = r.data.total ?? 0;
          return { value: v, unit: '', trend: 'stable', healthScore: v === 0 ? 1 : v < 5 ? 0.5 : 0 };
        }
        case 'dora-deploy-freq': {
          const r = await client.get<{ deploymentFrequency: { value: number; unit: string } }>('/executive/dora-metrics', {
            params: { periodDays: days },
          });
          const v = r.data.deploymentFrequency.value ?? 0;
          return { value: v, unit: r.data.deploymentFrequency.unit, trend: 'up', healthScore: Math.min(v / 5, 1) };
        }
        case 'dora-cfr': {
          const r = await client.get<{ changeFailureRate: { value: number; unit: string } }>('/executive/dora-metrics', {
            params: { periodDays: days },
          });
          const v = r.data.changeFailureRate.value ?? 0;
          return { value: v, unit: r.data.changeFailureRate.unit, trend: v > 5 ? 'up' : 'down', healthScore: v < 5 ? 1 : v < 15 ? 0.5 : 0 };
        }
        case 'dora-mttr': {
          const r = await client.get<{ meanTimeToRestore: { value: number; unit: string } }>('/executive/dora-metrics', {
            params: { periodDays: days },
          });
          const v = r.data.meanTimeToRestore.value ?? 0;
          return { value: v, unit: r.data.meanTimeToRestore.unit, trend: v > 60 ? 'up' : 'down', healthScore: v < 60 ? 1 : v < 120 ? 0.5 : 0 };
        }
        case 'changes-today': {
          const r = await client.get<{ totalCount: number }>('/governance/changes', {
            params: { periodDays: 1, serviceId: serviceId ?? undefined, teamId: teamId ?? undefined, pageSize: 1 },
          });
          const v = r.data.totalCount ?? 0;
          return { value: v, unit: '', trend: 'stable', healthScore: 1 };
        }
        default: {
          return { value: 0, unit: '', trend: 'stable', healthScore: 0.5 };
        }
      }
    },
  });
}

// ── Health colour helpers ──────────────────────────────────────────────────

function healthColour(score: number): string {
  if (score >= 0.8) return 'text-emerald-600 dark:text-emerald-400';
  if (score >= 0.4) return 'text-amber-600 dark:text-amber-400';
  return 'text-red-600 dark:text-red-400';
}

function healthBg(score: number): string {
  if (score >= 0.8) return 'bg-emerald-50 dark:bg-emerald-900/20';
  if (score >= 0.4) return 'bg-amber-50 dark:bg-amber-900/20';
  return 'bg-red-50 dark:bg-red-900/20';
}

// ── Component ──────────────────────────────────────────────────────────────

export function StatWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();

  const metric = (config.metric as StatMetric | null | undefined) ?? 'incidents-open';
  const { data, isLoading, isError } = useStatData(metric, timeRange, config.serviceId, config.teamId);

  const defaultLabel = t(
    `governance.customDashboards.statMetric.${metric.replace(/-/g, '_')}` as string,
    metric,
  );
  const displayTitle = title ?? defaultLabel;

  if (isLoading) {
    return (
      <div className="h-full flex flex-col items-center justify-center gap-2 p-2">
        <Skeleton variant="text" height="h-3" width="w-24" />
        <Skeleton variant="text" height="h-10" width="w-16" />
        <span className="sr-only">{displayTitle}</span>
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="h-full flex flex-col items-center justify-center gap-1 p-2">
        <span className="text-xs text-gray-400 text-center">
          {displayTitle} — {t('governance.dashboardView.widgetError', 'Could not load data')}
        </span>
      </div>
    );
  }

  const TrendIcon =
    data.trend === 'up' ? TrendingUp : data.trend === 'down' ? TrendingDown : Minus;

  return (
    <div
      className={`h-full flex flex-col items-center justify-center gap-1 p-2 rounded ${healthBg(data.healthScore)}`}
    >
      <span className="text-xs font-medium text-gray-500 dark:text-gray-400 truncate max-w-full text-center">
        {displayTitle}
      </span>
      <div className="flex items-end gap-1">
        <span className={`text-3xl font-bold tabular-nums leading-none ${healthColour(data.healthScore)}`}>
          {data.value.toLocaleString()}
        </span>
        {data.unit && (
          <span className="text-xs text-gray-400 mb-1">{data.unit}</span>
        )}
      </div>
      <TrendIcon
        size={14}
        className={
          data.trend === 'stable'
            ? 'text-gray-400'
            : data.healthScore >= 0.8
            ? 'text-emerald-500'
            : 'text-amber-500'
        }
        aria-label={t('governance.stat.trend', 'Trend')}
      />
    </div>
  );
}
