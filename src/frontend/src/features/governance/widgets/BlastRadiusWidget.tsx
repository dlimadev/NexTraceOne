/**
 * BlastRadiusWidget — blast radius da mudança mais recente activa.
 * Reforça o pilar de Change Intelligence & Production Change Confidence do NexTraceOne.
 * Dados via GET /governance/changes/blast-radius/latest.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Zap } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface BlastRadiusLatestResponse {
  changeId: string;
  changeName: string;
  affectedServices: number;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  confidenceScore: number;
  status: string;
}

// ── Risk style helpers ─────────────────────────────────────────────────────

const RISK_STYLES: Record<string, { bg: string; text: string }> = {
  Low:      { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-700 dark:text-emerald-300' },
  Medium:   { bg: 'bg-amber-100 dark:bg-amber-900/30',    text: 'text-amber-700 dark:text-amber-300' },
  High:     { bg: 'bg-orange-100 dark:bg-orange-900/30',  text: 'text-orange-700 dark:text-orange-300' },
  Critical: { bg: 'bg-red-100 dark:bg-red-900/30',        text: 'text-red-700 dark:text-red-300' },
};

// ── Component ──────────────────────────────────────────────────────────────

export function BlastRadiusWidget({ config, environmentId, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.blastRadius', 'Blast Radius');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-blast-radius', config.serviceId, environmentId],
    queryFn: () =>
      client
        .get<BlastRadiusLatestResponse>('/governance/changes/blast-radius/latest', {
          params: {
            serviceId: config.serviceId ?? undefined,
            environmentId: environmentId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const risk = RISK_STYLES[data.riskLevel] ?? RISK_STYLES.Medium;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      {/* Header */}
      <div className="flex items-center gap-2">
        <Zap size={14} className="text-orange-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
      </div>

      {/* Change name */}
      <p
        className="text-[10px] text-gray-500 dark:text-gray-400 truncate"
        title={data.changeName}
      >
        {data.changeName}
      </p>

      {/* Metrics grid */}
      <div className="grid grid-cols-2 gap-2 flex-1">
        {/* Affected services */}
        <div className="flex flex-col items-center justify-center rounded bg-blue-50 dark:bg-blue-900/20 p-2">
          <span className="text-2xl font-bold tabular-nums text-blue-700 dark:text-blue-300">
            {data.affectedServices}
          </span>
          <span className="text-[10px] text-blue-600 dark:text-blue-400 font-medium text-center">
            {t('governance.customDashboards.blastRadius.affectedServices', 'Affected')}
          </span>
        </div>

        {/* Risk level */}
        <div className={`flex flex-col items-center justify-center rounded p-2 ${risk.bg}`}>
          <span className={`text-sm font-bold ${risk.text}`}>{data.riskLevel}</span>
          <span className={`text-[10px] font-medium ${risk.text}`}>
            {t('governance.customDashboards.blastRadius.risk', 'Risk')}
          </span>
        </div>
      </div>

      {/* Confidence score */}
      <div className="flex items-center justify-between">
        <span className="text-[10px] text-gray-400">
          {t('governance.customDashboards.blastRadius.confidence', 'Confidence')}
        </span>
        <span className="text-[10px] font-semibold tabular-nums text-gray-600 dark:text-gray-300">
          {data.confidenceScore}%
        </span>
      </div>
    </div>
  );
}
