/**
 * RequestExplorerPage — Explorador de requests/spans com filtros facetados.
 *
 * Layout inspirado no Dynatrace Distributed Tracing → Request Explorer:
 * - Painel esquerdo: facetas colapsáveis (Core/Duration, Service, Endpoint,
 *   Request Status, Span Status, Span Kind, HTTP)
 * - Barra de filtros activos (chips com X para remover)
 * - Toggle Requests / Spans no topo
 * - Chart: AreaChart de throughput vs. erros ao longo do tempo (SRE timeseries)
 * - Modo Requests: tabela agregada por endpoint (Total requests, Error rate, Avg RT)
 * - Modo Spans: tabela paginada e ordenável de spans individuais
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Operational Intelligence
 */
import * as React from 'react';
import { useState, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import {
  ArrowUpDown,
  ChevronDown,
  ChevronRight,
  Clock,
  Filter,
  RefreshCw,
  Search,
  X,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  getRequests,
  getRequestFacets,
  getSreTimeSeries,
  getSreTopRequests,
  type RequestQueryParams,
  type RequestSpan,
  type RequestStatus,
  type SpanStatusFilter,
  type SpanKindFilter,
  type RequestViewMode,
  type SreTopRequest,
} from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Time range ────────────────────────────────────────────────────────────────

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'requestExplorer.timeRange.1h' },
  { value: '6h', labelKey: 'requestExplorer.timeRange.6h' },
  { value: '24h', labelKey: 'requestExplorer.timeRange.24h' },
  { value: '7d', labelKey: 'requestExplorer.timeRange.7d' },
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

// ── Helpers ───────────────────────────────────────────────────────────────────

function fmtDuration(ms: number): string {
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)} s`;
  return `${ms.toFixed(2)} ms`;
}

function fmtDateTime(iso: string): string {
  try {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getDate()} ${d.toLocaleString('en', { month: 'short' })}, ${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}.${String(d.getMilliseconds()).padStart(3, '0')}`;
  } catch {
    return iso;
  }
}

// ── Facet panel ───────────────────────────────────────────────────────────────

interface CollapsibleFacetProps {
  title: string;
  children: React.ReactNode;
  defaultOpen?: boolean;
}

function CollapsibleFacet({ title, children, defaultOpen = true }: CollapsibleFacetProps) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border-b border-divider pb-3 mb-2 last:border-b-0 last:pb-0 last:mb-0">
      <button
        className="flex items-center w-full gap-1 py-1 text-xs font-semibold text-muted hover:text-body transition-colors"
        onClick={() => setOpen(o => !o)}
      >
        {open
          ? <ChevronDown size={13} className="flex-shrink-0" />
          : <ChevronRight size={13} className="flex-shrink-0" />}
        {title}
      </button>
      {open && <div className="mt-2 pl-1">{children}</div>}
    </div>
  );
}

// ── Timeseries area chart ─────────────────────────────────────────────────────

interface TimeseriesPoint {
  time: string;
  success: number;
  errors: number;
}

interface TimeseriesAreaChartProps {
  data: TimeseriesPoint[];
}

function TimeseriesAreaChart({ data }: TimeseriesAreaChartProps) {
  if (!data.length) {
    return (
      <div className="flex items-center justify-center h-28 text-sm text-muted">
        No timeseries data
      </div>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={120}>
      <AreaChart data={data} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
        <defs>
          <linearGradient id="reqGradSuccess" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--t-accent)" stopOpacity={0.35} />
            <stop offset="95%" stopColor="var(--t-accent)" stopOpacity={0.03} />
          </linearGradient>
          <linearGradient id="reqGradError" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--t-danger)" stopOpacity={0.65} />
            <stop offset="95%" stopColor="var(--t-danger)" stopOpacity={0.08} />
          </linearGradient>
        </defs>
        <CartesianGrid
          strokeDasharray="3 3"
          stroke="var(--t-divider)"
          vertical={false}
        />
        <XAxis
          dataKey="time"
          tick={{ fontSize: 10, fill: 'var(--t-muted)' }}
          axisLine={false}
          tickLine={false}
          interval="preserveStartEnd"
        />
        <YAxis
          tick={{ fontSize: 10, fill: 'var(--t-muted)' }}
          axisLine={false}
          tickLine={false}
          width={32}
        />
        <Tooltip
          contentStyle={{
            background: 'var(--t-panel)',
            border: '1px solid var(--t-divider)',
            borderRadius: '4px',
            fontSize: '12px',
            color: 'var(--t-body)',
          }}
          labelStyle={{ color: 'var(--t-muted)', marginBottom: '4px' }}
        />
        <Area
          type="monotone"
          dataKey="success"
          stackId="1"
          stroke="var(--t-accent)"
          fill="url(#reqGradSuccess)"
          strokeWidth={1.5}
          name="Success"
          dot={false}
          activeDot={{ r: 3, stroke: 'var(--t-accent)' }}
        />
        <Area
          type="monotone"
          dataKey="errors"
          stackId="1"
          stroke="var(--t-danger)"
          fill="url(#reqGradError)"
          strokeWidth={1.5}
          name="Errors"
          dot={false}
          activeDot={{ r: 3, stroke: 'var(--t-danger)' }}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function RequestExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  // ── State ──────────────────────────────────────────────────────────────────
  const [viewMode, setViewMode] = useState<RequestViewMode>('requests');
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [timeRangeOpen, setTimeRangeOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  // Filter state
  const [durationMin, setDurationMin] = useState<string>('');
  const [durationMax, setDurationMax] = useState<string>('');
  const [selectedServices, setSelectedServices] = useState<string[]>([]);
  const [selectedEndpoints, setSelectedEndpoints] = useState<string[]>([]);
  const [requestStatus, setRequestStatus] = useState<RequestStatus | ''>('');
  const [spanStatus, setSpanStatus] = useState<SpanStatusFilter | ''>('');
  const [selectedSpanKinds, setSelectedSpanKinds] = useState<SpanKindFilter[]>([]);

  // Table state (spans mode)
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState<string>('startTime');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');

  const PAGE_SIZE = 20;

  const { from, until } = useMemo(() => timeRangeToInterval(timeRange), [timeRange]);
  const environment = activeEnvironmentId ?? 'production';

  // ── Queries ────────────────────────────────────────────────────────────────

  const queryParams: RequestQueryParams = {
    environment,
    from,
    until,
    viewMode,
    service: selectedServices[0],
    endpoint: selectedEndpoints[0],
    requestStatus: requestStatus || undefined,
    spanStatus: spanStatus || undefined,
    spanKind: selectedSpanKinds[0],
    durationMin: durationMin ? Number(durationMin) : undefined,
    durationMax: durationMax ? Number(durationMax) : undefined,
    page,
    pageSize: PAGE_SIZE,
    sortBy,
    sortDir,
  };

  // Individual spans — only needed in spans mode
  const requestsQuery = useQuery({
    queryKey: ['requests', queryParams, refreshKey],
    queryFn: () => getRequests(queryParams),
    enabled: !!environment && viewMode === 'spans',
  });

  // SRE timeseries — used for the area chart (both modes)
  const sreTimeSeriesQuery = useQuery({
    queryKey: ['sre-timeseries', environment, from, until, refreshKey],
    queryFn: () => getSreTimeSeries({ environment, from, until }),
    enabled: !!environment,
  });

  // SRE top requests (aggregated per endpoint) — used in requests mode
  const sreTopRequestsQuery = useQuery({
    queryKey: ['sre-top-requests', environment, from, until, selectedServices[0], refreshKey],
    queryFn: () =>
      getSreTopRequests({
        environment,
        from,
        until,
        serviceId: selectedServices[0],
        top: 50,
      }),
    enabled: !!environment && viewMode === 'requests',
  });

  const facetsQuery = useQuery({
    queryKey: ['request-facets', environment, from, until, refreshKey],
    queryFn: () => getRequestFacets(environment, from, until),
    enabled: !!environment,
  });

  const facets = facetsQuery.data;
  const spansResult = requestsQuery.data;

  // ── Chart data (SRE timeseries → area chart) ───────────────────────────────

  const chartData = useMemo((): TimeseriesPoint[] => {
    const ts = sreTimeSeriesQuery.data;
    if (!ts?.requests?.length) return [];
    return ts.requests.map((pt, i) => {
      const errVal = ts.requestErrors[i]?.value ?? 0;
      return {
        time: new Date(pt.timestamp).toLocaleTimeString([], {
          hour: '2-digit',
          minute: '2-digit',
        }),
        success: Math.max(0, pt.value - errVal),
        errors: errVal,
      };
    });
  }, [sreTimeSeriesQuery.data]);

  // ── Aggregated top requests (for requests mode table) ─────────────────────

  const topRequests = useMemo((): SreTopRequest[] => {
    const data = sreTopRequestsQuery.data ?? [];
    if (!selectedEndpoints.length) return data;
    const q = selectedEndpoints[0].toLowerCase();
    return data.filter(r => r.request.toLowerCase().includes(q));
  }, [sreTopRequestsQuery.data, selectedEndpoints]);

  // ── Active filter chips ────────────────────────────────────────────────────

  interface FilterChip {
    key: string;
    label: string;
    onRemove: () => void;
  }

  const activeChips: FilterChip[] = useMemo(() => {
    const chips: FilterChip[] = [];
    selectedServices.forEach(s =>
      chips.push({ key: `svc:${s}`, label: `${t('requestExplorer.facets.service')}: ${s}`, onRemove: () => setSelectedServices(prev => prev.filter(x => x !== s)) }),
    );
    selectedEndpoints.forEach(e =>
      chips.push({ key: `ep:${e}`, label: `${t('requestExplorer.facets.endpoint')}: ${e}`, onRemove: () => setSelectedEndpoints(prev => prev.filter(x => x !== e)) }),
    );
    if (requestStatus) chips.push({ key: 'reqStatus', label: `${t('requestExplorer.facets.requestStatus')}: ${requestStatus}`, onRemove: () => setRequestStatus('') });
    if (spanStatus) chips.push({ key: 'spanStatus', label: `${t('requestExplorer.facets.spanStatus')}: ${spanStatus}`, onRemove: () => setSpanStatus('') });
    selectedSpanKinds.forEach(k =>
      chips.push({ key: `kind:${k}`, label: `${t('requestExplorer.facets.spanKind')}: ${k}`, onRemove: () => setSelectedSpanKinds(prev => prev.filter(x => x !== k)) }),
    );
    if (durationMin) chips.push({ key: 'durMin', label: `${t('requestExplorer.facets.durationMin')}: ${durationMin}ms`, onRemove: () => setDurationMin('') });
    if (durationMax) chips.push({ key: 'durMax', label: `${t('requestExplorer.facets.durationMax')}: ${durationMax}ms`, onRemove: () => setDurationMax('') });
    return chips;
  }, [selectedServices, selectedEndpoints, requestStatus, spanStatus, selectedSpanKinds, durationMin, durationMax, t]);

  // ── Sort handler ───────────────────────────────────────────────────────────

  const handleSort = useCallback((col: string) => {
    setSortBy(prev => {
      if (prev === col) setSortDir(d => (d === 'asc' ? 'desc' : 'asc'));
      else setSortDir('desc');
      return col;
    });
    setPage(1);
  }, []);

  // ── Helpers for checkboxes ─────────────────────────────────────────────────

  function toggleListItem<T extends string>(
    list: T[],
    setList: React.Dispatch<React.SetStateAction<T[]>>,
    item: T,
  ) {
    setList(prev => (prev.includes(item) ? prev.filter(x => x !== item) : [...prev, item]));
    setPage(1);
  }

  // ── Time range label ───────────────────────────────────────────────────────

  const currentTimeRangeLabel = t(TIME_RANGE_OPTIONS.find(o => o.value === timeRange)?.labelKey ?? '');

  // ── Span kind options ──────────────────────────────────────────────────────

  const SPAN_KINDS: SpanKindFilter[] = ['client', 'server', 'consumer', 'producer', 'internal', 'link'];

  // ── Shared input / control classes ────────────────────────────────────────

  const inputCls = 'px-2 py-1.5 text-xs rounded-sm border border-edge bg-elevated text-body placeholder-faded focus:outline-none focus:ring-1 focus:ring-accent';

  // ── Total counts for chart title badge ────────────────────────────────────

  const totalCount = viewMode === 'requests'
    ? topRequests.reduce((sum, r) => sum + r.count, 0)
    : spansResult?.total ?? 0;

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <PageContainer>
      <PageHeader
        title={t('requestExplorer.title')}
        subtitle={t('requestExplorer.subtitle')}
      />

      <PageSection>
        <div className="flex flex-col gap-3">
          {/* ── Toolbar: view toggle + time range + refresh ── */}
          <div className="flex items-center gap-3 flex-wrap">
            {/* View toggle */}
            <div className="flex rounded-sm border border-edge overflow-hidden text-sm">
              {(['requests', 'spans'] as RequestViewMode[]).map(mode => (
                <button
                  key={mode}
                  onClick={() => { setViewMode(mode); setPage(1); }}
                  className={`px-4 py-1.5 text-xs font-medium capitalize transition-colors ${
                    viewMode === mode
                      ? 'bg-accent text-on-accent'
                      : 'bg-elevated text-muted hover:bg-hover hover:text-body'
                  }`}
                >
                  {mode === 'requests' ? t('requestExplorer.viewRequests') : t('requestExplorer.viewSpans')}
                </button>
              ))}
            </div>

            {/* Time range picker */}
            <div className="relative">
              <button
                onClick={() => setTimeRangeOpen(o => !o)}
                className="flex items-center gap-2 px-3 py-1.5 rounded-sm border border-edge bg-elevated text-xs text-body hover:bg-hover transition-colors"
              >
                <Clock size={13} className="text-muted flex-shrink-0" />
                {currentTimeRangeLabel}
                <ChevronDown size={13} className="text-muted flex-shrink-0" />
              </button>
              {timeRangeOpen && (
                <div className="absolute left-0 top-full mt-1 z-20 bg-card border border-edge rounded-sm shadow-lg min-w-[160px]">
                  {TIME_RANGE_OPTIONS.map(opt => (
                    <button
                      key={opt.value}
                      onClick={() => { setTimeRange(opt.value); setTimeRangeOpen(false); setPage(1); }}
                      className={`block w-full text-left px-4 py-2 text-xs hover:bg-hover transition-colors ${
                        timeRange === opt.value ? 'text-accent font-medium' : 'text-body'
                      }`}
                    >
                      {t(opt.labelKey)}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <Button variant="ghost" size="sm" onClick={() => setRefreshKey(k => k + 1)} title={t('requestExplorer.refresh')}>
              <RefreshCw size={13} />
            </Button>
          </div>

          {/* ── Active filter chips ── */}
          {activeChips.length > 0 && (
            <div className="flex items-center gap-2 flex-wrap">
              <Filter size={13} className="text-muted flex-shrink-0" />
              {activeChips.map(chip => (
                <span
                  key={chip.key}
                  className="flex items-center gap-1 px-2 py-0.5 bg-accent-muted text-accent rounded-sm text-xs font-medium"
                >
                  {chip.label}
                  <button
                    onClick={chip.onRemove}
                    className="ml-0.5 hover:text-accent-hover transition-colors"
                    aria-label={t('requestExplorer.removeFilter')}
                  >
                    <X size={10} />
                  </button>
                </span>
              ))}
              <button
                onClick={() => {
                  setSelectedServices([]);
                  setSelectedEndpoints([]);
                  setRequestStatus('');
                  setSpanStatus('');
                  setSelectedSpanKinds([]);
                  setDurationMin('');
                  setDurationMax('');
                }}
                className="text-xs text-muted hover:text-danger transition-colors ml-1"
              >
                {t('requestExplorer.clearAll')}
              </button>
            </div>
          )}

          {/* ── Main layout: facets + content ── */}
          <div className="flex gap-4 items-start">
            {/* ── Facet sidebar ── */}
            <aside className="w-60 flex-shrink-0">
              <Card>
                <CardBody className="p-3">
                  {/* Facet search */}
                  <div className="relative mb-3">
                    <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faded pointer-events-none" />
                    <input
                      className={`w-full pl-8 pr-3 ${inputCls}`}
                      placeholder={t('requestExplorer.searchFacets')}
                      readOnly
                    />
                  </div>

                  {/* Core / Duration */}
                  <CollapsibleFacet title={t('requestExplorer.facets.core')}>
                    <div className="text-xs text-faded mb-1">{t('requestExplorer.facets.duration')}</div>
                    <div className="flex gap-2">
                      <div>
                        <div className="text-xs text-faded mb-0.5">{t('requestExplorer.facets.min')}</div>
                        <input
                          className={`w-20 ${inputCls}`}
                          placeholder="0ns"
                          value={durationMin}
                          onChange={e => { setDurationMin(e.target.value); setPage(1); }}
                          aria-label={t('requestExplorer.facets.durationMin')}
                        />
                      </div>
                      <div>
                        <div className="text-xs text-faded mb-0.5">{t('requestExplorer.facets.max')}</div>
                        <input
                          className={`w-20 ${inputCls}`}
                          placeholder="1000s"
                          value={durationMax}
                          onChange={e => { setDurationMax(e.target.value); setPage(1); }}
                          aria-label={t('requestExplorer.facets.durationMax')}
                        />
                      </div>
                    </div>
                  </CollapsibleFacet>

                  {/* Service */}
                  <CollapsibleFacet title={t('requestExplorer.facets.service')}>
                    {(facets?.services ?? []).map(svc => (
                      <label key={svc} className="flex items-center gap-2 text-xs text-body py-0.5 cursor-pointer hover:text-accent transition-colors">
                        <input
                          type="checkbox"
                          checked={selectedServices.includes(svc)}
                          onChange={() => toggleListItem(selectedServices, setSelectedServices, svc)}
                          className="rounded-sm border-edge accent-accent"
                        />
                        {svc}
                      </label>
                    ))}
                    {!facets?.services?.length && (
                      <p className="text-xs text-faded">{t('requestExplorer.noFacets')}</p>
                    )}
                  </CollapsibleFacet>

                  {/* Endpoint */}
                  <CollapsibleFacet title={t('requestExplorer.facets.endpoint')}>
                    {(facets?.endpoints ?? []).slice(0, 8).map(ep => (
                      <label key={ep} className="flex items-center gap-2 text-xs text-body py-0.5 cursor-pointer hover:text-accent transition-colors break-all">
                        <input
                          type="checkbox"
                          checked={selectedEndpoints.includes(ep)}
                          onChange={() => toggleListItem(selectedEndpoints, setSelectedEndpoints, ep)}
                          className="rounded-sm border-edge accent-accent flex-shrink-0"
                        />
                        {ep}
                      </label>
                    ))}
                    {!facets?.endpoints?.length && (
                      <p className="text-xs text-faded">{t('requestExplorer.noFacets')}</p>
                    )}
                  </CollapsibleFacet>

                  {/* Request Status */}
                  <CollapsibleFacet title={t('requestExplorer.facets.requestStatus')}>
                    {(['Success', 'Failure'] as RequestStatus[]).map(s => (
                      <label key={s} className="flex items-center gap-2 text-xs py-0.5 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={requestStatus === s}
                          onChange={() => setRequestStatus(prev => (prev === s ? '' : s))}
                          className="rounded-sm border-edge accent-accent"
                        />
                        <span className={s === 'Success' ? 'text-success' : 'text-danger'}>
                          {t(`requestExplorer.status.${s.toLowerCase()}`)}
                        </span>
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* Span Status */}
                  <CollapsibleFacet title={t('requestExplorer.facets.spanStatus')}>
                    {(['Ok', 'Error'] as SpanStatusFilter[]).map(s => (
                      <label key={s} className="flex items-center gap-2 text-xs text-body py-0.5 cursor-pointer hover:text-accent transition-colors">
                        <input
                          type="checkbox"
                          checked={spanStatus === s}
                          onChange={() => setSpanStatus(prev => (prev === s ? '' : s))}
                          className="rounded-sm border-edge accent-accent"
                        />
                        {s}
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* Span Kind */}
                  <CollapsibleFacet title={t('requestExplorer.facets.spanKind')}>
                    {SPAN_KINDS.map(k => (
                      <label key={k} className="flex items-center gap-2 text-xs text-body py-0.5 cursor-pointer hover:text-accent transition-colors capitalize">
                        <input
                          type="checkbox"
                          checked={selectedSpanKinds.includes(k)}
                          onChange={() => toggleListItem(selectedSpanKinds, setSelectedSpanKinds, k)}
                          className="rounded-sm border-edge accent-accent"
                        />
                        {k}
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* HTTP */}
                  <CollapsibleFacet title="HTTP" defaultOpen={false}>
                    <p className="text-xs text-faded">{t('requestExplorer.facets.httpNote')}</p>
                  </CollapsibleFacet>
                </CardBody>
              </Card>
            </aside>

            {/* ── Content: chart + table ── */}
            <div className="flex-1 min-w-0 flex flex-col gap-3">
              {/* Chart card */}
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-body">
                        {viewMode === 'requests' ? t('requestExplorer.viewRequests') : t('requestExplorer.viewSpans')}
                      </h3>
                      {totalCount > 0 && (
                        <Badge variant="neutral" className="text-xs">
                          {totalCount.toLocaleString()} {viewMode === 'requests' ? t('requestExplorer.viewRequests').toLowerCase() : t('requestExplorer.viewSpans').toLowerCase()}
                        </Badge>
                      )}
                    </div>
                    {/* Legend */}
                    <div className="flex items-center gap-4 text-xs text-muted">
                      <span className="flex items-center gap-1.5">
                        <span className="inline-block w-2.5 h-2.5 rounded-sm" style={{ background: 'var(--t-danger)' }} />
                        {t('requestExplorer.chart.failedRequests')}
                      </span>
                      <span className="flex items-center gap-1.5">
                        <span className="inline-block w-2.5 h-2.5 rounded-sm" style={{ background: 'var(--t-accent)' }} />
                        {t('requestExplorer.chart.successfulRequests')}
                      </span>
                    </div>
                  </div>

                  {sreTimeSeriesQuery.isLoading ? (
                    <div className="flex items-center justify-center h-28 text-sm text-muted">
                      {t('requestExplorer.loading')}
                    </div>
                  ) : (
                    <TimeseriesAreaChart data={chartData} />
                  )}
                </CardBody>
              </Card>

              {/* Table card — requests mode: aggregated; spans mode: individual */}
              {viewMode === 'requests' ? (
                <Card>
                  <CardBody className="p-0">
                    {sreTopRequestsQuery.isLoading && (
                      <PageLoadingState message={t('requestExplorer.loading')} />
                    )}
                    {sreTopRequestsQuery.isError && (
                      <PageErrorState message={t('requestExplorer.loadError')} />
                    )}
                    {!sreTopRequestsQuery.isLoading && !sreTopRequestsQuery.isError && (
                      <div className="overflow-x-auto">
                        <table className="w-full text-sm text-left">
                          <thead>
                            <tr className="border-b border-edge bg-muted/20">
                              <th className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap">
                                {t('requestExplorer.table.endpoint')}
                              </th>
                              <th className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap">
                                {t('requestExplorer.table.service')}
                              </th>
                              <th className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap">
                                Total requests
                              </th>
                              <th className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap">
                                Error rate
                              </th>
                              <th className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap">
                                {t('requestExplorer.table.duration')}
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {topRequests.map((row, i) => (
                              // eslint-disable-next-line react/no-array-index-key
                              <AggregatedRequestRow key={i} row={row} />
                            ))}
                            {!topRequests.length && (
                              <tr>
                                <td colSpan={5} className="px-3 py-8 text-center text-sm text-muted">
                                  {t('requestExplorer.noRecords')}
                                </td>
                              </tr>
                            )}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </CardBody>
                </Card>
              ) : (
                <Card>
                  <CardBody className="p-0">
                    {requestsQuery.isLoading && (
                      <PageLoadingState message={t('requestExplorer.loading')} />
                    )}
                    {requestsQuery.isError && (
                      <PageErrorState message={t('requestExplorer.loadError')} />
                    )}
                    {!requestsQuery.isLoading && !requestsQuery.isError && (
                      <>
                        <div className="overflow-x-auto">
                          <table className="w-full text-sm text-left">
                            <thead>
                              <tr className="border-b border-edge bg-muted/20">
                                {[
                                  { key: 'startTime', label: t('requestExplorer.table.startTime') },
                                  { key: 'endpoint', label: t('requestExplorer.table.endpoint') },
                                  { key: 'service', label: t('requestExplorer.table.service') },
                                  { key: 'durationMs', label: t('requestExplorer.table.duration') },
                                  { key: 'requestStatus', label: t('requestExplorer.table.requestStatus') },
                                  { key: 'httpCode', label: t('requestExplorer.table.httpCode') },
                                  { key: 'processGroup', label: t('requestExplorer.table.processGroup') },
                                  { key: 'k8sWorkload', label: t('requestExplorer.table.k8sWorkload') },
                                  { key: 'k8sNamespace', label: t('requestExplorer.table.k8sNamespace') },
                                ].map(col => (
                                  <th
                                    key={col.key}
                                    className="px-3 py-2 text-xs font-medium text-muted whitespace-nowrap cursor-pointer hover:text-body transition-colors"
                                    onClick={() => handleSort(col.key)}
                                  >
                                    <div className="flex items-center gap-1">
                                      {col.label}
                                      <ArrowUpDown size={11} className={sortBy === col.key ? 'text-accent' : 'opacity-30'} />
                                    </div>
                                  </th>
                                ))}
                              </tr>
                            </thead>
                            <tbody>
                              {(spansResult?.items ?? []).map((row, i) => (
                                // eslint-disable-next-line react/no-array-index-key
                                <RequestRow key={i} row={row} t={t} />
                              ))}
                              {(!spansResult?.items?.length) && (
                                <tr>
                                  <td colSpan={9} className="px-3 py-8 text-center text-sm text-muted">
                                    {t('requestExplorer.noRecords')}
                                  </td>
                                </tr>
                              )}
                            </tbody>
                          </table>
                        </div>

                        {/* Pagination */}
                        {spansResult && spansResult.total > PAGE_SIZE && (
                          <div className="flex items-center justify-between px-4 py-3 border-t border-edge text-xs text-muted">
                            <span>
                              {t('requestExplorer.pagination.showing', {
                                from: (page - 1) * PAGE_SIZE + 1,
                                to: Math.min(page * PAGE_SIZE, spansResult.total),
                                total: spansResult.total,
                              })}
                            </span>
                            <div className="flex gap-2">
                              <Button
                                variant="ghost"
                                size="sm"
                                disabled={page <= 1}
                                onClick={() => setPage(p => p - 1)}
                              >
                                {t('requestExplorer.pagination.prev')}
                              </Button>
                              <Button
                                variant="ghost"
                                size="sm"
                                disabled={page * PAGE_SIZE >= spansResult.total}
                                onClick={() => setPage(p => p + 1)}
                              >
                                {t('requestExplorer.pagination.next')}
                              </Button>
                            </div>
                          </div>
                        )}
                      </>
                    )}
                  </CardBody>
                </Card>
              )}
            </div>
          </div>
        </div>
      </PageSection>
    </PageContainer>
  );
}

// ── Aggregated request row (requests mode) ────────────────────────────────────

function AggregatedRequestRow({ row }: { row: SreTopRequest }) {
  const errorRate = row.count > 0 ? (row.errors / row.count) * 100 : 0;

  function errorRateVariant(): 'danger' | 'warning' | 'success' | 'neutral' {
    if (errorRate > 10) return 'danger';
    if (errorRate > 0) return 'warning';
    return 'success';
  }

  return (
    <tr className="border-b border-edge/50 hover:bg-muted/20 transition-colors">
      <td className="px-3 py-2.5 text-xs text-body max-w-[240px] truncate" title={row.request}>
        {row.request}
      </td>
      <td className="px-3 py-2.5 text-xs text-muted whitespace-nowrap">
        {row.service}
      </td>
      <td className="px-3 py-2.5 text-xs text-body tabular-nums">
        {row.count.toLocaleString()}
      </td>
      <td className="px-3 py-2.5">
        {errorRate > 0 ? (
          <Badge variant={errorRateVariant()} className="text-xs tabular-nums">
            {errorRate.toFixed(1)}%
          </Badge>
        ) : (
          <Badge variant="success" className="text-xs">
            0%
          </Badge>
        )}
      </td>
      <td className="px-3 py-2.5 text-xs text-body tabular-nums whitespace-nowrap">
        {fmtDuration(row.avgLatencyMs)}
      </td>
    </tr>
  );
}

// ── Individual span row (spans mode) ─────────────────────────────────────────

function RequestRow({ row, t }: { row: RequestSpan; t: (k: string) => string }) {
  const isFailure = row.requestStatus === 'Failure';

  function httpCodeClass(code: number): string {
    if (code >= 500) return 'text-danger font-medium';
    if (code >= 400) return 'text-warning font-medium';
    if (code >= 200) return 'text-success';
    return 'text-muted';
  }

  return (
    <tr className={`border-b border-edge/50 hover:bg-muted/20 transition-colors ${isFailure ? 'border-l-2 border-l-danger' : ''}`}>
      <td className="px-3 py-2 text-xs text-muted whitespace-nowrap font-mono">
        {fmtDateTime(row.startTime)}
      </td>
      <td className="px-3 py-2 text-xs text-body max-w-[180px] truncate" title={row.endpoint}>
        {row.endpoint}
      </td>
      <td className="px-3 py-2 text-xs text-body whitespace-nowrap">
        {row.service}
      </td>
      <td className="px-3 py-2 text-xs text-body whitespace-nowrap tabular-nums">
        {fmtDuration(row.durationMs)}
      </td>
      <td className="px-3 py-2">
        <Badge variant={isFailure ? 'danger' : 'success'} className="text-xs">
          {isFailure ? t('requestExplorer.status.failure') : t('requestExplorer.status.success')}
        </Badge>
      </td>
      <td className="px-3 py-2 text-xs whitespace-nowrap">
        {row.httpCode != null ? (
          <span className={`font-mono ${httpCodeClass(row.httpCode)}`}>
            {row.httpCode}
          </span>
        ) : '—'}
      </td>
      <td className="px-3 py-2 text-xs text-muted whitespace-nowrap max-w-[120px] truncate" title={row.processGroup}>
        {row.processGroup ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-muted whitespace-nowrap">
        {row.k8sWorkload ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-muted whitespace-nowrap">
        {row.k8sNamespace ?? '—'}
      </td>
    </tr>
  );
}
