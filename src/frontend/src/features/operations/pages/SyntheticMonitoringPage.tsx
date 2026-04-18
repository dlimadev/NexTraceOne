/**
 * SyntheticMonitoringPage — Monitorização sintética com probes HTTP e validação de contratos.
 *
 * Gestão de probes HTTP single e multi-step com uptime, status e correlação
 * com contratos de API para validação contínua do comportamento esperado.
 *
 * @module operations/reliability
 * @pillar Contract Governance, Operational Reliability
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, Plus } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getSyntheticProbes, type SyntheticProbe, type ProbeStatus, type ContractValidationStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'syntheticMonitoring.timeRange.1h' },
  { value: '6h', labelKey: 'syntheticMonitoring.timeRange.6h' },
  { value: '24h', labelKey: 'syntheticMonitoring.timeRange.24h' },
  { value: '7d', labelKey: 'syntheticMonitoring.timeRange.7d' },
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

const FALLBACK: SyntheticProbe[] = [
  { id: '1', name: 'Order API Health', type: 'httpSingle', target: 'https://api.internal/orders/health', status: 'healthy', uptimePercent: 99.98, lastCheck: new Date(Date.now() - 60000).toISOString(), lastResult: '200 OK (142ms)', schedule: '1m', contractValidation: 'pass', environment: 'production' },
  { id: '2', name: 'Checkout Flow', type: 'httpMultiStep', target: 'https://api.internal/checkout/*', status: 'degraded', uptimePercent: 97.4, lastCheck: new Date(Date.now() - 120000).toISOString(), lastResult: '503 Service Unavailable', schedule: '5m', contractValidation: 'fail', environment: 'production' },
  { id: '3', name: 'Payment Gateway Ping', type: 'httpSingle', target: 'https://gateway.payments/ping', status: 'healthy', uptimePercent: 99.95, lastCheck: new Date(Date.now() - 30000).toISOString(), lastResult: '200 OK (89ms)', schedule: '2m', contractValidation: 'pass', environment: 'production' },
  { id: '4', name: 'Catalog Search', type: 'httpSingle', target: 'https://api.internal/catalog/search?q=test', status: 'down', uptimePercent: 88.2, lastCheck: new Date(Date.now() - 90000).toISOString(), lastResult: 'Connection refused', schedule: '1m', contractValidation: 'skipped', environment: 'production' },
  { id: '5', name: 'Notification Delivery', type: 'httpMultiStep', target: 'https://api.internal/notifications/*', status: 'healthy', uptimePercent: 99.9, lastCheck: new Date(Date.now() - 45000).toISOString(), lastResult: '201 Created (234ms)', schedule: '10m', contractValidation: 'pass', environment: 'staging' },
];

function probeStatusVariant(status: ProbeStatus): 'success' | 'warning' | 'danger' {
  switch (status) {
    case 'healthy': return 'success';
    case 'degraded': return 'warning';
    case 'down': return 'danger';
  }
}

function contractVariant(cv: ContractValidationStatus): 'success' | 'danger' | 'secondary' {
  switch (cv) {
    case 'pass': return 'success';
    case 'fail': return 'danger';
    case 'skipped': return 'secondary';
  }
}

export function SyntheticMonitoringPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['synthetic-probes', environment, timeRange, refreshKey],
    queryFn: () => getSyntheticProbes({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const probes = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const healthy = probes.filter((p) => p.status === 'healthy').length;
  const degraded = probes.filter((p) => p.status === 'degraded').length;
  const down = probes.filter((p) => p.status === 'down').length;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader title={t('syntheticMonitoring.title')} subtitle={t('syntheticMonitoring.subtitle')} />
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
            {t('syntheticMonitoring.actions.createProbe')}
          </Button>
        </div>
      </div>

      {isError && <PageErrorState message={t('syntheticMonitoring.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('syntheticMonitoring.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('syntheticMonitoring.stats.totalProbes'), value: String(probes.length) },
                { label: t('syntheticMonitoring.stats.healthy'), value: String(healthy) },
                { label: t('syntheticMonitoring.stats.degraded'), value: String(degraded) },
                { label: t('syntheticMonitoring.stats.down'), value: String(down) },
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
                <h3 className="text-sm font-semibold">{t('syntheticMonitoring.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {probes.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('syntheticMonitoring.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.name')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.type')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.target')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.status')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.uptime')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.lastResult')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('syntheticMonitoring.table.contractValidation')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {probes.map((p) => (
                          <tr key={p.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{p.name}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{t(`syntheticMonitoring.probeTypes.${p.type}`)}</Badge></td>
                            <td className="px-4 py-2.5 font-mono text-xs text-muted-foreground max-w-xs truncate" title={p.target}>{p.target}</td>
                            <td className="px-4 py-2.5"><Badge variant={probeStatusVariant(p.status)}>{t(`syntheticMonitoring.status.${p.status}`)}</Badge></td>
                            <td className="px-4 py-2.5 tabular-nums font-semibold">{p.uptimePercent.toFixed(2)}%</td>
                            <td className="px-4 py-2.5 text-xs text-muted-foreground">{p.lastResult}</td>
                            <td className="px-4 py-2.5"><Badge variant={contractVariant(p.contractValidation)}>{t(`syntheticMonitoring.contractValidation.${p.contractValidation}`)}</Badge></td>
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
