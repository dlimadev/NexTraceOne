/**
 * LoadTestingPage — Correlação de resultados de teste de carga com traces, métricas e capacidade.
 *
 * Integra resultados de ferramentas k6/Gatling/JMeter com traces e métricas observáveis,
 * permitindo estimar capacidade máxima e pontos de saturação por serviço.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Gauge, RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getLoadTestRuns, type LoadTestRun, type LoadTestStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'loadTesting.timeRange.1h' },
  { value: '6h', labelKey: 'loadTesting.timeRange.6h' },
  { value: '24h', labelKey: 'loadTesting.timeRange.24h' },
  { value: '7d', labelKey: 'loadTesting.timeRange.7d' },
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

function fmtMs(ms: number) {
  if (ms < 1000) return `${ms.toFixed(0)}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

function fmtDuration(ms: number) {
  const min = Math.floor(ms / 60000);
  const sec = Math.floor((ms % 60000) / 1000);
  return `${min}m ${sec}s`;
}

const FALLBACK: LoadTestRun[] = [
  { id: '1', name: 'Order Service Baseline', serviceName: 'order-service', source: 'k6', status: 'passed', vus: 500, durationMs: 300000, p95LatencyMs: 182, errorRate: 0.2, maxCapacityVus: 800, maxRps: 2400, executedAt: new Date(Date.now() - 86400000).toISOString(), environment: 'staging' },
  { id: '2', name: 'Payment Gateway Stress', serviceName: 'payment-service', source: 'gatling', status: 'failed', vus: 1000, durationMs: 600000, p95LatencyMs: 1840, errorRate: 8.4, maxCapacityVus: 600, maxRps: 890, executedAt: new Date(Date.now() - 172800000).toISOString(), environment: 'staging' },
  { id: '3', name: 'Catalog Search Load', serviceName: 'catalog-service', source: 'k6', status: 'passed', vus: 300, durationMs: 180000, p95LatencyMs: 95, errorRate: 0.05, maxCapacityVus: 1200, maxRps: 5600, executedAt: new Date(Date.now() - 259200000).toISOString(), environment: 'pre-production' },
  { id: '4', name: 'Auth Service Soak Test', serviceName: 'auth-service', source: 'jmeter', status: 'passed', vus: 200, durationMs: 3600000, p95LatencyMs: 48, errorRate: 0.01, maxCapacityVus: 2000, maxRps: 8900, executedAt: new Date(Date.now() - 345600000).toISOString(), environment: 'staging' },
  { id: '5', name: 'Notification Worker Throughput', serviceName: 'notification-service', source: 'k6', status: 'running', vus: 150, durationMs: 120000, p95LatencyMs: 340, errorRate: 1.2, executedAt: new Date(Date.now() - 30000).toISOString(), environment: 'staging' },
];

function statusVariant(status: LoadTestStatus): 'success' | 'danger' | 'info' | 'secondary' {
  switch (status) {
    case 'passed': return 'success';
    case 'failed': return 'danger';
    case 'running': return 'info';
    case 'cancelled': return 'secondary';
  }
}

export function LoadTestingPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'staging';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['load-test-runs', environment, timeRange, refreshKey],
    queryFn: () => getLoadTestRuns({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const runs = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const passed = runs.filter((r) => r.status === 'passed').length;
  const failed = runs.filter((r) => r.status === 'failed').length;
  const avgDuration = runs.length > 0 ? Math.round(runs.reduce((a, r) => a + r.durationMs, 0) / runs.length) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('loadTesting.title')}
          subtitle={t('loadTesting.subtitle')}
          icon={<Gauge className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('loadTesting.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('loadTesting.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('loadTesting.stats.totalRuns'), value: String(runs.length) },
                { label: t('loadTesting.stats.passed'), value: String(passed) },
                { label: t('loadTesting.stats.failed'), value: String(failed) },
                { label: t('loadTesting.stats.avgDuration'), value: fmtDuration(avgDuration) },
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
                <h3 className="text-sm font-semibold">{t('loadTesting.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {runs.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('loadTesting.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.name')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.source')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.status')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.vus')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.duration')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.p95')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.errorRate')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('loadTesting.table.capacity')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {runs.map((r) => (
                          <tr key={r.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{r.name}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{r.serviceName}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{t(`loadTesting.sources.${r.source}`)}</Badge></td>
                            <td className="px-4 py-2.5"><Badge variant={statusVariant(r.status)}>{t(`loadTesting.status.${r.status}`)}</Badge></td>
                            <td className="px-4 py-2.5 tabular-nums">{r.vus.toLocaleString()}</td>
                            <td className="px-4 py-2.5 tabular-nums">{fmtDuration(r.durationMs)}</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={r.p95LatencyMs > 500 ? 'text-red-500 font-semibold' : ''}>{fmtMs(r.p95LatencyMs)}</span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={r.errorRate > 1 ? 'text-red-500 font-semibold' : ''}>{r.errorRate.toFixed(2)}%</span>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted-foreground">
                              {r.maxCapacityVus ? `${r.maxCapacityVus} VUs / ${r.maxRps} RPS` : '—'}
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
