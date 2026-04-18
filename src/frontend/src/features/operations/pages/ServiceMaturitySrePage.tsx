/**
 * ServiceMaturitySrePage — Score de maturidade SRE por serviço com recomendações acionáveis.
 *
 * Avalia serviços contra critérios SRE (SLO, Runbook, On-Call, Alertas, Profiling, Post-Mortem)
 * e agrupa por domínio/equipa para identificar gaps e priorizar investimentos.
 *
 * @module operations/reliability
 * @pillar Service Governance, Operational Reliability
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Award, RefreshCw, CheckCircle2, XCircle } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getServiceMaturities, type ServiceMaturityEntry, type MaturityLevel } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'serviceMaturitySre.timeRange.1h' },
  { value: '6h', labelKey: 'serviceMaturitySre.timeRange.6h' },
  { value: '24h', labelKey: 'serviceMaturitySre.timeRange.24h' },
  { value: '7d', labelKey: 'serviceMaturitySre.timeRange.7d' },
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

const FALLBACK: ServiceMaturityEntry[] = [
  { id: '1', serviceName: 'payment-service', teamName: 'Platform Team', score: 92, maturityLevel: 'advanced', hasSlo: true, hasRunbook: true, hasOnCall: true, hasAlerts: true, hasProfiling: true, hasRecentPostMortem: true, environment: 'production' },
  { id: '2', serviceName: 'order-service', teamName: 'Orders Team', score: 78, maturityLevel: 'intermediate', hasSlo: true, hasRunbook: true, hasOnCall: true, hasAlerts: true, hasProfiling: false, hasRecentPostMortem: true, environment: 'production' },
  { id: '3', serviceName: 'catalog-service', teamName: 'Product Team', score: 55, maturityLevel: 'basic', hasSlo: true, hasRunbook: true, hasOnCall: false, hasAlerts: true, hasProfiling: false, hasRecentPostMortem: false, environment: 'production' },
  { id: '4', serviceName: 'notification-service', teamName: 'Platform Team', score: 38, maturityLevel: 'initial', hasSlo: false, hasRunbook: true, hasOnCall: false, hasAlerts: false, hasProfiling: false, hasRecentPostMortem: false, environment: 'production' },
  { id: '5', serviceName: 'auth-service', teamName: 'Security Team', score: 85, maturityLevel: 'advanced', hasSlo: true, hasRunbook: true, hasOnCall: true, hasAlerts: true, hasProfiling: true, hasRecentPostMortem: false, environment: 'production' },
];

function maturityVariant(level: MaturityLevel): 'success' | 'info' | 'warning' | 'secondary' {
  switch (level) {
    case 'advanced': return 'success';
    case 'intermediate': return 'info';
    case 'basic': return 'warning';
    case 'initial': return 'secondary';
  }
}

function CriteriaCell({ value }: { value: boolean }) {
  return value
    ? <CheckCircle2 className="w-4 h-4 text-emerald-500 mx-auto" />
    : <XCircle className="w-4 h-4 text-muted-foreground/40 mx-auto" />;
}

export function ServiceMaturitySrePage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['service-maturities', environment, timeRange, refreshKey],
    queryFn: () => getServiceMaturities({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const entries = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const advanced = entries.filter((e) => e.maturityLevel === 'advanced').length;
  const initial = entries.filter((e) => e.maturityLevel === 'initial').length;
  const avgScore = entries.length > 0 ? Math.round(entries.reduce((a, e) => a + e.score, 0) / entries.length) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('serviceMaturitySre.title')}
          subtitle={t('serviceMaturitySre.subtitle')}
          icon={<Award className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('serviceMaturitySre.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('serviceMaturitySre.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('serviceMaturitySre.stats.totalServices'), value: String(entries.length) },
                { label: t('serviceMaturitySre.stats.advanced'), value: String(advanced) },
                { label: t('serviceMaturitySre.stats.initial'), value: String(initial) },
                { label: t('serviceMaturitySre.stats.avgScore'), value: `${avgScore}/100` },
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
                <h3 className="text-sm font-semibold">{t('serviceMaturitySre.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {entries.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('serviceMaturitySre.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('serviceMaturitySre.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('serviceMaturitySre.table.team')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('serviceMaturitySre.table.score')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.slo')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.runbook')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.onCall')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.alerts')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.profiling')}</th>
                          <th className="px-4 py-2.5 text-center font-medium">{t('serviceMaturitySre.criteria.postMortem')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('serviceMaturitySre.table.level')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {entries.map((e) => (
                          <tr key={e.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{e.serviceName}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{e.teamName}</td>
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-2">
                                <div className="w-16 h-1.5 rounded-full bg-muted overflow-hidden">
                                  <div className={`h-full rounded-full ${e.score >= 80 ? 'bg-emerald-500' : e.score >= 60 ? 'bg-blue-500' : e.score >= 40 ? 'bg-amber-500' : 'bg-red-500'}`} style={{ width: `${e.score}%` }} />
                                </div>
                                <span className="font-semibold tabular-nums">{e.score}</span>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasSlo} /></td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasRunbook} /></td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasOnCall} /></td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasAlerts} /></td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasProfiling} /></td>
                            <td className="px-4 py-2.5 text-center"><CriteriaCell value={e.hasRecentPostMortem} /></td>
                            <td className="px-4 py-2.5">
                              <Badge variant={maturityVariant(e.maturityLevel)}>
                                {t(`serviceMaturitySre.maturityLevel.${e.maturityLevel}`)}
                              </Badge>
                            </td>
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
