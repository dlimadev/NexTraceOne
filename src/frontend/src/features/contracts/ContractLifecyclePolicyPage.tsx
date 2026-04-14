import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CalendarDays, Bell, Plus, AlertTriangle } from 'lucide-react';
import { Card, CardBody } from '../../components/Card';
import { Badge } from '../../components/Badge';
import { PageLoadingState } from '../../components/PageLoadingState';
import { PageErrorState } from '../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../components/shell';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/Button';
import { StatCard } from '../../components/StatCard';
import client from '../../api/client';

interface ContractPolicy {
  id: string;
  policyName: string;
  contractName: string;
  expiryDate: string;
  daysUntilExpiry: number;
  status: 'active' | 'deprecated' | 'expired';
  autoNotify: boolean;
}

interface ContractLifecycleResponse {
  contracts: ContractPolicy[];
  activeCount: number;
  deprecatedCount: number;
  expiredCount: number;
}

const useContractLifecycle = () =>
  useQuery({
    queryKey: ['contract-lifecycle-policies'],
    queryFn: () =>
      client
        .get<ContractLifecycleResponse>('/contracts/lifecycle-policies')
        .then((r) => r.data),
  });

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'neutral'> = {
  active: 'success',
  deprecated: 'warning',
  expired: 'danger',
};

export function ContractLifecyclePolicyPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const { data, isLoading, isError, refetch } = useContractLifecycle();

  if (isLoading) return <PageLoadingState message={t('contractLifecycle.loading')} />;
  if (isError) return <PageErrorState message={t('contractLifecycle.error')} onRetry={() => refetch()} />;

  const contracts = (data?.contracts ?? []).filter(
    (c) => !search || c.contractName.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('contractLifecycle.title')}
        subtitle={t('contractLifecycle.subtitle')}
        actions={
          <div className="flex gap-2">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('common.search')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1 w-48"
            />
            <Button size="sm">
              <Plus size={14} className="mr-1" />
              {t('contractLifecycle.addPolicy')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={3}>
        <StatCard title={t('contractLifecycle.active')} value={data?.activeCount ?? 0} icon={<CalendarDays size={20} />} color="text-success" />
        <StatCard title={t('contractLifecycle.deprecated')} value={data?.deprecatedCount ?? 0} icon={<AlertTriangle size={20} />} color="text-warning" />
        <StatCard title={t('contractLifecycle.expired')} value={data?.expiredCount ?? 0} icon={<AlertTriangle size={20} />} color="text-critical" />
      </StatsGrid>

      <PageSection title={t('contractLifecycle.contracts')}>
        {contracts.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('contractLifecycle.noContracts')}</p>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {contracts.map((cp) => (
              <Card key={cp.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between mb-2">
                    <p className="font-medium text-sm text-gray-900 dark:text-white">{cp.contractName}</p>
                    <Badge variant={STATUS_VARIANT[cp.status] ?? 'neutral'}>{cp.status}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('contractLifecycle.policyName')}: {cp.policyName}
                  </p>
                  <div className="flex items-center gap-1 text-xs text-gray-400 mb-2">
                    <CalendarDays size={11} />
                    {t('contractLifecycle.expiryDate')}: {cp.expiryDate}
                  </div>
                  {cp.daysUntilExpiry <= 30 && (
                    <div className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400 mb-2">
                      <AlertTriangle size={11} />
                      {t('contractLifecycle.daysUntilExpiry')}: {cp.daysUntilExpiry}
                    </div>
                  )}
                  <div className="flex items-center gap-1 text-xs text-gray-500">
                    <Bell size={11} />
                    {t('contractLifecycle.autoNotify')}: {cp.autoNotify ? '✓' : '✗'}
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
