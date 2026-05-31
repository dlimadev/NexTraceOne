/**
 * RequestExplorerPage — Explorador de requests/spans com filtros facetados.
 *
 * Layout inspirado no Dynatrace Distributed Tracing → Request Explorer:
 * - Painel esquerdo: facetas colapsáveis (Core/Duration, Service, Endpoint,
 *   Request Status, Span Status, Span Kind, HTTP)
 * - Barra de filtros activos (chips com X para remover)
 * - Toggle Requests / Spans no topo
 * - Toggle Timeseries / Histogram (ECharts bar chart)
 * - Tabela paginada e ordenável:
 *   Start time · Endpoint · Service · Duration · Request Status · HTTP code ·
 *   Process group · K8s workload · K8s namespace
 *
 * Observabilidade contextualizada — nunca como dashboard genérico isolado.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Operational Intelligence
 */
import * as React from 'react';
import { useState, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
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
  type RequestQueryParams,
  type RequestSpan,
  type RequestStatus,
  type SpanStatusFilter,
  type SpanKindFilter,
  type ChartMode,
  type RequestViewMode,
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

// ── Histogram chart ───────────────────────────────────────────────────────────

interface HistogramProps {
  buckets: Array<{ durationLabel: string; successCount: number; failureCount: number }>;
}

function HistogramChart({ buckets }: HistogramProps) {
  const maxVal = useMemo(
    () => Math.max(1, ...buckets.map(b => b.successCount + b.failureCount)),
    [buckets],
  );
  if (!buckets.length) return null;
  return (
    <div
      className="flex items-end gap-0.5 h-36 w-full px-2"
      role="img"
      aria-label="request duration histogram"
    >
      {buckets.map((b, i) => {
        const total = b.successCount + b.failureCount;
        const heightPct = (total / maxVal) * 100;
        const failurePct = total > 0 ? (b.failureCount / total) * 100 : 0;
        return (
          <div
            key={i}
            className="flex flex-col flex-1 items-stretch justify-end"
            title={`${b.durationLabel}: ${total} (${b.failureCount} failed)`}
          >
            <div className="flex flex-col rounded-t-sm overflow-hidden" style={{ height: `${heightPct}%` }}>
              {b.failureCount > 0 && (
                <div
                  style={{ height: `${failurePct}%`, minHeight: 2, background: 'var(--t-danger)' }}
                />
              )}
              <div className="flex-1" style={{ background: 'var(--t-data-2)' }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function RequestExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  // ── State ──────────────────────────────────────────────────────────────────
  const [viewMode, setViewMode] = useState<RequestViewMode>('requests');
  const [chartMode, setChartMode] = useState<ChartMode>('histogram');
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

  // Table state
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState<string>('startTime');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');

  const PAGE_SIZE = 20;

  const { from, until } = timeRangeToInterval(timeRange);
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

  const requestsQuery = useQuery({
    queryKey: ['requests', queryParams, refreshKey],
    queryFn: () => getRequests(queryParams),
    enabled: !!environment,
  });

  const facetsQuery = useQuery({
    queryKey: ['request-facets', environment, from, until, refreshKey],
    queryFn: () => getRequestFacets(environment, from, until),
    enabled: !!environment,
  });

  const facets = facetsQuery.data;
  const result = requestsQuery.data;

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

  // ── Loading / error ────────────────────────────────────────────────────────

  const isLoading = requestsQuery.isLoading;
  const isError = requestsQuery.isError;

  if (isLoading) return <PageLoadingState message={t('requestExplorer.loading')} />;
  if (isError) return <PageErrorState message={t('requestExplorer.loadError')} />;

  // ── Span kind options ──────────────────────────────────────────────────────

  const SPAN_KINDS: SpanKindFilter[] = ['client', 'server', 'consumer', 'producer', 'internal', 'link'];

  // ── Shared input / control classes ────────────────────────────────────────

  const inputCls = 'px-2 py-1.5 text-xs rounded-sm border border-edge bg-elevated text-body placeholder-faded focus:outline-none focus:ring-1 focus:ring-accent';

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
                  {/* Facet search (decorativo) */}
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
                      <h3 className="text-sm font-semibold text-foreground">
                        {viewMode === 'requests' ? t('requestExplorer.viewRequests') : t('requestExplorer.viewSpans')}
                      </h3>
                      {result && (
                        <Badge variant="neutral" className="text-xs">
                          {result.total.toLocaleString()} {viewMode === 'requests' ? t('requestExplorer.viewRequests').toLowerCase() : t('requestExplorer.viewSpans').toLowerCase()}
                        </Badge>
                      )}
                    </div>
                    {/* Chart mode toggle */}
                    <div className="flex rounded-sm border border-edge overflow-hidden text-xs">
                      {(['timeseries', 'histogram'] as ChartMode[]).map(mode => (
                        <button
                          key={mode}
                          onClick={() => setChartMode(mode)}
                          className={`px-3 py-1 font-medium capitalize transition-colors ${
                            chartMode === mode
                              ? 'bg-accent text-on-accent'
                              : 'bg-elevated text-muted hover:bg-hover hover:text-body'
                          }`}
                        >
                          {t(`requestExplorer.chart.${mode}`)}
                        </button>
                      ))}
                    </div>
                  </div>

                  {result && result.histogram.length > 0 ? (
                    <>
                      <div className="flex items-center gap-4 mb-2 text-xs text-muted">
                        <span className="flex items-center gap-1.5">
                          <span className="inline-block w-2.5 h-2.5 rounded-sm" style={{ background: 'var(--t-danger)' }} />
                          {t('requestExplorer.chart.failedRequests')}
                        </span>
                        <span className="flex items-center gap-1.5">
                          <span className="inline-block w-2.5 h-2.5 rounded-sm" style={{ background: 'var(--t-data-2)' }} />
                          {t('requestExplorer.chart.successfulRequests')}
                        </span>
                      </div>
                      <HistogramChart buckets={result.histogram} />
                      {/* x-axis labels */}
                      <div className="flex justify-between px-2 mt-1 text-[10px] text-faded">
                        {result.histogram
                          .filter((_, i) => i % Math.max(1, Math.floor(result.histogram.length / 6)) === 0)
                          .map((b, i) => <span key={i}>{b.durationLabel}</span>)}
                        <span>{t('requestExplorer.chart.durationLabel')}</span>
                      </div>
                    </>
                  ) : (
                    <div className="flex items-center justify-center h-28 text-sm text-muted">
                      {t('requestExplorer.noRecords')}
                    </div>
                  )}
                </CardBody>
              </Card>

              {/* Table card */}
              <Card>
                <CardBody className="p-0">
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                      <thead>
                        <tr className="border-b border-border bg-muted/20">
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
                              className="px-3 py-2 text-xs font-medium text-muted-foreground whitespace-nowrap cursor-pointer hover:text-foreground transition-colors"
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
                        {(result?.items ?? []).map((row, i) => (
                          <RequestRow key={i} row={row} t={t} />
                        ))}
                        {(!result?.items?.length) && (
                          <tr>
                            <td colSpan={9} className="px-3 py-8 text-center text-sm text-muted-foreground">
                              {t('requestExplorer.noRecords')}
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>

                  {/* Pagination */}
                  {result && result.total > PAGE_SIZE && (
                    <div className="flex items-center justify-between px-4 py-3 border-t border-border text-xs text-muted-foreground">
                      <span>
                        {t('requestExplorer.pagination.showing', {
                          from: (page - 1) * PAGE_SIZE + 1,
                          to: Math.min(page * PAGE_SIZE, result.total),
                          total: result.total,
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
                          disabled={page * PAGE_SIZE >= result.total}
                          onClick={() => setPage(p => p + 1)}
                        >
                          {t('requestExplorer.pagination.next')}
                        </Button>
                      </div>
                    </div>
                  )}
                </CardBody>
              </Card>
            </div>
          </div>
        </div>
      </PageSection>
    </PageContainer>
  );
}

// ── Row component ─────────────────────────────────────────────────────────────

function RequestRow({ row, t }: { row: RequestSpan; t: (k: string) => string }) {
  const isFailure = row.requestStatus === 'Failure';

  function httpCodeClass(code: number): string {
    if (code >= 500) return 'text-danger font-medium';
    if (code >= 400) return 'text-warning font-medium';
    if (code >= 200) return 'text-success';
    return 'text-muted-foreground';
  }

  return (
    <tr className={`border-b border-border/50 hover:bg-muted/20 transition-colors ${isFailure ? 'border-l-2 border-l-danger' : ''}`}>
      <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap font-mono">
        {fmtDateTime(row.startTime)}
      </td>
      <td className="px-3 py-2 text-xs text-foreground max-w-[180px] truncate" title={row.endpoint}>
        {row.endpoint}
      </td>
      <td className="px-3 py-2 text-xs text-foreground whitespace-nowrap">
        {row.service}
      </td>
      <td className="px-3 py-2 text-xs text-foreground whitespace-nowrap tabular-nums">
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
      <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap max-w-[120px] truncate" title={row.processGroup}>
        {row.processGroup ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap">
        {row.k8sWorkload ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap">
        {row.k8sNamespace ?? '—'}
      </td>
    </tr>
  );
}
