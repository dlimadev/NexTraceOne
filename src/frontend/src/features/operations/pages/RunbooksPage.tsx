import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, Search, FileText, Clock, Server, AlertTriangle, Plus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { incidentsApi } from '../api/incidents';

interface RunbookSummaryDto {
  runbookId: string;
  title: string;
  summary: string;
  linkedServiceId: string | null;
  linkedIncidentType: string | null;
  stepCount: number;
  createdAt: string;
}

interface RunbooksResponse {
  runbooks: RunbookSummaryDto[];
}

/**
 * Página de Runbooks — procedimentos operacionais e guias de mitigação.
 * Parte do módulo Operations do NexTraceOne.
 */
export function RunbooksPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');

  const { data, isLoading, isError, refetch } = useQuery<RunbooksResponse>({
    queryKey: ['runbooks', search],
    queryFn: () => incidentsApi.listRunbooks(search ? { search } : undefined),
  });

  const runbooks = data?.runbooks ?? [];
  const filtered = runbooks;
  const totalCount = filtered.length;
  const withService = filtered.filter((r) => r.linkedServiceId).length;
  const avgSteps = totalCount > 0 ? Math.round(filtered.reduce((s, r) => s + r.stepCount, 0) / totalCount) : 0;

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('runbooks.title')}
        subtitle={t('runbooks.subtitle')}
      />

      <PageSection>
        <div className="flex justify-end mb-4">
          <button
            onClick={() => navigate('/operations/runbooks/create')}
            className="flex items-center gap-1 px-3 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent/90 transition-colors"
          >
            <Plus size={14} />
            {t('runbooks.builder.createNew')}
          </button>
        </div>
      </PageSection>

      <StatsGrid columns={4}>
        <StatCard title={t('runbooks.stats.total')} value={totalCount} icon={<BookOpen size={20} />} color="text-accent" />
        <StatCard title={t('runbooks.stats.withService')} value={withService} icon={<Server size={20} />} color="text-info" />
        <StatCard title={t('runbooks.stats.avgSteps')} value={avgSteps} icon={<FileText size={20} />} color="text-warning" />
        <StatCard title={t('runbooks.stats.incidentTypes')} value={new Set(filtered.map((r) => r.linkedIncidentType).filter(Boolean)).size} icon={<AlertTriangle size={20} />} color="text-critical" />
      </StatsGrid>

      <PageSection>
        <div className="mb-4">
          <div className="relative">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              placeholder={t('runbooks.searchPlaceholder', 'Search runbooks...')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-10 pr-4 py-2 rounded-md border border-edge bg-surface text-sm text-body placeholder:text-muted focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
            />
          </div>
        </div>

        {filtered.length === 0 ? (
          <Card>
            <CardBody>
              <EmptyState
                icon={<BookOpen size={24} />}
                title={t('runbooks.emptyTitle')}
                description={t('productPolish.emptyRunbooks')}
              />
            </CardBody>
          </Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((runbook) => (
              <Card key={runbook.runbookId}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2">
                      <BookOpen size={16} className="text-accent shrink-0" />
                      <h3 className="text-sm font-semibold text-heading">{runbook.title}</h3>
                    </div>
                    <div className="flex items-center gap-2">
                      {runbook.linkedIncidentType && (
                        <Badge variant="warning">{runbook.linkedIncidentType}</Badge>
                      )}
                      <Badge variant="default">{runbook.stepCount} {t('runbooks.steps', 'steps')}</Badge>
                    </div>
                  </div>
                </CardHeader>
                <CardBody>
                  <p className="text-xs text-muted mb-2">{runbook.summary}</p>
                  <div className="flex items-center gap-4 text-xs text-muted">
                    {runbook.linkedServiceId && (
                      <span className="flex items-center gap-1">
                        <Server size={12} /> {runbook.linkedServiceId}
                      </span>
                    )}
                    <span className="flex items-center gap-1">
                      <Clock size={12} /> {new Date(runbook.createdAt).toLocaleDateString()}
                    </span>
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
