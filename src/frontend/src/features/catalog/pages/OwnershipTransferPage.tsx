import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { UserCheck, ArrowRight, Clock, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface OwnershipTransfer {
  id: string;
  serviceName: string;
  fromTeam: string;
  toTeam: string;
  shadowingDays: number;
  status: 'pending' | 'shadowing' | 'approved' | 'rejected';
  requestedAt: string;
}

interface OwnershipTransferResponse {
  transfers: OwnershipTransfer[];
  total: number;
  pending: number;
}

const useOwnershipTransfers = () =>
  useQuery({
    queryKey: ['ownership-transfers'],
    queryFn: () =>
      client
        .get<OwnershipTransferResponse>('/catalog/ownership-transfers')
        .then((r) => r.data),
  });

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'neutral'> = {
  approved: 'success',
  shadowing: 'warning',
  pending: 'neutral',
  rejected: 'danger',
};

export function OwnershipTransferPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const { data, isLoading, isError, refetch } = useOwnershipTransfers();

  if (isLoading) return <PageLoadingState message={t('ownershipTransfer.loading')} />;
  if (isError) return <PageErrorState message={t('ownershipTransfer.error')} onRetry={() => refetch()} />;

  const transfers = (data?.transfers ?? []).filter(
    (tr) =>
      !search ||
      tr.serviceName.toLowerCase().includes(search.toLowerCase()) ||
      tr.fromTeam.toLowerCase().includes(search.toLowerCase()) ||
      tr.toTeam.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('ownershipTransfer.title')}
        subtitle={t('ownershipTransfer.subtitle')}
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
              <UserCheck size={14} className="mr-1" />
              {t('ownershipTransfer.initiateTransfer')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('ownershipTransfer.pendingTransfers')} value={data?.pending ?? 0} icon={<Clock size={20} />} color="text-warning" />
        <StatCard title={t('common.total')} value={data?.total ?? 0} icon={<UserCheck size={20} />} color="text-accent" />
      </StatsGrid>

      <PageSection title={t('ownershipTransfer.pendingTransfers')}>
        {transfers.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('ownershipTransfer.emptyState')}</p>
        ) : (
          <div className="space-y-3">
            {transfers.map((tr) => (
              <Card key={tr.id}>
                <CardBody className="p-4">
                  <div className="flex items-center justify-between flex-wrap gap-3">
                    <div>
                      <p className="font-medium text-gray-900 dark:text-white">{tr.serviceName}</p>
                      <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mt-1">
                        <span>{tr.fromTeam}</span>
                        <ArrowRight size={12} />
                        <span>{tr.toTeam}</span>
                        <Clock size={12} className="ml-2" />
                        <span>{tr.shadowingDays}d {t('ownershipTransfer.shadowingPeriod')}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={STATUS_VARIANT[tr.status] ?? 'neutral'}>
                        {tr.status}
                      </Badge>
                      <Button size="sm" variant="ghost">
                        <CheckCircle size={14} className="mr-1 text-green-500" />
                        {t('ownershipTransfer.approve')}
                      </Button>
                      <Button size="sm" variant="ghost">
                        <XCircle size={14} className="mr-1 text-red-500" />
                        {t('ownershipTransfer.reject')}
                      </Button>
                    </div>
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
