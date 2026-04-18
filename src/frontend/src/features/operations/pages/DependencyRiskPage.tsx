/**
 * DependencyRiskPage — Scoring de risco de dependências com matriz de impacto downstream.
 *
 * Classifica serviços por score de risco (0-10) considerando histórico de falhas,
 * SLO health, blast radius e frequência de deploy, com matriz de impacto entre serviços.
 *
 * @module operations/reliability
 * @pillar Service Governance, Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitFork, RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getDependencyRisks, type DependencyRiskEntry, type RiskLevel } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'dependencyRisk.timeRange.1h' },
  { value: '6h', labelKey: 'dependencyRisk.timeRange.6h' },
  { value: '24h', labelKey: 'dependencyRisk.timeRange.24h' },
  { value: '7d', labelKey: 'dependencyRisk.timeRange.7d' },
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

const FALLBACK: DependencyRiskEntry[] = [
  { id: '1', serviceName: 'payment-service', riskScore: 9.1, riskLevel: 'critical', failureCount30d: 18, sloHealthPercent: 87.4, blastRadius: 12, deployFrequency: 8.2, dependentsCount: 12, trendDirection: 'up', environment: 'production' },
  { id: '2', serviceName: 'order-service', riskScore: 7.4, riskLevel: 'high', failureCount30d: 11, sloHealthPercent: 92.1, blastRadius: 9, deployFrequency: 12.5, dependentsCount: 9, trendDirection: 'stable', environment: 'production' },
  { id: '3', serviceName: 'auth-service', riskScore: 6.8, riskLevel: 'high', failureCount30d: 5, sloHealthPercent: 98.4, blastRadius: 25, deployFrequency: 4.1, dependentsCount: 25, trendDirection: 'down', environment: 'production' },
  { id: '4', serviceName: 'catalog-service', riskScore: 3.2, riskLevel: 'medium', failureCount30d: 2, sloHealthPercent: 99.6, blastRadius: 6, deployFrequency: 15.8, dependentsCount: 6, trendDirection: 'down', environment: 'production' },
  { id: '5', serviceName: 'notification-service', riskScore: 2.1, riskLevel: 'low', failureCount30d: 1, sloHealthPercent: 99.9, blastRadius: 3, deployFrequency: 6.0, dependentsCount: 3, trendDirection: 'stable', environment: 'production' },
];

function riskVariant(level: RiskLevel): 'danger' | 'warning' | 'info' | 'success' {
  switch (level) {
    case 'critical': return 'danger';
    case 'high': return 'warning';
    case 'medium': return 'info';
    case 'low': return 'success';
  }
}

function riskScoreColor(score: number): string {
  if (score >= 8) return 'text-red-500 font-bold';
  if (score >= 6) return 'text-amber-500 font-semibold';
  if (score >= 4) return 'text-blue-500';
  return 'text-emerald-500';
}

export function DependencyRiskPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dependency-risks', environment, timeRange, refreshKey],
    queryFn: () => getDependencyRisks({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const entries = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const critical = entries.filter((e) => e.riskLevel === 'critical').length;
  const high = entries.filter((e) => e.riskLevel === 'high').length;
  const avgScore = entries.length > 0 ? (entries.reduce((a, e) => a + e.riskScore, 0) / entries.length).toFixed(1) : '0';

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('dependencyRisk.title')}
          subtitle={t('dependencyRisk.subtitle')}
          icon={<GitFork className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('dependencyRisk.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('dependencyRisk.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('dependencyRisk.stats.totalServices'), value: String(entries.length) },
                { label: t('dependencyRisk.stats.critical'), value: String(critical) },
                { label: t('dependencyRisk.stats.high'), value: String(high) },
                { label: t('dependencyRisk.stats.avgScore'), value: avgScore },
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
                <h3 className="text-sm font-semibold">{t('dependencyRisk.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {entries.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('dependencyRisk.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.riskScore')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.failureHistory')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.sloHealth')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.blastRadius')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.dependents')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dependencyRisk.table.trend')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {entries.map((e) => (
                          <tr key={e.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{e.serviceName}</td>
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-2">
                                <span className={`tabular-nums ${riskScoreColor(e.riskScore)}`}>{e.riskScore.toFixed(1)}</span>
                                <Badge variant={riskVariant(e.riskLevel)}>{t(`dependencyRisk.riskLevel.${e.riskLevel}`)}</Badge>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums text-muted-foreground">{e.failureCount30d}x</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={e.sloHealthPercent < 95 ? 'text-amber-500 font-semibold' : 'text-emerald-500'}>
                                {e.sloHealthPercent.toFixed(1)}%
                              </span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">{e.blastRadius} {e.blastRadius === 1 ? 'svc' : 'svcs'}</td>
                            <td className="px-4 py-2.5 tabular-nums">{e.dependentsCount}</td>
                            <td className="px-4 py-2.5">
                              <span className={e.trendDirection === 'up' ? 'text-red-500' : e.trendDirection === 'down' ? 'text-emerald-500' : 'text-muted-foreground'}>
                                {e.trendDirection === 'up' ? '↑' : e.trendDirection === 'down' ? '↓' : '→'}
                              </span>
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
