/**
 * SloGaugeWidget — gauge circular de conformidade de SLO.
 * Dados via GET /governance/slo/summary.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Target } from 'lucide-react';
import { timeRangeToDays } from './WidgetRegistry';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

interface SloSummaryResponse {
  compliancePercent: number;
  target: number;
  status: 'met' | 'at-risk' | 'breached';
  sloName: string;
}

/** Draws a simple SVG arc gauge */
function ArcGauge({ value, target }: { value: number; target: number }) {
  const radius = 38;
  const cx = 50;
  const cy = 54;
  const startAngle = -180; // degrees
  const endAngle = 0;
  const range = endAngle - startAngle;
  const filledDeg = startAngle + (range * Math.min(value, 100)) / 100;

  const toRad = (d: number) => (d * Math.PI) / 180;

  const arcPath = (from: number, to: number, r: number) => {
    const sx = cx + r * Math.cos(toRad(from));
    const sy = cy + r * Math.sin(toRad(from));
    const ex = cx + r * Math.cos(toRad(to));
    const ey = cy + r * Math.sin(toRad(to));
    const large = Math.abs(to - from) > 180 ? 1 : 0;
    return `M ${sx} ${sy} A ${r} ${r} 0 ${large} 1 ${ex} ${ey}`;
  };

  const targetDeg = startAngle + (range * Math.min(target, 100)) / 100;
  const targetX = cx + (radius + 4) * Math.cos(toRad(targetDeg));
  const targetY = cy + (radius + 4) * Math.sin(toRad(targetDeg));

  const color = value >= target ? '#22c55e' : value >= target - 5 ? '#f59e0b' : '#ef4444';

  return (
    <svg viewBox="0 0 100 60" className="w-full max-w-[120px]" aria-hidden="true">
      {/* Background arc */}
      <path
        d={arcPath(startAngle, endAngle, radius)}
        fill="none"
        stroke="currentColor"
        strokeWidth="8"
        className="text-gray-200 dark:text-gray-700"
        strokeLinecap="round"
      />
      {/* Filled arc */}
      <path
        d={arcPath(startAngle, filledDeg, radius)}
        fill="none"
        stroke={color}
        strokeWidth="8"
        strokeLinecap="round"
      />
      {/* Target tick */}
      <circle cx={targetX} cy={targetY} r={2} fill="#94a3b8" />
    </svg>
  );
}

export function SloGaugeWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-slo-gauge', config.serviceId, config.teamId, timeRange],
    queryFn: () =>
      client
        .get<SloSummaryResponse>('/governance/slo/summary', {
          params: {
            periodDays: timeRangeToDays(timeRange),
            serviceId: config.serviceId ?? undefined,
            teamId: config.teamId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  const displayTitle = title ?? t('governance.customDashboards.widgets.sloGauge', 'SLO Gauge');

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const statusColor =
    data.status === 'met'
      ? 'text-green-600 dark:text-green-400'
      : data.status === 'at-risk'
      ? 'text-yellow-600 dark:text-yellow-400'
      : 'text-red-600 dark:text-red-400';

  const statusLabel =
    data.status === 'met'
      ? t('governance.sloStatus.met', 'Met')
      : data.status === 'at-risk'
      ? t('governance.sloStatus.atRisk', 'At Risk')
      : t('governance.sloStatus.breached', 'Breached');

  return (
    <div className="h-full flex flex-col items-center justify-center gap-1 p-1">
      <div className="flex items-center gap-2 w-full">
        <Target size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>
      <ArcGauge value={data.compliancePercent} target={data.target} />
      <div className="flex flex-col items-center -mt-1">
        <span className="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
          {data.compliancePercent.toFixed(1)}%
        </span>
        <span className={`text-xs font-medium ${statusColor}`}>{statusLabel}</span>
        <span className="text-[10px] text-gray-400 truncate max-w-full">{data.sloName}</span>
      </div>
      <span className="text-[10px] text-gray-400">
        {t('governance.sloStatus.target', 'Target')}: {data.target}%
      </span>
    </div>
  );
}
