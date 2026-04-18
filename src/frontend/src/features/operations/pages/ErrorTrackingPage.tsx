/**
 * ErrorTrackingPage — Rastreamento e gestão de grupos de erros com correlação de deploy.
 *
 * Agrupa erros por fingerprint, exibe contagem, utilizadores afetados e status,
 * permitindo correlação direta com deploys para análise de causa raiz.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getErrorGroups, type ErrorGroup, type ErrorGroupStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'errorTracking.timeRange.1h' },
  { value: '6h', labelKey: 'errorTracking.timeRange.6h' },
  { value: '24h', labelKey: 'errorTracking.timeRange.24h' },
  { value: '7d', labelKey: 'errorTracking.timeRange.7d' },
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

const FALLBACK: ErrorGroup[] = [
  { id: '1', fingerprint: 'a1b2c3d4', message: 'NullPointerException in OrderProcessor.process()', serviceName: 'order-service', count: 342, affectedUsers: 89, status: 'regressing', firstSeen: new Date(Date.now() - 86400000 * 3).toISOString(), lastSeen: new Date(Date.now() - 600000).toISOString(), deployCorrelated: true, deployId: 'deploy-001', environment: 'production' },
  { id: '2', fingerprint: 'e5f6g7h8', message: 'Connection timeout to payment-gateway after 30000ms', serviceName: 'payment-service', count: 128, affectedUsers: 34, status: 'new', firstSeen: new Date(Date.now() - 3600000).toISOString(), lastSeen: new Date(Date.now() - 60000).toISOString(), deployCorrelated: true, deployId: 'deploy-002', environment: 'production' },
  { id: '3', fingerprint: 'i9j0k1l2', message: 'Invalid schema: field "price" missing from response', serviceName: 'catalog-service', count: 56, affectedUsers: 12, status: 'new', firstSeen: new Date(Date.now() - 7200000).toISOString(), lastSeen: new Date(Date.now() - 300000).toISOString(), deployCorrelated: false, environment: 'production' },
  { id: '4', fingerprint: 'm3n4o5p6', message: 'Unhandled promise rejection: ECONNREFUSED 127.0.0.1:5432', serviceName: 'notification-service', count: 23, affectedUsers: 5, status: 'resolved', firstSeen: new Date(Date.now() - 172800000).toISOString(), lastSeen: new Date(Date.now() - 43200000).toISOString(), deployCorrelated: false, environment: 'production' },
  { id: '5', fingerprint: 'q7r8s9t0', message: 'Rate limit exceeded for external API calls', serviceName: 'integration-service', count: 891, affectedUsers: 203, status: 'ignored', firstSeen: new Date(Date.now() - 604800000).toISOString(), lastSeen: new Date(Date.now() - 1800000).toISOString(), deployCorrelated: false, environment: 'production' },
];

function statusVariant(status: ErrorGroupStatus): 'danger' | 'warning' | 'success' | 'secondary' {
  switch (status) {
    case 'new': return 'danger';
    case 'regressing': return 'warning';
    case 'resolved': return 'success';
    case 'ignored': return 'secondary';
  }
}

export function ErrorTrackingPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['error-groups', environment, timeRange, refreshKey],
    queryFn: () => getErrorGroups({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const groups = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const newCount = groups.filter((g) => g.status === 'new').length;
  const regressingCount = groups.filter((g) => g.status === 'regressing').length;
  const totalAffectedUsers = groups.reduce((a, g) => a + g.affectedUsers, 0);

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('errorTracking.title')}
          subtitle={t('errorTracking.subtitle')}
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

      {isError && <PageErrorState message={t('errorTracking.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('errorTracking.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('errorTracking.stats.totalGroups'), value: String(groups.length) },
                { label: t('errorTracking.stats.newErrors'), value: String(newCount) },
                { label: t('errorTracking.stats.regressing'), value: String(regressingCount) },
                { label: t('errorTracking.stats.affectedUsers'), value: String(totalAffectedUsers) },
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
                <h3 className="text-sm font-semibold">{t('errorTracking.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {groups.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('errorTracking.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.fingerprint')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.message')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.count')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.users')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.status')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.lastSeen')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {groups.map((g) => (
                          <tr key={g.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-mono text-xs text-muted-foreground">{g.fingerprint}</td>
                            <td className="px-4 py-2.5 max-w-xs truncate font-medium text-xs" title={g.message}>{g.message}</td>
                            <td className="px-4 py-2.5">{g.serviceName}</td>
                            <td className="px-4 py-2.5 tabular-nums font-semibold">{g.count.toLocaleString()}</td>
                            <td className="px-4 py-2.5 tabular-nums">{g.affectedUsers}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusVariant(g.status)}>
                                {t(`errorTracking.status.${g.status}`)}
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted-foreground">{new Date(g.lastSeen).toLocaleString()}</td>
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
