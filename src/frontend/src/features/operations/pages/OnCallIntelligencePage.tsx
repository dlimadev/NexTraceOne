import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Phone, TrendingDown, Clock, AlertTriangle, BarChart2, Calendar } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

interface IncidentDistribution {
  hour: number;
  dayOfWeek: string;
  incidentCount: number;
}

interface OnCallFatigue {
  teamName: string;
  incidentsLastWeek: number;
  incidentsLastMonth: number;
  avgResponseMinutes: number;
  fatigueLevel: string;
}

interface OnCallIntelligenceResponse {
  periodDays: number;
  generatedAt: string;
  totalIncidentsInPeriod: number;
  avgIncidentsPerWeek: number;
  peakHour: number;
  peakDayOfWeek: string;
  fatigueSeverity: string;
  recommendations: string[];
  distribution: IncidentDistribution[];
  teamFatigue: OnCallFatigue[];
}

const useOnCallIntelligence = (periodDays: number, teamId?: string) => {
  const { activeEnvironmentId } = useEnvironment();
  return useQuery({
    queryKey: ['on-call-intelligence', periodDays, teamId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<OnCallIntelligenceResponse>('/incidents/on-call-intelligence', {
          params: { periodDays, teamId: teamId || undefined },
        })
        .then((r) => r.data),
  });
};

const FATIGUE_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Low: 'success',
  Moderate: 'warning',
  High: 'danger',
  Critical: 'danger',
};

export function OnCallIntelligencePage() {
  const { t } = useTranslation();
  const [periodDays, setPeriodDays] = useState(30);
  const [teamId, setTeamId] = useState('');
  const { data, isLoading, isError, refetch } = useOnCallIntelligence(periodDays, teamId);

  if (isLoading) return <PageLoadingState message={t('operations.onCall.loading')} />;
  if (isError) return <PageErrorState message={t('operations.onCall.error')} onRetry={() => refetch()} />;

  const stats = [
    { label: t('operations.onCall.totalIncidents'), value: data?.totalIncidentsInPeriod ?? 0 },
    { label: t('operations.onCall.avgPerWeek'), value: `${data?.avgIncidentsPerWeek ?? 0}` },
    { label: t('operations.onCall.peakHour'), value: `${data?.peakHour ?? '-'}:00` },
    { label: t('operations.onCall.peakDay'), value: data?.peakDayOfWeek ?? '-' },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('operations.onCall.title')}
        subtitle={t('operations.onCall.subtitle')}
        icon={<Phone size={24} />}
        actions={
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={teamId}
              onChange={(e) => setTeamId(e.target.value)}
              placeholder={t('operations.onCall.filterByTeam')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1 w-36"
            />
            <label className="text-sm text-gray-600 dark:text-gray-400">
              {t('operations.onCall.period')}:
            </label>
            <select
              value={periodDays}
              onChange={(e) => setPeriodDays(Number(e.target.value))}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              {[7, 14, 30, 60, 90].map((d) => (
                <option key={d} value={d}>{t('common.daysN', { count: d })}</option>
              ))}
            </select>
            <Button size="sm" onClick={() => refetch()}>
              <BarChart2 size={14} className="mr-1" />
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid stats={stats} />

      {/* Fatigue severity banner */}
      {data?.fatigueSeverity && data.fatigueSeverity !== 'Low' && (
        <Card className="mb-4 border-amber-300 dark:border-amber-600 bg-amber-50 dark:bg-amber-900/20">
          <CardBody className="p-3 flex items-center gap-2">
            <AlertTriangle size={16} className="text-amber-600 dark:text-amber-400" />
            <span className="text-sm text-amber-700 dark:text-amber-300">
              {t('operations.onCall.fatigueAlert', { level: data.fatigueSeverity })}
            </span>
            <Badge variant={FATIGUE_VARIANT[data.fatigueSeverity] ?? 'secondary'} className="ml-auto">
              {data.fatigueSeverity}
            </Badge>
          </CardBody>
        </Card>
      )}

      {/* Recommendations */}
      {data?.recommendations && data.recommendations.length > 0 && (
        <PageSection title={t('operations.onCall.recommendations')}>
          <div className="space-y-2">
            {data.recommendations.map((rec, idx) => (
              <div key={idx} className="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300">
                <TrendingDown size={14} className="mt-0.5 text-indigo-500 flex-shrink-0" />
                {rec}
              </div>
            ))}
          </div>
        </PageSection>
      )}

      {/* Team fatigue */}
      {data?.teamFatigue && data.teamFatigue.length > 0 && (
        <PageSection title={t('operations.onCall.teamFatigue')}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {data.teamFatigue.map((tf) => (
              <Card key={tf.teamName}>
                <CardBody className="p-4">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-sm text-gray-900 dark:text-white">{tf.teamName}</span>
                    <Badge variant={FATIGUE_VARIANT[tf.fatigueLevel] ?? 'secondary'} className="text-xs">
                      {tf.fatigueLevel}
                    </Badge>
                  </div>
                  <div className="space-y-1 text-xs text-gray-500 dark:text-gray-400">
                    <div className="flex items-center gap-1">
                      <Clock size={12} />
                      {t('operations.onCall.avgResponse', { minutes: tf.avgResponseMinutes })}
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar size={12} />
                      {t('operations.onCall.lastWeekCount', { count: tf.incidentsLastWeek })}
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar size={12} />
                      {t('operations.onCall.lastMonthCount', { count: tf.incidentsLastMonth })}
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        </PageSection>
      )}

      {/* Distribution */}
      {data?.distribution && data.distribution.length > 0 && (
        <PageSection title={t('operations.onCall.incidentDistribution')}>
          <Card>
            <CardBody className="p-4">
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                {data.distribution.slice(0, 8).map((slot, idx) => (
                  <div key={idx} className="rounded bg-gray-50 dark:bg-gray-800 p-2 text-center">
                    <p className="text-xs text-gray-500 dark:text-gray-400">{slot.dayOfWeek} {slot.hour}:00</p>
                    <p className="text-lg font-bold text-gray-900 dark:text-white">{slot.incidentCount}</p>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        </PageSection>
      )}
    </PageContainer>
  );
}
