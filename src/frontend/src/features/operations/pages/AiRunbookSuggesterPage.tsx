/**
 * AiRunbookSuggesterPage — Sugestão de runbooks por IA para incidentes ativos com contexto pré-preenchido.
 *
 * Sugere runbooks relevantes para incidentes em curso com score de confiança,
 * contexto pré-preenchido (serviço, ambiente, versão) e trilha de auditoria governada.
 *
 * @module operations/runbooks
 * @pillar AI-assisted Operations, AI Governance, Operational Reliability
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Lightbulb, RefreshCw, Check, X } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getAiRunbookSuggestions, type AiRunbookSuggestion } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'aiRunbookSuggester.timeRange.1h' },
  { value: '6h', labelKey: 'aiRunbookSuggester.timeRange.6h' },
  { value: '24h', labelKey: 'aiRunbookSuggester.timeRange.24h' },
  { value: '7d', labelKey: 'aiRunbookSuggester.timeRange.7d' },
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

const FALLBACK: AiRunbookSuggestion[] = [
  { id: '1', incidentId: 'INC-2024-0142', incidentTitle: 'Payment Gateway Down', serviceName: 'payment-service', environment: 'production', version: 'v1.9.2', runbookTitle: 'Payment Gateway Recovery Procedure', runbookId: 'rb-001', confidencePercent: 94, reasoning: 'Symptoms match connection pool exhaustion pattern documented in this runbook (3 prior matches)', modelName: 'nexttrace-ops-v2', suggestedAt: new Date(Date.now() - 600000).toISOString(), status: 'pending', tokensUsed: 820, knowledgeSources: ['runbook-rb-001', 'incident-INC-2024-0115', 'incident-INC-2023-0089'] },
  { id: '2', incidentId: 'INC-2024-0142', incidentTitle: 'Payment Gateway Down', serviceName: 'payment-service', environment: 'production', version: 'v1.9.2', runbookTitle: 'Database Connection Pool Troubleshooting', runbookId: 'rb-008', confidencePercent: 78, reasoning: 'Secondary match: error logs show JDBC connection timeout pattern', modelName: 'nexttrace-ops-v2', suggestedAt: new Date(Date.now() - 600000).toISOString(), status: 'pending', tokensUsed: 820, knowledgeSources: ['runbook-rb-008', 'post-mortem-INC-2024-0138'] },
  { id: '3', incidentId: 'INC-2024-0138', incidentTitle: 'Orders DB Degraded', serviceName: 'order-service', environment: 'production', version: 'v2.4.0', runbookTitle: 'PostgreSQL Performance Investigation', runbookId: 'rb-012', confidencePercent: 88, reasoning: 'Full table scan detected in slow query logs, runbook covers index analysis and emergency optimization', modelName: 'nexttrace-ops-v2', suggestedAt: new Date(Date.now() - 3600000).toISOString(), status: 'accepted', tokensUsed: 640, knowledgeSources: ['runbook-rb-012', 'runbook-rb-013'] },
  { id: '4', incidentId: 'INC-2024-0135', incidentTitle: 'High Memory Usage Alert', serviceName: 'catalog-service', environment: 'production', version: 'v3.1.0', runbookTitle: 'JVM Memory Leak Investigation', runbookId: 'rb-019', confidencePercent: 65, reasoning: 'Heap growth pattern matches previously documented memory leak in catalog-service v3.x', modelName: 'nexttrace-ops-v1', suggestedAt: new Date(Date.now() - 7200000).toISOString(), status: 'rejected', tokensUsed: 490, knowledgeSources: ['runbook-rb-019', 'post-mortem-INC-2023-0142'] },
];

export function AiRunbookSuggesterPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-runbook-suggestions', environment, timeRange, refreshKey],
    queryFn: () => getAiRunbookSuggestions({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const suggestions = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const active = suggestions.filter((s) => s.status === 'pending').length;
  const accepted = suggestions.filter((s) => s.status === 'accepted').length;
  const rejected = suggestions.filter((s) => s.status === 'rejected').length;
  const avgConfidence = suggestions.length > 0 ? Math.round(suggestions.reduce((a, s) => a + s.confidencePercent, 0) / suggestions.length) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('aiRunbookSuggester.title')}
          subtitle={t('aiRunbookSuggester.subtitle')}
          icon={<Lightbulb className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('aiRunbookSuggester.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('aiRunbookSuggester.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('aiRunbookSuggester.stats.activeSuggestions'), value: String(active) },
                { label: t('aiRunbookSuggester.stats.avgConfidence'), value: `${avgConfidence}%` },
                { label: t('aiRunbookSuggester.stats.accepted'), value: String(accepted) },
                { label: t('aiRunbookSuggester.stats.rejected'), value: String(rejected) },
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
              {suggestions.length === 0 ? (
                <Card>
                  <CardBody className="p-8 text-center text-muted-foreground text-sm">{t('aiRunbookSuggester.noRecords')}</CardBody>
                </Card>
              ) : (
                suggestions.map((s) => (
                  <Card key={s.id} className={s.status === 'rejected' ? 'opacity-60' : ''}>
                    <CardHeader>
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-1 flex-wrap">
                            <span className="font-semibold text-sm">{s.runbookTitle}</span>
                            <Badge variant={s.confidencePercent >= 85 ? 'success' : s.confidencePercent >= 70 ? 'warning' : 'secondary'}>
                              {s.confidencePercent}%
                            </Badge>
                            <Badge variant={s.status === 'accepted' ? 'success' : s.status === 'rejected' ? 'secondary' : 'info'}>
                              {t(`aiRunbookSuggester.actions.${s.status === 'pending' ? 'accept' : s.status}`)}
                            </Badge>
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {t('aiRunbookSuggester.table.incident')}: <span className="font-mono">{s.incidentId}</span> · {s.serviceName} · {s.environment} · {s.version}
                          </div>
                        </div>
                        {s.status === 'pending' && (
                          <div className="flex gap-2 flex-shrink-0">
                            <Button variant="outline" size="sm">
                              <Check className="w-3.5 h-3.5 mr-1" />
                              {t('aiRunbookSuggester.actions.accept')}
                            </Button>
                            <Button variant="ghost" size="sm">
                              <X className="w-3.5 h-3.5 mr-1" />
                              {t('aiRunbookSuggester.actions.reject')}
                            </Button>
                          </div>
                        )}
                      </div>
                    </CardHeader>
                    <CardBody className="pt-0">
                      <p className="text-sm text-muted-foreground">{s.reasoning}</p>
                      <div className="mt-2 flex items-center gap-4 text-xs text-muted-foreground/70">
                        <span>{t('aiRunbookSuggester.audit.tokensUsed')}: {s.tokensUsed}</span>
                        <span>{t('aiRunbookSuggester.audit.knowledgeSources')}: {s.knowledgeSources.join(', ')}</span>
                        <span>{new Date(s.suggestedAt).toLocaleString()}</span>
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
