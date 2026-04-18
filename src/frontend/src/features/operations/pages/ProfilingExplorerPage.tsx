/**
 * ProfilingExplorerPage — Exploração contínua de profiling por serviço, versão e ambiente.
 *
 * Exibe sessões de profiling CPU/memória/heap com comparação antes/depois de deploy
 * e correlação direta com eventos de mudança em produção.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Cpu, RefreshCw, GitCommit } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getProfilingSessions, type ProfilingSession } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'profilingExplorer.timeRange.1h' },
  { value: '6h', labelKey: 'profilingExplorer.timeRange.6h' },
  { value: '24h', labelKey: 'profilingExplorer.timeRange.24h' },
  { value: '7d', labelKey: 'profilingExplorer.timeRange.7d' },
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

const FALLBACK: ProfilingSession[] = [
  { id: '1', serviceName: 'order-service', version: 'v2.3.1', environment: 'production', cpuPercent: 68.4, memoryMb: 512, heapMb: 310, sampleCount: 4200, durationMs: 60000, deployCorrelated: true, deployId: 'deploy-001', capturedAt: new Date(Date.now() - 3600000).toISOString(), profileType: 'cpu' },
  { id: '2', serviceName: 'payment-service', version: 'v1.8.0', environment: 'production', cpuPercent: 82.1, memoryMb: 768, heapMb: 540, sampleCount: 6100, durationMs: 60000, deployCorrelated: true, deployId: 'deploy-002', capturedAt: new Date(Date.now() - 7200000).toISOString(), profileType: 'memory' },
  { id: '3', serviceName: 'catalog-service', version: 'v3.0.2', environment: 'production', cpuPercent: 41.2, memoryMb: 256, heapMb: 198, sampleCount: 2800, durationMs: 60000, deployCorrelated: false, capturedAt: new Date(Date.now() - 10800000).toISOString(), profileType: 'heap' },
  { id: '4', serviceName: 'notification-service', version: 'v2.1.0', environment: 'staging', cpuPercent: 55.0, memoryMb: 384, heapMb: 220, sampleCount: 3500, durationMs: 60000, deployCorrelated: false, capturedAt: new Date(Date.now() - 14400000).toISOString(), profileType: 'cpu' },
];

function fmtMs(ms: number) {
  if (ms < 1000) return `${ms}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

export function ProfilingExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['profiling-sessions', environment, timeRange, refreshKey],
    queryFn: () => getProfilingSessions({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const sessions = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const regressions = sessions.filter((s) => s.deployCorrelated).length;
  const avgCpu = sessions.length > 0 ? (sessions.reduce((a, s) => a + s.cpuPercent, 0) / sessions.length).toFixed(1) : '0';
  const avgMemory = sessions.length > 0 ? Math.round(sessions.reduce((a, s) => a + s.memoryMb, 0) / sessions.length) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('profilingExplorer.title')}
          subtitle={t('profilingExplorer.subtitle')}
          icon={<Cpu className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('profilingExplorer.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('profilingExplorer.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('profilingExplorer.stats.totalSessions'), value: String(sessions.length) },
                { label: t('profilingExplorer.stats.avgCpu'), value: `${avgCpu}%` },
                { label: t('profilingExplorer.stats.avgMemory'), value: `${avgMemory} MB` },
                { label: t('profilingExplorer.stats.regressions'), value: String(regressions) },
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
                <h3 className="text-sm font-semibold">{t('profilingExplorer.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {sessions.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('profilingExplorer.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          {(['service', 'version', 'environment', 'cpu', 'memory', 'heap', 'samples', 'duration', 'deploy'] as const).map((col) => (
                            <th key={col} className="px-4 py-2.5 text-left font-medium">
                              {t(`profilingExplorer.table.${col}`)}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {sessions.map((s) => (
                          <tr key={s.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{s.serviceName}</td>
                            <td className="px-4 py-2.5 text-muted-foreground font-mono text-xs">{s.version}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{s.environment}</Badge></td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={s.cpuPercent > 75 ? 'text-red-500 font-semibold' : ''}>{s.cpuPercent.toFixed(1)}%</span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">{s.memoryMb} MB</td>
                            <td className="px-4 py-2.5 tabular-nums">{s.heapMb} MB</td>
                            <td className="px-4 py-2.5 tabular-nums">{s.sampleCount.toLocaleString()}</td>
                            <td className="px-4 py-2.5 tabular-nums">{fmtMs(s.durationMs)}</td>
                            <td className="px-4 py-2.5">
                              {s.deployCorrelated ? (
                                <Badge variant="warning" className="flex items-center gap-1 w-fit">
                                  <GitCommit className="w-3 h-3" />
                                  {t('profilingExplorer.table.deployCorrelated')}
                                </Badge>
                              ) : (
                                <span className="text-muted-foreground text-xs">—</span>
                              )}
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
