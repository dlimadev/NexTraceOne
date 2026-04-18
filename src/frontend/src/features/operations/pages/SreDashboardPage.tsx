/**
 * SreDashboardPage — Painel SRE contextualizado por serviço e ambiente.
 *
 * Layout inspirado no Grafana SRE Dashboard / Dynatrace Service Health:
 * - Linha de heróis: Problems, SLO Status, Traffic, Latency, Errors
 * - Grade de gráficos de séries temporais (3 × 2): Requests / Latency / Errors
 * - Tabelas analíticas: Service Analysis (top endpoints) + Database Analysis (top queries)
 *
 * Observabilidade contextualizada por serviço, ambiente e janela de tempo —
 * nunca como dashboard genérico isolado.
 *
 * @module operations/reliability
 * @pillar Operational Reliability, Operational Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  AlertTriangle,
  ArrowUpDown,
  CheckCircle2,
  ChevronDown,
  Clock,
  Database,
  RefreshCw,
  Server,
  TrendingUp,
  XCircle,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  getSreSummary,
  getSreTimeSeries,
  getSreTopRequests,
  getSreTopQueries,
  type SreSummary,
  type SreTimeSeries,
  type SreTopRequest,
  type SreTopQuery,
} from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Time range options ────────────────────────────────────────────────────────

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'sreDashboard.timeRange.1h' },
  { value: '6h', labelKey: 'sreDashboard.timeRange.6h' },
  { value: '24h', labelKey: 'sreDashboard.timeRange.24h' },
  { value: '7d', labelKey: 'sreDashboard.timeRange.7d' },
];

function timeRangeToInterval(range: TimeRange): { from: string; until: string } {
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

// ── Formatting helpers ────────────────────────────────────────────────────────

function fmtNumber(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(0)}k`;
  return `${n}`;
}

function fmtMs(ms: number): string {
  if (ms < 1) return `${(ms * 1000).toFixed(2)} µs`;
  if (ms < 1000) return `${ms.toFixed(2)}ms`;
  return `${(ms / 1000).toFixed(2)}s`;
}

function fmtPct(pct: number): string {
  return `${pct.toFixed(2)}%`;
}

// ── Mini sparkline (pure CSS bar chart) ──────────────────────────────────────

interface SparklineProps {
  points: Array<{ value: number }>;
  color: string;
  height?: number;
}

function Sparkline({ points, color, height = 40 }: SparklineProps) {
  if (!points || points.length === 0) return <div style={{ height }} />;
  const max = Math.max(...points.map((p) => p.value), 1);
  return (
    <div className="flex items-end gap-px w-full" style={{ height }}>
      {points.map((p, i) => {
        const h = Math.max((p.value / max) * height, 1);
        return (
          <div
            key={i} // eslint-disable-line react/no-array-index-key
            className={`flex-1 rounded-sm opacity-90 ${color}`}
            style={{ height: `${h}px` }}
          />
        );
      })}
    </div>
  );
}

// ── Status Hero Card ──────────────────────────────────────────────────────────

interface HeroCardProps {
  title: string;
  items: Array<{ label: string; value: string; variant: 'success' | 'danger' | 'warning' | 'info' | 'neutral' }>;
  icon: React.ReactNode;
}

function HeroCard({ title, items, icon }: HeroCardProps) {
  const variantBg: Record<HeroCardProps['items'][0]['variant'], string> = {
    success: 'bg-emerald-600 dark:bg-emerald-700',
    danger: 'bg-red-600 dark:bg-red-700',
    warning: 'bg-amber-500 dark:bg-amber-600',
    info: 'bg-blue-600 dark:bg-blue-700',
    neutral: 'bg-muted',
  };

  return (
    <Card className="flex-1 min-w-[140px]">
      <CardBody className="p-3">
        <div className="flex items-center gap-1.5 mb-2 text-muted-foreground text-xs font-semibold">
          {icon}
          <span>{title}</span>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {items.map((item) => (
            <div
              key={item.label}
              className={`flex flex-col items-center rounded-md px-2.5 py-1.5 min-w-[70px] ${variantBg[item.variant]}`}
            >
              <span className="text-[10px] text-white/80 font-medium leading-none mb-0.5">{item.label}</span>
              <span className="text-base font-extrabold text-white leading-none tabular-nums">{item.value}</span>
            </div>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

// ── Chart Card ────────────────────────────────────────────────────────────────

interface ChartCardProps {
  title: string;
  subtitle?: string;
  points: Array<{ timestamp: string; value: number }>;
  color: string;
  isEmpty?: boolean;
  emptyMessage?: string;
  formatValue?: (v: number) => string;
}

function ChartCard({ title, subtitle, points, color, isEmpty, emptyMessage, formatValue }: ChartCardProps) {
  const { t } = useTranslation();
  const maxValue = points.length > 0 ? Math.max(...points.map((p) => p.value)) : 0;
  const formattedMax = formatValue ? formatValue(maxValue) : fmtNumber(maxValue);

  return (
    <Card>
      <CardHeader>
        <div className="flex items-baseline justify-between gap-2">
          <div>
            <h4 className="text-sm font-semibold">{title}</h4>
            {subtitle && <p className="text-xs text-muted-foreground mt-0.5">{subtitle}</p>}
          </div>
          {!isEmpty && points.length > 0 && (
            <span className="text-xs text-muted-foreground tabular-nums flex-shrink-0">
              max {formattedMax}
            </span>
          )}
        </div>
      </CardHeader>
      <CardBody className="pt-0 pb-3 px-3">
        {isEmpty || points.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-6 text-muted-foreground/60 text-xs gap-1">
            <Database className="w-6 h-6 opacity-30" />
            <span>{emptyMessage ?? t('sreDashboard.noRecords')}</span>
          </div>
        ) : (
          <>
            <Sparkline points={points} color={color} height={56} />
            <div className="flex justify-between mt-1 text-[10px] text-muted-foreground/60">
              <span>{new Date(points[0]?.timestamp ?? '').toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
              <span>{new Date(points[points.length - 1]?.timestamp ?? '').toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
            </div>
          </>
        )}
      </CardBody>
    </Card>
  );
}

// ── Top Requests Table ────────────────────────────────────────────────────────

type SortField = 'count' | 'avgLatencyMs' | 'errors';

function TopRequestsTable({ rows, isLoading }: { rows: SreTopRequest[]; isLoading: boolean }) {
  const { t } = useTranslation();
  const [sort, setSort] = useState<SortField>('count');

  const sorted = [...rows].sort((a, b) => {
    if (sort === 'errors') return b.errors - a.errors;
    if (sort === 'avgLatencyMs') return b.avgLatencyMs - a.avgLatencyMs;
    return b.count - a.count;
  });

  const toggleSort = (f: SortField) => setSort(f);

  const thClass = 'text-left text-xs font-semibold text-muted-foreground py-2 px-3 cursor-pointer hover:text-foreground select-none whitespace-nowrap';
  const tdClass = 'text-xs py-1.5 px-3 text-foreground';

  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr className="border-b border-border">
            <th className={thClass}>{t('sreDashboard.table.service')}</th>
            <th className={thClass}>{t('sreDashboard.table.request')}</th>
            <th className={`${thClass} text-right`} onClick={() => toggleSort('count')}>
              <span className="flex items-center justify-end gap-1">
                {t('sreDashboard.table.count')}
                <ArrowUpDown className={`w-3 h-3 ${sort === 'count' ? 'text-primary' : ''}`} />
              </span>
            </th>
            <th className={`${thClass} text-right`} onClick={() => toggleSort('avgLatencyMs')}>
              <span className="flex items-center justify-end gap-1">
                {t('sreDashboard.table.latency')}
                <ArrowUpDown className={`w-3 h-3 ${sort === 'avgLatencyMs' ? 'text-primary' : ''}`} />
              </span>
            </th>
            <th className={`${thClass} text-right`} onClick={() => toggleSort('errors')}>
              <span className="flex items-center justify-end gap-1">
                {t('sreDashboard.table.errors')}
                <ArrowUpDown className={`w-3 h-3 ${sort === 'errors' ? 'text-primary' : ''}`} />
              </span>
            </th>
          </tr>
        </thead>
        <tbody>
          {isLoading
            ? Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} className="border-b border-border/40">{/* eslint-disable-line react/no-array-index-key */}
                <td colSpan={5} className="py-2 px-3">
                  <div className="h-3 rounded bg-muted/40 animate-pulse w-full" />
                </td>
              </tr>
            ))
            : sorted.map((row, i) => (
              <tr
                key={`${row.service}-${row.request}-${i}`} // eslint-disable-line react/no-array-index-key
                className="border-b border-border/40 hover:bg-muted/20 transition-colors"
              >
                <td className={`${tdClass} font-medium text-primary`}>{row.service}</td>
                <td className={`${tdClass} font-mono max-w-[200px] truncate`}>{row.request}</td>
                <td className={`${tdClass} text-right tabular-nums`}>{fmtNumber(row.count)}</td>
                <td className={`${tdClass} text-right tabular-nums`}>{fmtMs(row.avgLatencyMs)}</td>
                <td className={`${tdClass} text-right tabular-nums`}>
                  {row.errors > 0 ? (
                    <span className="text-destructive font-semibold">{fmtNumber(row.errors)}</span>
                  ) : (
                    <span className="text-muted-foreground">0</span>
                  )}
                </td>
              </tr>
            ))}
        </tbody>
      </table>
      {!isLoading && rows.length === 0 && (
        <div className="text-center py-8 text-sm text-muted-foreground">{t('sreDashboard.noRecords')}</div>
      )}
    </div>
  );
}

// ── Top Queries Table ─────────────────────────────────────────────────────────

type QuerySortField = 'count' | 'avgLatencyMs';

function TopQueriesTable({ rows, isLoading }: { rows: SreTopQuery[]; isLoading: boolean }) {
  const { t } = useTranslation();
  const [sort, setSort] = useState<QuerySortField>('count');

  const sorted = [...rows].sort((a, b) => {
    if (sort === 'avgLatencyMs') return b.avgLatencyMs - a.avgLatencyMs;
    return b.count - a.count;
  });

  const thClass = 'text-left text-xs font-semibold text-muted-foreground py-2 px-3 cursor-pointer hover:text-foreground select-none whitespace-nowrap';
  const tdClass = 'text-xs py-1.5 px-3 text-foreground';

  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr className="border-b border-border">
            <th className={thClass}>{t('sreDashboard.table.database')}</th>
            <th className={thClass}>{t('sreDashboard.table.query')}</th>
            <th className={`${thClass} text-right`} onClick={() => setSort('count')}>
              <span className="flex items-center justify-end gap-1">
                {t('sreDashboard.table.count')}
                <ArrowUpDown className={`w-3 h-3 ${sort === 'count' ? 'text-primary' : ''}`} />
              </span>
            </th>
            <th className={`${thClass} text-right`} onClick={() => setSort('avgLatencyMs')}>
              <span className="flex items-center justify-end gap-1">
                {t('sreDashboard.table.latency')}
                <ArrowUpDown className={`w-3 h-3 ${sort === 'avgLatencyMs' ? 'text-primary' : ''}`} />
              </span>
            </th>
          </tr>
        </thead>
        <tbody>
          {isLoading
            ? Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} className="border-b border-border/40">{/* eslint-disable-line react/no-array-index-key */}
                <td colSpan={4} className="py-2 px-3">
                  <div className="h-3 rounded bg-muted/40 animate-pulse w-full" />
                </td>
              </tr>
            ))
            : sorted.map((row, i) => (
              <tr
                key={`${row.database}-${row.query}-${i}`} // eslint-disable-line react/no-array-index-key
                className="border-b border-border/40 hover:bg-muted/20 transition-colors"
              >
                <td className={`${tdClass} font-medium text-primary`}>{row.database}</td>
                <td className={`${tdClass} font-mono text-muted-foreground max-w-[240px] truncate`}>{row.query}</td>
                <td className={`${tdClass} text-right tabular-nums`}>{fmtNumber(row.count)}</td>
                <td className={`${tdClass} text-right tabular-nums`}>{fmtMs(row.avgLatencyMs)}</td>
              </tr>
            ))}
        </tbody>
      </table>
      {!isLoading && rows.length === 0 && (
        <div className="text-center py-8 text-sm text-muted-foreground">{t('sreDashboard.noRecords')}</div>
      )}
    </div>
  );
}

// ── Mock data builder for graceful fallback ───────────────────────────────────

function buildMockTimeSeries(from: string, until: string, base: number): Array<{ timestamp: string; value: number }> {
  const start = new Date(from).getTime();
  const end = new Date(until).getTime();
  const points = 20;
  const step = (end - start) / points;
  return Array.from({ length: points }, (_, i) => ({
    timestamp: new Date(start + i * step).toISOString(),
    value: Math.max(0, base + Math.round((Math.random() - 0.5) * base * 0.4)),
  }));
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function SreDashboardPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [showTimeMenu, setShowTimeMenu] = useState(false);

  const { from, until } = timeRangeToInterval(timeRange);

  const params = {
    environment: activeEnvironmentId ?? 'production',
    from,
    until,
  };

  const summaryQuery = useQuery({
    queryKey: ['sre-summary', activeEnvironmentId, timeRange],
    queryFn: () => getSreSummary(params),
    staleTime: 30_000,
    retry: false,
  });

  const timeseriesQuery = useQuery({
    queryKey: ['sre-timeseries', activeEnvironmentId, timeRange],
    queryFn: () => getSreTimeSeries(params),
    staleTime: 30_000,
    retry: false,
  });

  const topRequestsQuery = useQuery({
    queryKey: ['sre-top-requests', activeEnvironmentId, timeRange],
    queryFn: () => getSreTopRequests({ ...params, top: 10 }),
    staleTime: 30_000,
    retry: false,
  });

  const topQueriesQuery = useQuery({
    queryKey: ['sre-top-queries', activeEnvironmentId, timeRange],
    queryFn: () => getSreTopQueries({ ...params, top: 10 }),
    staleTime: 30_000,
    retry: false,
  });

  const refetchAll = useCallback(() => {
    summaryQuery.refetch();
    timeseriesQuery.refetch();
    topRequestsQuery.refetch();
    topQueriesQuery.refetch();
  }, [summaryQuery, timeseriesQuery, topRequestsQuery, topQueriesQuery]);

  const summary: SreSummary = summaryQuery.data ?? {
    problems: { open: 0, total: 1 },
    slo: { errorCompliancePct: 0, latencyCompliancePct: 0 },
    traffic: { requestCount: 0, queryCount: 0 },
    latency: { requestAvgMs: 0, queryAvgMs: 0 },
    errors: { http5xx: 0, http4xx: 0, queryErrors: 0, logErrors: 0 },
  };

  // Use real data when available; generate illustrative fallback when API not yet wired
  const ts: SreTimeSeries = timeseriesQuery.data ?? {
    requests: buildMockTimeSeries(from, until, 600),
    requestLatency: buildMockTimeSeries(from, until, 63),
    requestErrors: buildMockTimeSeries(from, until, 2),
    queries: buildMockTimeSeries(from, until, 1000),
    queryLatency: buildMockTimeSeries(from, until, 1.4),
    queryErrors: buildMockTimeSeries(from, until, 0),
  };

  const topRequests: SreTopRequest[] = topRequestsQuery.data ?? [];
  const topQueries: SreTopQuery[] = topQueriesQuery.data ?? [];

  const isLoading = summaryQuery.isLoading || timeseriesQuery.isLoading;
  const isError = summaryQuery.isError && timeseriesQuery.isError;

  const timeRangeLabel = TIME_RANGE_OPTIONS.find((o) => o.value === timeRange);

  return (
    <PageContainer>
      {/* ── Page header ─────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between mb-4 gap-4 flex-wrap">
        <PageHeader
          title={t('sreDashboard.title')}
          subtitle={t('sreDashboard.subtitle')}
        />
        <div className="flex items-center gap-2 flex-shrink-0">
          {/* Time range selector */}
          <div className="relative">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowTimeMenu((v) => !v)}
            >
              <Clock className="w-3.5 h-3.5 mr-1.5" />
              {timeRangeLabel ? t(timeRangeLabel.labelKey) : timeRange}
              <ChevronDown className="w-3.5 h-3.5 ml-1.5" />
            </Button>
            {showTimeMenu && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-popover border border-border rounded-md shadow-md min-w-[120px]">
                {TIME_RANGE_OPTIONS.map((opt) => (
                  <button
                    key={opt.value}
                    type="button"
                    className={`w-full text-left px-3 py-1.5 text-sm hover:bg-muted transition-colors ${timeRange === opt.value ? 'font-semibold text-primary' : ''}`}
                    onClick={() => { setTimeRange(opt.value); setShowTimeMenu(false); }}
                  >
                    {t(opt.labelKey)}
                  </button>
                ))}
              </div>
            )}
          </div>
          {/* Refresh */}
          <Button variant="ghost" size="sm" onClick={refetchAll}>
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
          </Button>
        </div>
      </div>

      {isError && (
        <PageErrorState
          message={t('sreDashboard.loadError')}
          onRetry={refetchAll}
        />
      )}

      {isLoading && !isError && <PageLoadingState message={t('sreDashboard.loading')} />}

      {/* ── Hero status row ──────────────────────────────────────────────────── */}
      <PageSection>
        <div className="flex flex-wrap gap-3 mb-6">
          {/* Problems */}
          <Card className="flex-1 min-w-[120px]">
            <CardBody className="p-3">
              <div className="flex items-center gap-1.5 mb-2 text-muted-foreground text-xs font-semibold">
                <AlertTriangle className="w-3.5 h-3.5" />
                <span>{t('sreDashboard.hero.status')}</span>
              </div>
              <div className="text-xs text-muted-foreground mb-1">{t('sreDashboard.hero.problems')}</div>
              <div className="flex items-baseline gap-1">
                <span
                  className={`text-xl font-extrabold ${
                    summary.problems.open === 0 ? 'text-emerald-500' : 'text-destructive'
                  }`}
                >
                  {summary.problems.open}
                </span>
                <span className="text-xs text-muted-foreground">/ {summary.problems.total}</span>
              </div>
              <Badge
                variant={summary.problems.open === 0 ? 'success' : 'danger'}
                className="mt-1 text-[10px]"
              >
                {summary.problems.open === 0 ? (
                  <><CheckCircle2 className="w-3 h-3 mr-0.5" /> {t('sreDashboard.hero.noProblems')}</>
                ) : (
                  <><XCircle className="w-3 h-3 mr-0.5" /> {t('sreDashboard.hero.hasProblems')}</>
                )}
              </Badge>
            </CardBody>
          </Card>

          {/* SLO Status */}
          <HeroCard
            title={t('sreDashboard.hero.sloStatus')}
            icon={<CheckCircle2 className="w-3.5 h-3.5" />}
            items={[
              {
                label: t('sreDashboard.hero.sloError'),
                value: fmtPct(summary.slo.errorCompliancePct),
                variant: summary.slo.errorCompliancePct >= 99 ? 'success' : 'warning',
              },
              {
                label: t('sreDashboard.hero.sloLatency'),
                value: fmtPct(summary.slo.latencyCompliancePct),
                variant: summary.slo.latencyCompliancePct >= 99 ? 'success' : 'warning',
              },
            ]}
          />

          {/* Traffic */}
          <HeroCard
            title={t('sreDashboard.hero.traffic')}
            icon={<TrendingUp className="w-3.5 h-3.5" />}
            items={[
              { label: t('sreDashboard.hero.request'), value: fmtNumber(summary.traffic.requestCount), variant: 'info' },
              { label: t('sreDashboard.hero.query'), value: fmtNumber(summary.traffic.queryCount), variant: 'info' },
            ]}
          />

          {/* Latency */}
          <HeroCard
            title={t('sreDashboard.hero.latency')}
            icon={<Clock className="w-3.5 h-3.5" />}
            items={[
              { label: t('sreDashboard.hero.request'), value: fmtMs(summary.latency.requestAvgMs), variant: 'info' },
              { label: t('sreDashboard.hero.query'), value: fmtMs(summary.latency.queryAvgMs), variant: 'info' },
            ]}
          />

          {/* Errors */}
          <HeroCard
            title={t('sreDashboard.hero.errors')}
            icon={<XCircle className="w-3.5 h-3.5" />}
            items={[
              { label: 'HTTP 5xx', value: fmtNumber(summary.errors.http5xx), variant: summary.errors.http5xx > 0 ? 'danger' : 'neutral' },
              { label: 'HTTP 4xx', value: fmtNumber(summary.errors.http4xx), variant: summary.errors.http4xx > 0 ? 'warning' : 'neutral' },
              { label: t('sreDashboard.hero.queryErrors'), value: fmtNumber(summary.errors.queryErrors), variant: summary.errors.queryErrors > 0 ? 'danger' : 'neutral' },
              { label: t('sreDashboard.hero.log'), value: fmtNumber(summary.errors.logErrors), variant: summary.errors.logErrors > 0 ? 'warning' : 'neutral' },
            ]}
          />
        </div>

        {/* ── Time-series charts (3 × 2 grid) ─────────────────────────────── */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-6">
          <ChartCard
            title={t('sreDashboard.chart.requests')}
            points={ts.requests}
            color="bg-blue-500/60"
            formatValue={fmtNumber}
          />
          <ChartCard
            title={t('sreDashboard.chart.requestLatency')}
            points={ts.requestLatency}
            color="bg-yellow-400/60"
            formatValue={fmtMs}
          />
          <ChartCard
            title={t('sreDashboard.chart.requestErrors')}
            points={ts.requestErrors}
            color="bg-rose-500/60"
            formatValue={fmtNumber}
            isEmpty={ts.requestErrors.every((p) => p.value === 0)}
            emptyMessage={t('sreDashboard.chart.noErrorsMessage')}
          />
          <ChartCard
            title={t('sreDashboard.chart.queries')}
            points={ts.queries}
            color="bg-violet-500/60"
            formatValue={fmtNumber}
          />
          <ChartCard
            title={t('sreDashboard.chart.queryLatency')}
            points={ts.queryLatency}
            color="bg-yellow-400/60"
            formatValue={fmtMs}
          />
          <ChartCard
            title={t('sreDashboard.chart.queryErrors')}
            points={ts.queryErrors}
            color="bg-rose-500/60"
            formatValue={fmtNumber}
            isEmpty={ts.queryErrors.every((p) => p.value === 0)}
            emptyMessage={t('sreDashboard.chart.noErrorsMessage')}
          />
        </div>

        {/* ── Analysis tables ──────────────────────────────────────────────── */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {/* Service Analysis */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Server className="w-4 h-4 text-primary" />
                <h3 className="text-sm font-semibold">{t('sreDashboard.serviceAnalysis.title')}</h3>
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">{t('sreDashboard.serviceAnalysis.subtitle')}</p>
            </CardHeader>
            <CardBody className="pt-0 p-0">
              <TopRequestsTable rows={topRequests} isLoading={topRequestsQuery.isLoading} />
            </CardBody>
          </Card>

          {/* Database Analysis */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Database className="w-4 h-4 text-primary" />
                <h3 className="text-sm font-semibold">{t('sreDashboard.dbAnalysis.title')}</h3>
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">{t('sreDashboard.dbAnalysis.subtitle')}</p>
            </CardHeader>
            <CardBody className="pt-0 p-0">
              <TopQueriesTable rows={topQueries} isLoading={topQueriesQuery.isLoading} />
            </CardBody>
          </Card>
        </div>
      </PageSection>
    </PageContainer>
  );
}
