import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Clock, Filter } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { incidentsApi } from '../api/incidents';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type SourceFilter = 'all' | 'incident' | 'observability';

function badgeVariantForSource(source: string): 'info' | 'warning' | 'danger' | 'success' | 'default' {
  if (source === 'incident') return 'warning';
  if (source === 'observability') return 'info';
  return 'default';
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

export function IncidentTimelinePage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [sourceFilter, setSourceFilter] = useState<SourceFilter>('all');
  const [environment, setEnvironment] = useState(activeEnvironmentId ?? '');
  const [serviceName, setServiceName] = useState('');
  const [page] = useState(1);
  const [pageSize] = useState(80);

  const timelineQuery = useQuery({
    queryKey: ['incidents-unified-timeline', environment, serviceName, page, pageSize],
    queryFn: () => incidentsApi.getUnifiedTimeline({
      environment: environment || undefined,
      serviceName: serviceName || undefined,
      page,
      pageSize,
    }),
  });

  const filteredEntries = useMemo(() => {
    const entries = timelineQuery.data?.entries ?? [];
    if (sourceFilter === 'all') return entries;
    return entries.filter((entry) => entry.source === sourceFilter);
  }, [timelineQuery.data?.entries, sourceFilter]);

  if (timelineQuery.isLoading) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (timelineQuery.isError) {
    return (
      <PageContainer>
        <PageErrorState />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('incidents.timelineView.title')}
        subtitle={t('incidents.timelineView.subtitle')}
      />

      <Card className="mb-4">
        <CardBody className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2 text-xs text-muted">
            <Filter size={14} /> {t('incidents.timelineView.filters')}
          </div>
          <input
            value={serviceName}
            onChange={(e) => setServiceName(e.target.value)}
            placeholder={t('incidents.timelineView.servicePlaceholder')}
            className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
          />
          <input
            value={environment}
            onChange={(e) => setEnvironment(e.target.value)}
            placeholder={t('incidents.timelineView.environmentPlaceholder')}
            className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
          />
          <select
            value={sourceFilter}
            onChange={(e) => setSourceFilter(e.target.value as SourceFilter)}
            className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
          >
            <option value="all">{t('incidents.timelineView.source.all')}</option>
            <option value="incident">{t('incidents.timelineView.source.incident')}</option>
            <option value="observability">{t('incidents.timelineView.source.observability')}</option>
          </select>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Clock size={16} className="text-accent" />
            {t('incidents.timelineView.entries', { count: filteredEntries.length })}
          </h2>
        </CardHeader>
        <CardBody>
          {filteredEntries.length === 0 ? (
            <p className="text-sm text-muted">{t('incidents.timelineView.empty')}</p>
          ) : (
            <div className="space-y-3">
              {filteredEntries.map((entry, idx) => (
                <div key={`${entry.id}-${idx}`} className="flex gap-3">
                  <div className="flex flex-col items-center">
                    <div className={`w-2 h-2 rounded-full mt-1.5 ${entry.source === 'incident' ? 'bg-warning' : 'bg-info'}`} />
                    {idx < filteredEntries.length - 1 && <div className="w-px flex-1 bg-edge" />}
                  </div>
                  <div className="pb-3 flex-1 rounded bg-elevated p-3">
                    <div className="flex flex-wrap items-center gap-2 mb-1">
                      <Badge variant={badgeVariantForSource(entry.source)}>{entry.source}</Badge>
                      <span className="text-xs text-muted">{entry.eventType}</span>
                      <span className="text-xs text-muted">•</span>
                      <span className="text-xs text-muted">{formatDate(entry.timestamp)}</span>
                    </div>
                    <p className="text-sm text-body">{entry.title ?? t('incidents.timelineView.noTitle')}</p>
                    {(entry.serviceName || entry.systemName) && (
                      <p className="text-xs text-muted mt-1">
                        {entry.serviceName ?? '-'}
                        {entry.systemName ? ` • ${entry.systemName}` : ''}
                      </p>
                    )}
                    {entry.details && Object.keys(entry.details).length > 0 && (
                      <div className="mt-2 text-xs text-muted">
                        {Object.entries(entry.details).slice(0, 3).map(([key, value]) => (
                          <p key={key}>{key}: {value}</p>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}
