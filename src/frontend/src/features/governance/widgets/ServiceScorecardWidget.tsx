/**
 * ServiceScorecardWidget — scorecard de maturidade de serviço.
 * Dados via GET /executive/service-scorecards.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Star } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface ScorecardsResponse {
  items: { serviceName: string; finalScore: number; maturityLevel: string }[];
  averageScore: number;
}

export function ServiceScorecardWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.serviceScorecard');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-service-scorecards', config.teamId, timeRange],
    queryFn: () =>
      client
        .get<ScorecardsResponse>('/executive/service-scorecards', {
          params: {
            teamName: config.teamId ?? undefined,
            page: 1,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <Star size={14} className="text-yellow-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-medium text-gray-500 dark:text-gray-400">
          {t('governance.dashboardView.avgScore', 'Avg')}: {data.averageScore?.toFixed(0) ?? '—'}
        </span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.slice(0, 5).map((s) => (
          <div key={s.serviceName} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <span className="text-xs text-gray-700 dark:text-gray-300 truncate">{s.serviceName}</span>
            <span className="text-xs font-semibold tabular-nums text-gray-900 dark:text-white">
              {s.finalScore} <span className="font-normal text-gray-400">/ 100</span>
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
