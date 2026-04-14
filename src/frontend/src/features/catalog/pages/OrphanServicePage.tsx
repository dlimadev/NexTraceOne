import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, Wrench, UserPlus, Clock } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface OrphanService {
  id: string;
  name: string;
  reason: string;
  lastActivity: string;
  severity: 'critical' | 'warning' | 'info';
}

interface OrphanServiceResponse {
  services: OrphanService[];
  totalOrphans: number;
  criticalOrphans: number;
}

const useOrphanServices = () =>
  useQuery({
    queryKey: ['orphan-services'],
    queryFn: () =>
      client
        .get<OrphanServiceResponse>('/catalog/orphan-services')
        .then((r) => r.data),
  });

const SEV_VARIANT: Record<string, 'danger' | 'warning' | 'neutral'> = {
  critical: 'danger',
  warning: 'warning',
  info: 'neutral',
};

export function OrphanServicePage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<string>('');
  const { data, isLoading, isError, refetch } = useOrphanServices();

  if (isLoading) return <PageLoadingState message={t('orphanService.loading')} />;
  if (isError) return <PageErrorState message={t('orphanService.error')} onRetry={() => refetch()} />;

  const services = (data?.services ?? []).filter(
    (s) => !filter || s.severity === filter,
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('orphanService.title')}
        subtitle={t('orphanService.subtitle')}
        actions={
          <div className="flex gap-2">
            <select
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="">{t('common.all')}</option>
              <option value="critical">{t('common.critical')}</option>
              <option value="warning">{t('common.warning')}</option>
            </select>
            <Button size="sm" onClick={() => refetch()}>
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('orphanService.totalOrphans')} value={data?.totalOrphans ?? 0} icon={<AlertTriangle size={20} />} color="text-warning" />
        <StatCard title={t('orphanService.criticalOrphans')} value={data?.criticalOrphans ?? 0} icon={<AlertTriangle size={20} />} color="text-critical" />
      </StatsGrid>

      <PageSection title={t('orphanService.orphanedServices')}>
        {services.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('orphanService.noOrphans')}</p>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {services.map((svc) => (
              <Card key={svc.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between mb-2">
                    <p className="font-medium text-gray-900 dark:text-white">{svc.name}</p>
                    <Badge variant={SEV_VARIANT[svc.severity] ?? 'neutral'}>{svc.severity}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('orphanService.reason')}: {svc.reason}
                  </p>
                  <div className="flex items-center gap-1 text-xs text-gray-400 mb-3">
                    <Clock size={11} />
                    {t('orphanService.lastActivity')}: {svc.lastActivity}
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="ghost" className="flex-1">
                      <UserPlus size={12} className="mr-1" />
                      {t('orphanService.assignOwner')}
                    </Button>
                    <Button size="sm" variant="ghost">
                      <Wrench size={12} className="mr-1" />
                      {t('orphanService.remediate')}
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
