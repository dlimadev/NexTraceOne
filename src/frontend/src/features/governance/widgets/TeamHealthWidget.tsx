/**
 * TeamHealthWidget — visão de saúde de uma equipa: incidentes P1/P2, on-call, último deploy e score.
 * Reforça o pilar de Operational Reliability e Team Ownership do NexTraceOne.
 * Dados via GET /governance/teams/{teamId}/health.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Users, PhoneCall, Clock } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface TeamHealthResponse {
  teamId: string;
  teamName: string;
  openIncidentsP1: number;
  openIncidentsP2: number;
  onCallEngineer: string | null;
  lastDeployAt: string | null;
  healthScore: number;
  healthStatus: 'healthy' | 'degraded' | 'critical';
}

// ── Helpers ────────────────────────────────────────────────────────────────

const HEALTH_COLORS: Record<string, string> = {
  healthy:  'text-emerald-600 dark:text-emerald-400',
  degraded: 'text-amber-600 dark:text-amber-400',
  critical: 'text-red-600 dark:text-red-400',
};

function formatRelativeTime(iso: string | null): string {
  if (!iso) return '—';
  const diffMs = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diffMs / 60_000);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

// ── Component ──────────────────────────────────────────────────────────────

export function TeamHealthWidget({ config, environmentId, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.teamHealth', 'Team Health');
  const teamId = config.teamId;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-team-health', teamId, environmentId],
    queryFn: () =>
      client
        .get<TeamHealthResponse>(`/governance/teams/${teamId}/health`, {
          params: { environmentId: environmentId ?? undefined },
        })
        .then((r) => r.data),
    enabled: Boolean(teamId),
  });

  if (!teamId) {
    return (
      <div className="h-full flex flex-col gap-1 p-1">
        <div className="flex items-center gap-2">
          <Users size={14} className="text-accent shrink-0" />
          <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
            {displayTitle}
          </span>
        </div>
        <div className="flex-1 flex items-center justify-center text-xs text-gray-400 text-center px-2">
          {t('governance.customDashboards.teamHealth.noTeam', 'Select a team in widget settings')}
        </div>
      </div>
    );
  }

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const healthColorClass = HEALTH_COLORS[data.healthStatus] ?? HEALTH_COLORS.degraded;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      {/* Header */}
      <div className="flex items-center gap-2">
        <Users size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className={`ml-auto text-[10px] font-bold tabular-nums ${healthColorClass}`}>
          {data.healthScore}%
        </span>
      </div>

      {/* Team name */}
      <p className="text-[10px] text-gray-500 dark:text-gray-400 truncate" title={data.teamName}>
        {data.teamName}
      </p>

      {/* P1 / P2 incident counts */}
      <div className="grid grid-cols-2 gap-2 flex-1">
        <div
          className={`flex flex-col items-center justify-center rounded p-2 ${
            data.openIncidentsP1 > 0
              ? 'bg-red-50 dark:bg-red-900/20'
              : 'bg-gray-50 dark:bg-gray-800/50'
          }`}
        >
          <span
            className={`text-2xl font-bold tabular-nums ${
              data.openIncidentsP1 > 0
                ? 'text-red-600 dark:text-red-400'
                : 'text-gray-500 dark:text-gray-400'
            }`}
          >
            {data.openIncidentsP1}
          </span>
          <span className="text-[9px] text-gray-500 font-medium text-center">
            {t('governance.customDashboards.teamHealth.p1Incidents', 'P1 Incidents')}
          </span>
        </div>

        <div
          className={`flex flex-col items-center justify-center rounded p-2 ${
            data.openIncidentsP2 > 0
              ? 'bg-amber-50 dark:bg-amber-900/20'
              : 'bg-gray-50 dark:bg-gray-800/50'
          }`}
        >
          <span
            className={`text-2xl font-bold tabular-nums ${
              data.openIncidentsP2 > 0
                ? 'text-amber-600 dark:text-amber-400'
                : 'text-gray-500 dark:text-gray-400'
            }`}
          >
            {data.openIncidentsP2}
          </span>
          <span className="text-[9px] text-gray-500 font-medium text-center">
            {t('governance.customDashboards.teamHealth.p2Incidents', 'P2 Incidents')}
          </span>
        </div>
      </div>

      {/* Footer: on-call + last deploy */}
      <div className="flex flex-col gap-0.5">
        {data.onCallEngineer && (
          <div className="flex items-center gap-1">
            <PhoneCall size={10} className="text-gray-400 shrink-0" />
            <span className="text-[10px] text-gray-500 dark:text-gray-400 truncate">
              {data.onCallEngineer}
            </span>
          </div>
        )}
        <div className="flex items-center gap-1">
          <Clock size={10} className="text-gray-400 shrink-0" />
          <span className="text-[10px] text-gray-500 dark:text-gray-400">
            {t('governance.customDashboards.teamHealth.lastDeploy', 'Last deploy')}:{' '}
            {formatRelativeTime(data.lastDeployAt)}
          </span>
        </div>
      </div>
    </div>
  );
}
