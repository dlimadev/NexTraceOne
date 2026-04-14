import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { FileText, Wand2, Download, List } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface ResolvedIncident {
  id: string;
  title: string;
  resolvedAt: string;
  duration: string;
  severity: string;
  hasDraft: boolean;
}

interface PostMortemResponse {
  incidents: ResolvedIncident[];
  total: number;
  withDraft: number;
}

const usePostMortemIncidents = () =>
  useQuery({
    queryKey: ['post-mortem-incidents'],
    queryFn: () =>
      client
        .get<PostMortemResponse>('/incidents/post-mortem/resolved')
        .then((r) => r.data),
  });

export function IncidentPostMortemPage() {
  const { t } = useTranslation();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const { data, isLoading, isError, refetch } = usePostMortemIncidents();

  if (isLoading) return <PageLoadingState message={t('incidentPostMortem.loading')} />;
  if (isError) return <PageErrorState message={t('incidentPostMortem.error')} onRetry={() => refetch()} />;

  const incidents = data?.incidents ?? [];

  const handleGenerate = () => {
    if (!selectedId) return;
    setGenerating(true);
    setTimeout(() => setGenerating(false), 2000);
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('incidentPostMortem.title')}
        subtitle={t('incidentPostMortem.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button
              size="sm"
              onClick={handleGenerate}
              disabled={!selectedId || generating}
            >
              <Wand2 size={14} className="mr-1" />
              {generating ? t('incidentPostMortem.generating') : t('incidentPostMortem.generatePostMortem')}
            </Button>
          </div>
        }
      />

      <StatsGrid columns={2}>
        <StatCard title={t('common.total')} value={data?.total ?? 0} icon={<List size={20} />} color="text-accent" />
        <StatCard title={t('incidentPostMortem.draft')} value={data?.withDraft ?? 0} icon={<FileText size={20} />} color="text-info" />
      </StatsGrid>

      <PageSection title={t('incidentPostMortem.incidents')}>
        {incidents.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('incidentPostMortem.noResolved')}</p>
        ) : (
          <div className="space-y-2">
            {incidents.map((inc) => (
              <Card
                key={inc.id}
                className={`cursor-pointer transition-colors ${selectedId === inc.id ? 'ring-2 ring-indigo-500' : ''}`}
                onClick={() => setSelectedId(inc.id === selectedId ? null : inc.id)}
              >
                <CardBody className="p-3 flex items-center gap-3">
                  <List size={14} className="text-gray-400 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-sm text-gray-900 dark:text-white">{inc.title}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {inc.resolvedAt} · {inc.duration} · {inc.severity}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    {inc.hasDraft && (
                      <Badge variant="neutral">{t('incidentPostMortem.draft')}</Badge>
                    )}
                    <Button size="sm" variant="ghost" onClick={(e) => e.stopPropagation()}>
                      <Download size={12} className="mr-1" />
                      {t('incidentPostMortem.export')}
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
