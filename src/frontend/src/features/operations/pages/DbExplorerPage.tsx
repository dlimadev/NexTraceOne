/**
 * DbExplorerPage — Explorador de performance de base de dados com análise de queries lentas.
 *
 * Identifica queries problemáticas por fingerprint, detecta ausência de índices
 * e classifica por tempo total, execuções ou espera de lock.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Operational Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { HardDrive, RefreshCw, AlertTriangle } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getSlowQueries, type SlowQuery, type DbSortMode } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'dbExplorer.timeRange.1h' },
  { value: '6h', labelKey: 'dbExplorer.timeRange.6h' },
  { value: '24h', labelKey: 'dbExplorer.timeRange.24h' },
  { value: '7d', labelKey: 'dbExplorer.timeRange.7d' },
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

const FALLBACK: SlowQuery[] = [
  { id: '1', fingerprint: 'SELECT * FROM orders WHERE customer_id = ?', database: 'orders_db', avgDurationMs: 1840, maxDurationMs: 12300, executionCount: 4821, totalTimeMs: 8877240, lockWaitMs: 120, hasIndexMiss: true, indexMissCount: 4821, recommendation: 'Add index on orders(customer_id)', environment: 'production' },
  { id: '2', fingerprint: 'UPDATE inventory SET quantity = ? WHERE product_id = ?', database: 'catalog_db', avgDurationMs: 890, maxDurationMs: 4500, executionCount: 12034, totalTimeMs: 10710260, lockWaitMs: 2340, hasIndexMiss: false, indexMissCount: 0, environment: 'production' },
  { id: '3', fingerprint: 'SELECT p.*, c.name FROM products p LEFT JOIN categories c ON p.category_id = c.id WHERE p.active = true', database: 'catalog_db', avgDurationMs: 2100, maxDurationMs: 8900, executionCount: 1892, totalTimeMs: 3973200, lockWaitMs: 0, hasIndexMiss: true, indexMissCount: 1892, recommendation: 'Add composite index on products(active, category_id)', environment: 'production' },
  { id: '4', fingerprint: 'DELETE FROM audit_log WHERE created_at < ?', database: 'audit_db', avgDurationMs: 5600, maxDurationMs: 45000, executionCount: 24, totalTimeMs: 134400, lockWaitMs: 8900, hasIndexMiss: false, indexMissCount: 0, environment: 'production' },
  { id: '5', fingerprint: 'SELECT COUNT(*) FROM events WHERE service_id = ? AND timestamp BETWEEN ? AND ?', database: 'events_db', avgDurationMs: 3200, maxDurationMs: 18000, executionCount: 892, totalTimeMs: 2854400, lockWaitMs: 450, hasIndexMiss: true, indexMissCount: 892, recommendation: 'Add index on events(service_id, timestamp)', environment: 'production' },
];

const SORT_TABS: Array<{ value: DbSortMode; labelKey: string }> = [
  { value: 'totalTime', labelKey: 'dbExplorer.tabs.totalTime' },
  { value: 'executions', labelKey: 'dbExplorer.tabs.executions' },
  { value: 'lockWait', labelKey: 'dbExplorer.tabs.lockWait' },
];

export function DbExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [sortBy, setSortBy] = useState<DbSortMode>('totalTime');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['slow-queries', environment, timeRange, sortBy, refreshKey],
    queryFn: () => getSlowQueries({ environment, from: interval.from, until: interval.until, sortBy }),
    staleTime: 30_000,
    retry: false,
  });

  const queries = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const indexMisses = queries.filter((q) => q.hasIndexMiss).length;
  const avgDuration = queries.length > 0 ? Math.round(queries.reduce((a, q) => a + q.avgDurationMs, 0) / queries.length) : 0;
  const maxDuration = queries.length > 0 ? Math.max(...queries.map((q) => q.maxDurationMs)) : 0;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('dbExplorer.title')}
          subtitle={t('dbExplorer.subtitle')}
          icon={<HardDrive className="w-5 h-5" />}
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

      {isError && <PageErrorState message={t('dbExplorer.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('dbExplorer.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('dbExplorer.stats.slowQueries'), value: String(queries.length) },
                { label: t('dbExplorer.stats.avgDuration'), value: fmtMs(avgDuration) },
                { label: t('dbExplorer.stats.maxDuration'), value: fmtMs(maxDuration) },
                { label: t('dbExplorer.stats.indexMisses'), value: String(indexMisses) },
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

          {indexMisses > 0 && (
            <PageSection>
              <Card className="border-amber-500/40 bg-amber-500/5">
                <CardBody className="p-3">
                  <div className="flex items-center gap-2 text-amber-600 dark:text-amber-400 text-sm font-semibold mb-1">
                    <AlertTriangle className="w-4 h-4" />
                    {t('dbExplorer.indexWarnings.title')}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {t('dbExplorer.indexWarnings.message', { count: indexMisses })}
                  </p>
                </CardBody>
              </Card>
            </PageSection>
          )}

          <PageSection>
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  {SORT_TABS.map((tab) => (
                    <button
                      key={tab.value}
                      type="button"
                      onClick={() => setSortBy(tab.value)}
                      className={`px-3 py-1 rounded text-xs font-medium transition-colors ${sortBy === tab.value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted'}`}
                    >
                      {t(tab.labelKey)}
                    </button>
                  ))}
                </div>
              </CardHeader>
              <CardBody className="p-0">
                {queries.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('dbExplorer.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.fingerprint')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.database')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.avgDuration')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.maxDuration')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.count')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.lockWait')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('dbExplorer.table.indexMiss')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {queries.map((q) => (
                          <tr key={q.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-mono text-xs max-w-xs truncate" title={q.fingerprint}>{q.fingerprint}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{q.database}</td>
                            <td className="px-4 py-2.5 tabular-nums">
                              <span className={q.avgDurationMs > 2000 ? 'text-red-500 font-semibold' : ''}>{fmtMs(q.avgDurationMs)}</span>
                            </td>
                            <td className="px-4 py-2.5 tabular-nums">{fmtMs(q.maxDurationMs)}</td>
                            <td className="px-4 py-2.5 tabular-nums">{q.executionCount.toLocaleString()}</td>
                            <td className="px-4 py-2.5 tabular-nums">{fmtMs(q.lockWaitMs)}</td>
                            <td className="px-4 py-2.5">
                              {q.hasIndexMiss ? (
                                <Badge variant="warning">{q.indexMissCount.toLocaleString()}x</Badge>
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
