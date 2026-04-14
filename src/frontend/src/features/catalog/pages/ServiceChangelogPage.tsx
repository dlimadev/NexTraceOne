import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Clock, Download, Zap, FileText, AlertTriangle, Users } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface ChangelogEntry {
  id: string;
  serviceId: string;
  serviceName: string;
  type: 'deploy' | 'contractChange' | 'incident' | 'ownerChange';
  description: string;
  timestamp: string;
}

interface ServiceChangelogResponse {
  entries: ChangelogEntry[];
  total: number;
}

const useServiceChangelog = (serviceFilter: string, typeFilter: string) =>
  useQuery({
    queryKey: ['service-changelog', serviceFilter, typeFilter],
    queryFn: () =>
      client
        .get<ServiceChangelogResponse>('/catalog/changelog', {
          params: {
            service: serviceFilter || undefined,
            type: typeFilter || undefined,
          },
        })
        .then((r) => r.data),
  });

const TYPE_ICONS: Record<string, React.ReactNode> = {
  deploy: <Zap size={14} />,
  contractChange: <FileText size={14} />,
  incident: <AlertTriangle size={14} />,
  ownerChange: <Users size={14} />,
};

const TYPE_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'neutral'> = {
  deploy: 'success',
  contractChange: 'neutral',
  incident: 'danger',
  ownerChange: 'warning',
};

export function ServiceChangelogPage() {
  const { t } = useTranslation();
  const [serviceFilter, setServiceFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const { data, isLoading, isError, refetch } = useServiceChangelog(serviceFilter, typeFilter);

  if (isLoading) return <PageLoadingState message={t('serviceChangelog.loading')} />;
  if (isError) return <PageErrorState message={t('serviceChangelog.error')} onRetry={() => refetch()} />;

  const entries = data?.entries ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('serviceChangelog.title')}
        subtitle={t('serviceChangelog.subtitle')}
        actions={
          <div className="flex gap-2 flex-wrap">
            <input
              type="text"
              value={serviceFilter}
              onChange={(e) => setServiceFilter(e.target.value)}
              placeholder={t('serviceChangelog.filterService')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1 w-40"
            />
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="">{t('serviceChangelog.allTypes')}</option>
              <option value="deploy">{t('serviceChangelog.deploy')}</option>
              <option value="contractChange">{t('serviceChangelog.contractChange')}</option>
              <option value="incident">{t('serviceChangelog.incident')}</option>
              <option value="ownerChange">{t('serviceChangelog.ownerChange')}</option>
            </select>
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('serviceChangelog.exportChangelog')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('common.total')} value={data?.total ?? 0} icon={<Clock size={20} />} color="text-accent" />
        <StatCard title={t('serviceChangelog.deploy')} value={entries.filter(e => e.type === 'deploy').length} icon={<Zap size={20} />} color="text-success" />
      </StatsGrid>

      <PageSection title={t('serviceChangelog.title')}>
        {entries.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('serviceChangelog.noEntries')}</p>
        ) : (
          <div className="space-y-2">
            {entries.map((entry) => (
              <Card key={entry.id}>
                <CardBody className="p-3 flex items-start gap-3">
                  <div className="text-gray-500 dark:text-gray-400 mt-0.5">
                    {TYPE_ICONS[entry.type]}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium text-sm text-gray-900 dark:text-white">
                        {entry.serviceName}
                      </span>
                      <Badge variant={TYPE_VARIANT[entry.type] ?? 'neutral'} className="text-xs">
                        {entry.type}
                      </Badge>
                    </div>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{entry.description}</p>
                    <p className="text-xs text-gray-400 mt-0.5">
                      {t('serviceChangelog.generatedFrom')}: {entry.timestamp}
                    </p>
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
