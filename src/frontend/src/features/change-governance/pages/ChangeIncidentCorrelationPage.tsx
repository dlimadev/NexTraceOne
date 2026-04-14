import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitMerge, AlertTriangle, Clock, Search } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface ChangeIncidentCorrelation {
  id: string;
  changeId: string;
  changeSummary: string;
  incidentId: string;
  incidentTitle: string;
  causalProbability: number;
  timeWindow: string;
  deployTime: string;
  incidentTime: string;
  deltaMinutes: number;
  correlationScore: 'high' | 'medium' | 'low';
}

interface CorrelationResponse {
  correlations: ChangeIncidentCorrelation[];
  highCount: number;
  mediumCount: number;
  lowCount: number;
}

const useCorrelations = (timeWindow: string) =>
  useQuery({
    queryKey: ['change-incident-correlations', timeWindow],
    queryFn: () =>
      client
        .get<CorrelationResponse>('/changes/incident-correlations', {
          params: { window: timeWindow },
        })
        .then((r) => r.data),
  });

const SCORE_VARIANT: Record<string, 'danger' | 'warning' | 'neutral'> = {
  high: 'danger',
  medium: 'warning',
  low: 'neutral',
};

export function ChangeIncidentCorrelationPage() {
  const { t } = useTranslation();
  const [timeWindow, setTimeWindow] = useState('24h');
  const { data, isLoading, isError, refetch } = useCorrelations(timeWindow);

  if (isLoading) return <PageLoadingState message={t('changeIncidentCorr.loading')} />;
  if (isError) return <PageErrorState message={t('changeIncidentCorr.error')} onRetry={() => refetch()} />;

  const correlations = data?.correlations ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('changeIncidentCorr.title')}
        subtitle={t('changeIncidentCorr.subtitle')}
        actions={
          <div className="flex gap-2">
            <select
              value={timeWindow}
              onChange={(e) => setTimeWindow(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              {['1h', '6h', '24h', '48h', '7d'].map((w) => (
                <option key={w} value={w}>{w}</option>
              ))}
            </select>
            <Button size="sm" onClick={() => refetch()}>
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={3}>
        <StatCard title={t('changeIncidentCorr.high')} value={data?.highCount ?? 0} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('changeIncidentCorr.medium')} value={data?.mediumCount ?? 0} icon={<AlertTriangle size={20} />} color="text-warning" />
        <StatCard title={t('changeIncidentCorr.low')} value={data?.lowCount ?? 0} icon={<GitMerge size={20} />} color="text-info" />
      </StatsGrid>

      <PageSection title={t('changeIncidentCorr.correlations')}>
        {correlations.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('changeIncidentCorr.noCorrelations')}</p>
        ) : (
          <div className="space-y-3">
            {correlations.map((corr) => (
              <Card key={corr.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between flex-wrap gap-2 mb-2">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <GitMerge size={14} className="text-gray-400" />
                        <span className="text-sm font-medium text-gray-900 dark:text-white">
                          {corr.changeSummary}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 mt-1">
                        <AlertTriangle size={14} className="text-red-400" />
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                          {corr.incidentTitle}
                        </span>
                      </div>
                    </div>
                    <Badge variant={SCORE_VARIANT[corr.correlationScore] ?? 'neutral'}>
                      {Math.round(corr.causalProbability * 100)}%
                    </Badge>
                  </div>
                  <div className="flex items-center gap-4 text-xs text-gray-400 mb-2">
                    <span className="flex items-center gap-1">
                      <Clock size={11} />
                      {t('changeIncidentCorr.deltaMinutes')}: {corr.deltaMinutes}m
                    </span>
                  </div>
                  <Button size="sm" variant="ghost">
                    <Search size={12} className="mr-1" />
                    {t('changeIncidentCorr.investigate')}
                  </Button>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
