import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { RotateCcw, CheckCircle, AlertTriangle, Server } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface RollbackRelease {
  id: string;
  service: string;
  version: string;
  environment: string;
  previousStableVersion: string;
  estimatedImpact: string;
  rollbackReady: boolean;
  status: 'ready' | 'pending' | 'unavailable';
  steps: number;
}

interface RollbackIntelligenceResponse {
  releases: RollbackRelease[];
  readyCount: number;
  pendingCount: number;
}

const useRollbackIntelligence = (environment: string) =>
  useQuery({
    queryKey: ['rollback-intelligence', environment],
    queryFn: () =>
      client
        .get<RollbackIntelligenceResponse>('/changes/rollback-intelligence', {
          params: { environment: environment || undefined },
        })
        .then((r) => r.data),
  });

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'neutral'> = {
  ready: 'success',
  pending: 'warning',
  unavailable: 'neutral',
};

export function RollbackIntelligencePage() {
  const { t } = useTranslation();
  const [environment, setEnvironment] = useState('production');
  const { data, isLoading, isError, refetch } = useRollbackIntelligence(environment);

  if (isLoading) return <PageLoadingState message={t('rollbackIntelligence.loading')} />;
  if (isError) return <PageErrorState message={t('rollbackIntelligence.error')} onRetry={() => refetch()} />;

  const releases = data?.releases ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('rollbackIntelligence.title')}
        subtitle={t('rollbackIntelligence.subtitle')}
        actions={
          <div className="flex gap-2">
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="production">Production</option>
              <option value="staging">Staging</option>
              <option value="development">Development</option>
            </select>
            <Button size="sm" onClick={() => refetch()}>
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('rollbackIntelligence.rollbackReady')} value={data?.readyCount ?? 0} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('common.pending')} value={data?.pendingCount ?? 0} icon={<RotateCcw size={20} />} color="text-warning" />
      </StatsGrid>

      <PageSection title={t('rollbackIntelligence.releases')}>
        {releases.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('rollbackIntelligence.noReleases')}</p>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {releases.map((rel) => (
              <Card key={rel.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <div className="flex items-center gap-2">
                        <Server size={14} className="text-gray-400" />
                        <p className="font-medium text-sm text-gray-900 dark:text-white">{rel.service}</p>
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                        {t('rollbackIntelligence.version')}: {rel.version} → {rel.previousStableVersion}
                      </p>
                    </div>
                    <Badge variant={STATUS_VARIANT[rel.status] ?? 'neutral'}>{rel.status}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('rollbackIntelligence.estimatedImpact')}: {rel.estimatedImpact}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">
                    {t('rollbackIntelligence.steps')}: {rel.steps}
                  </p>
                  <div className="flex gap-2 items-center">
                    {rel.rollbackReady ? (
                      <CheckCircle size={14} className="text-green-500" />
                    ) : (
                      <AlertTriangle size={14} className="text-amber-500" />
                    )}
                    <Button size="sm" variant="ghost">
                      {t('rollbackIntelligence.generatePlan')}
                    </Button>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
