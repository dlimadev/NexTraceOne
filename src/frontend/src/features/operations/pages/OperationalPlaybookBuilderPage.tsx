import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ClipboardList, Plus, Edit2, Clock, Users } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface Playbook {
  id: string;
  playbookName: string;
  trigger: string;
  stepCount: number;
  sla: string;
  owner: string;
  status: 'active' | 'draft';
  linkedServices: number;
}

interface PlaybookResponse {
  playbooks: Playbook[];
  activeCount: number;
  draftCount: number;
}

const usePlaybooks = () =>
  useQuery({
    queryKey: ['operational-playbooks'],
    queryFn: () =>
      client
        .get<PlaybookResponse>('/operations/playbooks')
        .then((r) => r.data),
  });

export function OperationalPlaybookBuilderPage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<string>('');
  const { data, isLoading, isError, refetch } = usePlaybooks();

  if (isLoading) return <PageLoadingState message={t('operationalPlaybook.loading')} />;
  if (isError) return <PageErrorState message={t('operationalPlaybook.error')} onRetry={() => refetch()} />;

  const playbooks = (data?.playbooks ?? []).filter(
    (p) => !filter || p.status === filter,
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('operationalPlaybook.title')}
        subtitle={t('operationalPlaybook.subtitle')}
        actions={
          <div className="flex gap-2">
            <select
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              <option value="">{t('common.all')}</option>
              <option value="active">{t('operationalPlaybook.active')}</option>
              <option value="draft">{t('operationalPlaybook.draft')}</option>
            </select>
            <Button size="sm">
              <Plus size={14} className="mr-1" />
              {t('operationalPlaybook.createPlaybook')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('operationalPlaybook.active')} value={data?.activeCount ?? 0} icon={<ClipboardList size={20} />} color="text-success" />
        <StatCard title={t('operationalPlaybook.draft')} value={data?.draftCount ?? 0} icon={<ClipboardList size={20} />} color="text-warning" />
      </StatsGrid>

      <PageSection title={t('operationalPlaybook.playbooks')}>
        {playbooks.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('operationalPlaybook.noPlaybooks')}</p>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {playbooks.map((pb) => (
              <Card key={pb.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between mb-2">
                    <p className="font-medium text-sm text-gray-900 dark:text-white">{pb.playbookName}</p>
                    <Badge variant={pb.status === 'active' ? 'success' : 'neutral'}>{pb.status}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                    {t('operationalPlaybook.trigger')}: {pb.trigger}
                  </p>
                  <div className="flex items-center gap-3 text-xs text-gray-400 mb-3">
                    <span className="flex items-center gap-1">
                      <Clock size={11} />
                      {t('operationalPlaybook.sla')}: {pb.sla}
                    </span>
                    <span className="flex items-center gap-1">
                      <Users size={11} />
                      {pb.owner}
                    </span>
                  </div>
                  <div className="flex items-center justify-between text-xs text-gray-400">
                    <span>{t('operationalPlaybook.stepCount')}: {pb.stepCount}</span>
                    <span>{t('operationalPlaybook.linkedServices')}: {pb.linkedServices}</span>
                  </div>
                  <Button size="sm" variant="ghost" className="mt-2 w-full">
                    <Edit2 size={12} className="mr-1" />
                    {t('operationalPlaybook.editPlaybook')}
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
