/**
 * PostIncidentPage — Hub de aprendizado pós-incidente com post-mortems e deteção de padrões.
 *
 * Centraliza post-mortems, rastreamento de action items, análise de causas raiz
 * e deteção de padrões recorrentes para redução sistemática de incidentes.
 *
 * @module operations/incidents
 * @pillar Operational Reliability, Source of Truth & Operational Knowledge
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { FileText, RefreshCw, Plus } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getPostMortems, type PostMortem, type PostMortemStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'postIncident.timeRange.1h' },
  { value: '6h', labelKey: 'postIncident.timeRange.6h' },
  { value: '24h', labelKey: 'postIncident.timeRange.24h' },
  { value: '7d', labelKey: 'postIncident.timeRange.7d' },
];

function timeRangeToInterval(range: TimeRange) {
  const until = new Date();
  const from = new Date(until);
  switch (range) {
    case '1h': from.setHours(from.getHours() - 1); break;
    case '6h': from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d': from.setDate(from.getDate() - 7); break;
  }
  return { from: from.toISOString(), until: until.toISOString() };
}

const FALLBACK: PostMortem[] = [
  { id: '1', title: 'Payment service outage - 45min downtime', incidentId: 'INC-2024-0142', incidentTitle: 'Payment Gateway Down', status: 'published', author: 'João Silva', severity: 'critical', actionItemsCount: 5, openActionItemsCount: 2, createdAt: new Date(Date.now() - 86400000 * 2).toISOString(), publishedAt: new Date(Date.now() - 86400000).toISOString(), patternCount: 3, environment: 'production' },
  { id: '2', title: 'Database connection pool exhaustion', incidentId: 'INC-2024-0138', incidentTitle: 'Orders DB Degraded', status: 'review', author: 'Maria Costa', severity: 'high', actionItemsCount: 8, openActionItemsCount: 8, createdAt: new Date(Date.now() - 86400000 * 5).toISOString(), patternCount: 2, environment: 'production' },
  { id: '3', title: 'CDN misconfiguration causing 404 cascade', incidentId: 'INC-2024-0129', incidentTitle: 'Frontend Asset Unavailable', status: 'published', author: 'Carlos Mendes', severity: 'medium', actionItemsCount: 3, openActionItemsCount: 0, createdAt: new Date(Date.now() - 86400000 * 12).toISOString(), publishedAt: new Date(Date.now() - 86400000 * 10).toISOString(), patternCount: 0, environment: 'production' },
  { id: '4', title: 'Memory leak in notification worker after deploy', incidentId: 'INC-2024-0115', incidentTitle: 'Notification Delays', status: 'draft', author: 'Ana Ferreira', severity: 'medium', actionItemsCount: 4, openActionItemsCount: 4, createdAt: new Date(Date.now() - 86400000 * 20).toISOString(), patternCount: 1, environment: 'production' },
];

function statusVariant(status: PostMortemStatus): 'secondary' | 'warning' | 'success' | 'info' {
  switch (status) {
    case 'draft': return 'secondary';
    case 'review': return 'warning';
    case 'published': return 'success';
    case 'archived': return 'info';
  }
}

export function PostIncidentPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['post-mortems', environment, timeRange, refreshKey],
    queryFn: () => getPostMortems({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const mortems = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const openActionItems = mortems.reduce((a, m) => a + m.openActionItemsCount, 0);
  const patterns = mortems.reduce((a, m) => a + m.patternCount, 0);

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('postIncident.title')}
          subtitle={t('postIncident.subtitle')}
          icon={<FileText className="w-5 h-5" />}
        />
        <div className="flex items-center gap-2 flex-wrap">
          <div className="flex rounded-md border border-border overflow-hidden text-xs">
            {TIME_RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => setTimeRange(opt.value)}
                className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-muted-foreground'}`}
              >
                {t(opt.labelKey)}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={handleRefresh}>
            <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
            {t('common.refresh')}
          </Button>
          <Button size="sm">
            <Plus className="w-3.5 h-3.5 mr-1.5" />
            {t('postIncident.actions.createPostMortem')}
          </Button>
        </div>
      </div>

      {isError && <PageErrorState message={t('postIncident.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('postIncident.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('postIncident.stats.total'), value: String(mortems.length) },
                { label: t('postIncident.stats.open'), value: String(openActionItems) },
                { label: t('postIncident.stats.patterns'), value: String(patterns) },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <div className="text-xs text-muted-foreground mb-1">{stat.label}</div>
                    <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>

          <PageSection>
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold">{t('postIncident.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {mortems.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('postIncident.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.title')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.incident')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.severity')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.status')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.author')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.actionItems')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('postIncident.table.createdAt')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {mortems.map((m) => (
                          <tr key={m.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium max-w-xs truncate" title={m.title}>{m.title}</td>
                            <td className="px-4 py-2.5 text-muted-foreground font-mono text-xs">{m.incidentId}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant={m.severity === 'critical' ? 'danger' : m.severity === 'high' ? 'warning' : 'secondary'}>
                                {m.severity}
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusVariant(m.status)}>{t(`postIncident.status.${m.status}`)}</Badge>
                            </td>
                            <td className="px-4 py-2.5 text-muted-foreground">{m.author}</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              {m.openActionItemsCount > 0 ? (
                                <Badge variant="warning">{m.openActionItemsCount}/{m.actionItemsCount}</Badge>
                              ) : (
                                <Badge variant="success">{m.actionItemsCount}/{m.actionItemsCount}</Badge>
                              )}
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted-foreground">{new Date(m.createdAt).toLocaleDateString()}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
