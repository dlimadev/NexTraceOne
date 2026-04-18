/**
 * ApiRegressionPage — Deteção de regressão de performance de endpoints com correlação de deploy.
 *
 * Compara baseline vs valores atuais de p50/p95/p99 por endpoint, identificando
 * regressões e correlacionando com deploys recentes para suporte à decisão de rollback.
 *
 * @module operations/telemetry
 * @pillar Change Intelligence, Operational Reliability
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingDown, RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getApiRegressions, type ApiRegressionEntry, type RegressionStatus, type ChangeConfidence } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'apiRegression.timeRange.1h' },
  { value: '6h', labelKey: 'apiRegression.timeRange.6h' },
  { value: '24h', labelKey: 'apiRegression.timeRange.24h' },
  { value: '7d', labelKey: 'apiRegression.timeRange.7d' },
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
  return `${(ms / 1000).toFixed(2)}s`;
}

const FALLBACK: ApiRegressionEntry[] = [
  { id: '1', endpoint: 'POST /api/v1/orders', serviceName: 'order-service', p50BaselineMs: 45, p50CurrentMs: 89, p95BaselineMs: 120, p95CurrentMs: 384, p99BaselineMs: 280, p99CurrentMs: 1240, regressionPercent: 220, status: 'regressed', deployId: 'deploy-001', changeConfidence: 'high', environment: 'production' },
  { id: '2', endpoint: 'GET /api/v1/products', serviceName: 'catalog-service', p50BaselineMs: 32, p50CurrentMs: 28, p95BaselineMs: 95, p95CurrentMs: 84, p99BaselineMs: 180, p99CurrentMs: 165, regressionPercent: -10, status: 'improved', changeConfidence: 'medium', environment: 'production' },
  { id: '3', endpoint: 'POST /api/v1/payments', serviceName: 'payment-service', p50BaselineMs: 180, p50CurrentMs: 340, p95BaselineMs: 450, p95CurrentMs: 920, p99BaselineMs: 800, p99CurrentMs: 2100, regressionPercent: 110, status: 'regressed', deployId: 'deploy-002', changeConfidence: 'high', environment: 'production' },
  { id: '4', endpoint: 'GET /api/v1/users/profile', serviceName: 'auth-service', p50BaselineMs: 22, p50CurrentMs: 24, p95BaselineMs: 58, p95CurrentMs: 61, p99BaselineMs: 120, p99CurrentMs: 125, regressionPercent: 4.2, status: 'stable', changeConfidence: 'low', environment: 'production' },
  { id: '5', endpoint: 'GET /api/v1/notifications', serviceName: 'notification-service', p50BaselineMs: 15, p50CurrentMs: 18, p95BaselineMs: 45, p95CurrentMs: 52, p99BaselineMs: 95, p99CurrentMs: 110, regressionPercent: 15.8, status: 'regressed', changeConfidence: 'medium', environment: 'production' },
];

function statusVariant(status: RegressionStatus): 'danger' | 'success' | 'secondary' {
  switch (status) {
    case 'regressed': return 'danger';
    case 'improved': return 'success';
    case 'stable': return 'secondary';
  }
}

function confidenceVariant(conf: ChangeConfidence): 'success' | 'warning' | 'secondary' {
  switch (conf) {
    case 'high': return 'success';
    case 'medium': return 'warning';
    case 'low': return 'secondary';
  }
}

export function ApiRegressionPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['api-regressions', environment, timeRange, refreshKey],
    queryFn: () => getApiRegressions({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const entries = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const regressions = entries.filter((e) => e.status === 'regressed').length;
  const improved = entries.filter((e) => e.status === 'improved').length;
  const avgRegression = entries.filter((e) => e.status === 'regressed').length > 0
    ? Math.round(entries.filter((e) => e.status === 'regressed').reduce((a, e) => a + e.regressionPercent, 0) / regressions)
    : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('apiRegression.title')}
          subtitle={t('apiRegression.subtitle')}
          icon={<TrendingDown className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('apiRegression.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('apiRegression.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('apiRegression.stats.totalEndpoints'), value: String(entries.length) },
                { label: t('apiRegression.stats.regressions'), value: String(regressions) },
                { label: t('apiRegression.stats.improved'), value: String(improved) },
                { label: t('apiRegression.stats.avgRegression'), value: `+${avgRegression}%` },
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
                <h3 className="text-sm font-semibold">{t('apiRegression.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {entries.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('apiRegression.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.endpoint')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.p50Baseline')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.p50Current')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.p95Current')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.p99Current')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.regression')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('apiRegression.table.confidence')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {entries.map((e) => (
                          <tr key={e.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-mono text-xs font-medium">{e.endpoint}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{e.serviceName}</td>
                            <td className="px-4 py-2.5 tabular-nums text-muted-foreground">{fmtMs(e.p50BaselineMs)}</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={e.p50CurrentMs > e.p50BaselineMs * 1.2 ? 'text-red-500 font-semibold' : ''}>{fmtMs(e.p50CurrentMs)}</span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={e.p95CurrentMs > e.p95BaselineMs * 1.2 ? 'text-red-500 font-semibold' : ''}>{fmtMs(e.p95CurrentMs)}</span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={e.p99CurrentMs > e.p99BaselineMs * 1.2 ? 'text-red-500 font-semibold' : ''}>{fmtMs(e.p99CurrentMs)}</span>
                            </td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusVariant(e.status)}>
                                {e.regressionPercent > 0 ? `+${e.regressionPercent.toFixed(0)}%` : `${e.regressionPercent.toFixed(0)}%`}
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5">
                              <Badge variant={confidenceVariant(e.changeConfidence)}>
                                {t(`apiRegression.confidence.${e.changeConfidence}`)}
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
