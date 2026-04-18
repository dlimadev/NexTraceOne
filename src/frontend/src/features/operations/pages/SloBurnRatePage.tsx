/**
 * SloBurnRatePage — Taxa de consumo de error budget por SLO com projeção de esgotamento.
 *
 * Exibe burn rate em janelas rápidas (1h/6h) e lentas (24h/72h) com projeção
 * de quando o budget será esgotado, suportando decisões de alerta e rollback.
 *
 * @module operations/reliability
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, Settings } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getSloBurnRates, type SloBurnRate, type SloBurnStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'sloBurnRate.timeRange.1h' },
  { value: '6h', labelKey: 'sloBurnRate.timeRange.6h' },
  { value: '24h', labelKey: 'sloBurnRate.timeRange.24h' },
  { value: '7d', labelKey: 'sloBurnRate.timeRange.7d' },
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

const FALLBACK: SloBurnRate[] = [
  { id: '1', sloName: 'Order API Availability', serviceName: 'order-service', budgetRemainingPercent: 12.4, burnRate1h: 14.2, burnRate6h: 8.9, burnRate24h: 3.2, burnRate72h: 1.8, depletedInHours: 7, alertThreshold: 10, status: 'critical', environment: 'production' },
  { id: '2', sloName: 'Payment Latency p95 < 500ms', serviceName: 'payment-service', budgetRemainingPercent: 38.7, burnRate1h: 4.1, burnRate6h: 3.8, burnRate24h: 2.1, burnRate72h: 1.4, depletedInHours: 62, alertThreshold: 10, status: 'warning', environment: 'production' },
  { id: '3', sloName: 'Catalog Search Availability', serviceName: 'catalog-service', budgetRemainingPercent: 91.2, burnRate1h: 0.8, burnRate6h: 0.9, burnRate24h: 0.7, burnRate72h: 0.6, alertThreshold: 10, status: 'healthy', environment: 'production' },
  { id: '4', sloName: 'Notification Delivery SLO', serviceName: 'notification-service', budgetRemainingPercent: 67.5, burnRate1h: 1.2, burnRate6h: 1.4, burnRate24h: 1.1, burnRate72h: 0.9, alertThreshold: 10, status: 'healthy', environment: 'production' },
  { id: '5', sloName: 'User Auth Error Rate < 0.1%', serviceName: 'auth-service', budgetRemainingPercent: 5.2, burnRate1h: 22.1, burnRate6h: 18.4, burnRate24h: 8.7, burnRate72h: 4.2, depletedInHours: 3, alertThreshold: 5, status: 'critical', environment: 'production' },
];

function statusVariant(status: SloBurnStatus): 'danger' | 'warning' | 'success' {
  switch (status) {
    case 'critical': return 'danger';
    case 'warning': return 'warning';
    case 'healthy': return 'success';
  }
}

function burnRateClass(rate: number): string {
  if (rate > 10) return 'text-red-500 font-bold';
  if (rate > 5) return 'text-amber-500 font-semibold';
  return '';
}

export function SloBurnRatePage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['slo-burn-rates', environment, timeRange, refreshKey],
    queryFn: () => getSloBurnRates({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const slos = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const critical = slos.filter((s) => s.status === 'critical').length;
  const warning = slos.filter((s) => s.status === 'warning').length;
  const healthy = slos.filter((s) => s.status === 'healthy').length;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader title={t('sloBurnRate.title')} subtitle={t('sloBurnRate.subtitle')} />
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

      {isError && <PageErrorState message={t('sloBurnRate.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('sloBurnRate.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('sloBurnRate.stats.totalSlos'), value: String(slos.length) },
                { label: t('sloBurnRate.stats.critical'), value: String(critical) },
                { label: t('sloBurnRate.stats.warning'), value: String(warning) },
                { label: t('sloBurnRate.stats.healthy'), value: String(healthy) },
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
                <h3 className="text-sm font-semibold">{t('sloBurnRate.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {slos.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('sloBurnRate.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.table.slo')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.table.budgetRemaining')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.windows.fast1h')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.windows.fast6h')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.windows.slow24h')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.windows.slow72h')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.table.depleted')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloBurnRate.table.alertThreshold')}</th>
                          <th className="px-4 py-2.5 text-left font-medium"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {slos.map((s) => (
                          <tr key={s.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{s.sloName}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{s.serviceName}</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <div className="flex items-center gap-2">
                                <div className="w-16 h-1.5 rounded-full bg-muted overflow-hidden">
                                  <div className={`h-full rounded-full ${s.budgetRemainingPercent < 20 ? 'bg-red-500' : s.budgetRemainingPercent < 50 ? 'bg-amber-500' : 'bg-emerald-500'}`} style={{ width: `${s.budgetRemainingPercent}%` }} />
                                </div>
                                <span className="font-semibold">{s.budgetRemainingPercent.toFixed(1)}%</span>
                              </div>
                            </td>
                            <td className={`px-4 py-2.5 tabular-nums ${burnRateClass(s.burnRate1h)}`}>{s.burnRate1h.toFixed(1)}x</td>
                            <td className={`px-4 py-2.5 tabular-nums ${burnRateClass(s.burnRate6h)}`}>{s.burnRate6h.toFixed(1)}x</td>
                            <td className={`px-4 py-2.5 tabular-nums ${burnRateClass(s.burnRate24h)}`}>{s.burnRate24h.toFixed(1)}x</td>
                            <td className={`px-4 py-2.5 tabular-nums ${burnRateClass(s.burnRate72h)}`}>{s.burnRate72h.toFixed(1)}x</td>
                            <td className="px-4 py-2.5">
                              {s.depletedInHours ? (
                                <Badge variant={s.depletedInHours < 12 ? 'danger' : 'warning'}>
                                  {t('sloBurnRate.projection.depletedIn', { hours: s.depletedInHours })}
                                </Badge>
                              ) : (
                                <Badge variant="success">{t('sloBurnRate.status.healthy')}</Badge>
                              )}
                            </td>
                            <td className="px-4 py-2.5 tabular-nums text-muted-foreground">{s.alertThreshold}x</td>
                            <td className="px-4 py-2.5">
                              <Button variant="ghost" size="sm">
                                <Settings className="w-3.5 h-3.5" />
                              </Button>
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
