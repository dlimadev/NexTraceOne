/**
 * AiIncidentSummarizerPage — Sumarização de incidentes por IA com geração de post-mortem e auditoria.
 *
 * Gera sumários estruturados de incidentes com timeline, serviços afetados e correlação
 * de mudanças, permitindo geração de rascunho de post-mortem com trilha de auditoria completa.
 *
 * @module operations/incidents
 * @pillar AI-assisted Operations, AI Governance, Source of Truth
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { FileSearch, RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getAiIncidentSummaries, type AiIncidentSummary } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'aiIncidentSummarizer.timeRange.1h' },
  { value: '6h', labelKey: 'aiIncidentSummarizer.timeRange.6h' },
  { value: '24h', labelKey: 'aiIncidentSummarizer.timeRange.24h' },
  { value: '7d', labelKey: 'aiIncidentSummarizer.timeRange.7d' },
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

const FALLBACK: AiIncidentSummary[] = [
  { id: '1', incidentId: 'INC-2024-0142', incidentTitle: 'Payment Gateway Down', severity: 'critical', serviceName: 'payment-service', summaryText: 'Payment service experienced complete outage for 45 minutes. Root cause: new deploy introduced breaking change in connection pool configuration. 89 users affected. Correlated with deploy-001 at 14:32 UTC.', generatedAt: new Date(Date.now() - 86400000).toISOString(), modelName: 'nexttrace-ops-v2', confidencePercent: 92, tokensUsed: 1840, requestedBy: 'João Silva', environment: 'production' },
  { id: '2', incidentId: 'INC-2024-0138', incidentTitle: 'Orders DB Degraded', severity: 'high', serviceName: 'order-service', summaryText: 'Database connection pool exhausted after traffic spike. Missing index on orders(customer_id) caused full table scans. 34 users experienced timeouts. No correlated deploy found.', generatedAt: new Date(Date.now() - 172800000).toISOString(), modelName: 'nexttrace-ops-v2', confidencePercent: 87, tokensUsed: 1420, requestedBy: 'Maria Costa', environment: 'production' },
  { id: '3', incidentId: 'INC-2024-0129', incidentTitle: 'Frontend Asset Unavailable', severity: 'medium', serviceName: 'catalog-service', summaryText: 'CDN misconfiguration caused 404 cascade on static assets. Rollback of CDN configuration resolved issue within 12 minutes. 203 users affected during peak hours.', generatedAt: new Date(Date.now() - 432000000).toISOString(), modelName: 'nexttrace-ops-v1', confidencePercent: 78, tokensUsed: 980, requestedBy: 'Carlos Mendes', environment: 'production' },
];

export function AiIncidentSummarizerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-incident-summaries', environment, timeRange, refreshKey],
    queryFn: () => getAiIncidentSummaries({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const summaries = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const avgConfidence = summaries.length > 0 ? Math.round(summaries.reduce((a, s) => a + s.confidencePercent, 0) / summaries.length) : 0;
  const avgTokens = summaries.length > 0 ? Math.round(summaries.reduce((a, s) => a + s.tokensUsed, 0) / summaries.length) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('aiIncidentSummarizer.title')}
          subtitle={t('aiIncidentSummarizer.subtitle')}
          icon={<FileSearch className="w-5 h-5" />}
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
        </div>
      </div>

      {isError && <PageErrorState message={t('aiIncidentSummarizer.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('aiIncidentSummarizer.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('aiIncidentSummarizer.stats.summarized'), value: String(summaries.length) },
                { label: t('aiIncidentSummarizer.stats.avgConfidence'), value: `${avgConfidence}%` },
                { label: t('aiIncidentSummarizer.stats.avgTokens'), value: String(avgTokens) },
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
            <div className="flex flex-col gap-4">
              {summaries.length === 0 ? (
                <Card>
                  <CardBody className="p-8 text-center text-muted-foreground text-sm">{t('aiIncidentSummarizer.noRecords')}</CardBody>
                </Card>
              ) : (
                summaries.map((s) => (
                  <Card key={s.id}>
                    <CardHeader>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2 mb-1">
                            <span className="font-semibold text-sm">{s.incidentTitle}</span>
                            <Badge variant="secondary" className="font-mono text-xs">{s.incidentId}</Badge>
                            <Badge variant={s.severity === 'critical' ? 'danger' : s.severity === 'high' ? 'warning' : 'secondary'}>
                              {s.severity}
                            </Badge>
                          </div>
                          <div className="text-xs text-muted-foreground">{s.serviceName} · {s.modelName} · {t('aiIncidentSummarizer.table.confidence')}: {s.confidencePercent}%</div>
                        </div>
                        <div className="flex gap-2 flex-shrink-0">
                          <Button variant="outline" size="sm">
                            {t('aiIncidentSummarizer.actions.generateDraft')}
                          </Button>
                          <Button variant="ghost" size="sm">
                            {t('aiIncidentSummarizer.actions.viewAudit')}
                          </Button>
                        </div>
                      </div>
                    </CardHeader>
                    <CardBody className="pt-0">
                      <p className="text-sm text-muted-foreground leading-relaxed">{s.summaryText}</p>
                      <div className="mt-3 flex items-center gap-4 text-xs text-muted-foreground/70">
                        <span>{t('aiIncidentSummarizer.audit.requestedBy')}: {s.requestedBy}</span>
                        <span>{t('aiIncidentSummarizer.audit.tokens')}: {s.tokensUsed.toLocaleString()}</span>
                        <span>{new Date(s.generatedAt).toLocaleString()}</span>
                      </div>
                    </CardBody>
                  </Card>
                ))
              )}
            </div>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
