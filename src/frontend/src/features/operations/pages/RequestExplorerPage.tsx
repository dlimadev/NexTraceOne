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
    <div className="border-b border-slate-200 dark:border-slate-700 pb-3 mb-2">
      <button
        className="flex items-center w-full gap-1 py-1 text-sm font-medium text-slate-700 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white"
        onClick={() => setOpen(o => !o)}
      >
        {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        {title}
      </button>
      {open && <div className="mt-2 pl-1">{children}</div>}
    </div>
  );
}

// ── Histogram chart (pure CSS bars, no ECharts dep needed for test compat) ────

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
      className="flex items-end gap-0.5 h-40 w-full px-2"
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
            <div className="flex flex-col" style={{ height: `${heightPct}%` }}>
              {b.failureCount > 0 && (
                <div
                  className="bg-red-400 dark:bg-red-500"
                  style={{ height: `${failurePct}%`, minHeight: 2 }}
                />
              )}
              <div className="bg-teal-500 dark:bg-teal-400 flex-1" />
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
            <div className="flex rounded-md border border-slate-300 dark:border-slate-600 overflow-hidden text-sm">
              {(['requests', 'spans'] as RequestViewMode[]).map(mode => (
                <button
                  key={mode}
                  onClick={() => { setViewMode(mode); setPage(1); }}
                  className={`px-4 py-1.5 font-medium capitalize transition-colors ${
                    viewMode === mode
                      ? 'bg-blue-600 text-white'
                      : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'
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
                className="flex items-center gap-2 px-3 py-1.5 rounded-md border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
              >
                <Clock size={14} />
                {currentTimeRangeLabel}
                <ChevronDown size={14} />
              </button>
              {timeRangeOpen && (
                <div className="absolute left-0 top-full mt-1 z-20 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md shadow-lg min-w-[160px]">
                  {TIME_RANGE_OPTIONS.map(opt => (
                    <button
                      key={opt.value}
                      onClick={() => { setTimeRange(opt.value); setTimeRangeOpen(false); setPage(1); }}
                      className={`block w-full text-left px-4 py-2 text-sm hover:bg-slate-50 dark:hover:bg-slate-700 ${
                        timeRange === opt.value ? 'text-blue-600 dark:text-blue-400 font-medium' : 'text-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {t(opt.labelKey)}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <Button variant="ghost" size="sm" onClick={() => setRefreshKey(k => k + 1)} title={t('requestExplorer.refresh')}>
              <RefreshCw size={14} />
            </Button>
          </div>

          {/* ── Active filter chips ── */}
          {activeChips.length > 0 && (
            <div className="flex items-center gap-2 flex-wrap">
              <Filter size={14} className="text-slate-400 flex-shrink-0" />
              {activeChips.map(chip => (
                <span
                  key={chip.key}
                  className="flex items-center gap-1 px-2 py-0.5 bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 rounded text-xs font-medium"
                >
                  {chip.label}
                  <button
                    onClick={chip.onRemove}
                    className="ml-0.5 hover:text-blue-900 dark:hover:text-blue-100"
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
                className="text-xs text-slate-500 hover:text-red-500 ml-1"
              >
                {t('requestExplorer.clearAll')}
              </button>
            </div>
          )}

          {/* ── Main layout: facets + content ── */}
          <div className="flex gap-4 items-start">
            {/* ── Facet sidebar ── */}
            <aside className="w-64 flex-shrink-0">
              <Card>
                <CardBody>
                  <div className="mb-3">
                    <div className="relative">
                      <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" />
                      <input
                        className="w-full pl-8 pr-3 py-1.5 text-sm rounded border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 placeholder-slate-400"
                        placeholder={t('requestExplorer.searchFacets')}
                        readOnly
                      />
                    </div>
                  </div>

                  {/* Core / Duration */}
                  <CollapsibleFacet title={t('requestExplorer.facets.core')}>
                    <div className="text-xs text-slate-500 dark:text-slate-400 mb-1">{t('requestExplorer.facets.duration')}</div>
                    <div className="flex gap-2">
                      <div>
                        <div className="text-xs text-slate-400 mb-0.5">{t('requestExplorer.facets.min')}</div>
                        <input
                          className="w-20 px-2 py-1 text-xs border border-slate-300 dark:border-slate-600 rounded bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300"
                          placeholder="0ns"
                          value={durationMin}
                          onChange={e => { setDurationMin(e.target.value); setPage(1); }}
                          aria-label={t('requestExplorer.facets.durationMin')}
                        />
                      </div>
                      <div>
                        <div className="text-xs text-slate-400 mb-0.5">{t('requestExplorer.facets.max')}</div>
                        <input
                          className="w-20 px-2 py-1 text-xs border border-slate-300 dark:border-slate-600 rounded bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300"
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
                      <label key={svc} className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300 py-0.5 cursor-pointer hover:text-blue-600">
                        <input
                          type="checkbox"
                          checked={selectedServices.includes(svc)}
                          onChange={() => toggleListItem(selectedServices, setSelectedServices, svc)}
                          className="rounded border-slate-300"
                        />
                        {svc}
                      </label>
                    ))}
                    {!facets?.services?.length && (
                      <p className="text-xs text-slate-400">{t('requestExplorer.noFacets')}</p>
                    )}
                  </CollapsibleFacet>

                  {/* Endpoint */}
                  <CollapsibleFacet title={t('requestExplorer.facets.endpoint')}>
                    {(facets?.endpoints ?? []).slice(0, 8).map(ep => (
                      <label key={ep} className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300 py-0.5 cursor-pointer hover:text-blue-600 break-all">
                        <input
                          type="checkbox"
                          checked={selectedEndpoints.includes(ep)}
                          onChange={() => toggleListItem(selectedEndpoints, setSelectedEndpoints, ep)}
                          className="rounded border-slate-300 flex-shrink-0"
                        />
                        {ep}
                      </label>
                    ))}
                    {!facets?.endpoints?.length && (
                      <p className="text-xs text-slate-400">{t('requestExplorer.noFacets')}</p>
                    )}
                  </CollapsibleFacet>

                  {/* Request Status */}
                  <CollapsibleFacet title={t('requestExplorer.facets.requestStatus')}>
                    {(['Success', 'Failure'] as RequestStatus[]).map(s => (
                      <label key={s} className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300 py-0.5 cursor-pointer hover:text-blue-600">
                        <input
                          type="checkbox"
                          checked={requestStatus === s}
                          onChange={() => setRequestStatus(prev => (prev === s ? '' : s))}
                          className="rounded border-slate-300"
                        />
                        <span className={s === 'Success' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
                          {t(`requestExplorer.status.${s.toLowerCase()}`)}
                        </span>
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* Span Status */}
                  <CollapsibleFacet title={t('requestExplorer.facets.spanStatus')}>
                    {(['Ok', 'Error'] as SpanStatusFilter[]).map(s => (
                      <label key={s} className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300 py-0.5 cursor-pointer hover:text-blue-600">
                        <input
                          type="checkbox"
                          checked={spanStatus === s}
                          onChange={() => setSpanStatus(prev => (prev === s ? '' : s))}
                          className="rounded border-slate-300"
                        />
                        {s}
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* Span Kind */}
                  <CollapsibleFacet title={t('requestExplorer.facets.spanKind')}>
                    {SPAN_KINDS.map(k => (
                      <label key={k} className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300 py-0.5 cursor-pointer hover:text-blue-600">
                        <input
                          type="checkbox"
                          checked={selectedSpanKinds.includes(k)}
                          onChange={() => toggleListItem(selectedSpanKinds, setSelectedSpanKinds, k)}
                          className="rounded border-slate-300"
                        />
                        {k}
                      </label>
                    ))}
                  </CollapsibleFacet>

                  {/* HTTP */}
                  <CollapsibleFacet title="HTTP" defaultOpen={false}>
                    <p className="text-xs text-slate-400">{t('requestExplorer.facets.httpNote')}</p>
                  </CollapsibleFacet>
                </CardBody>
              </Card>
            </aside>

            {/* ── Content: chart + table ── */}
            <div className="flex-1 min-w-0 flex flex-col gap-4">
              {/* Chart card */}
              <Card>
                <CardBody>
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">
                        {viewMode === 'requests' ? t('requestExplorer.viewRequests') : t('requestExplorer.viewSpans')}
                      </h3>
                      {result && (
                        <Badge variant="neutral" className="text-xs">
                          {result.total.toLocaleString()} {viewMode === 'requests' ? t('requestExplorer.viewRequests').toLowerCase() : t('requestExplorer.viewSpans').toLowerCase()}
                        </Badge>
                      )}
                    </div>
                    {/* Chart mode toggle */}
                    <div className="flex rounded border border-slate-300 dark:border-slate-600 overflow-hidden text-xs">
                      {(['timeseries', 'histogram'] as ChartMode[]).map(mode => (
                        <button
                          key={mode}
                          onClick={() => setChartMode(mode)}
                          className={`px-3 py-1 font-medium capitalize transition-colors ${
                            chartMode === mode
                              ? 'bg-blue-600 text-white'
                              : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'
                          }`}
                        >
                          {t(`requestExplorer.chart.${mode}`)}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Histogram / legend */}
                  {result && result.histogram.length > 0 ? (
                    <>
                      <div className="flex items-center gap-4 mb-2 text-xs text-slate-500">
                        <span className="flex items-center gap-1">
                          <span className="inline-block w-3 h-3 rounded-sm bg-red-400 dark:bg-red-500" />
                          {t('requestExplorer.chart.failedRequests')}
                        </span>
                        <span className="flex items-center gap-1">
                          <span className="inline-block w-3 h-3 rounded-sm bg-teal-500 dark:bg-teal-400" />
                          {t('requestExplorer.chart.successfulRequests')}
                        </span>
                      </div>
                      <HistogramChart buckets={result.histogram} />
                      {/* x-axis labels */}
                      <div className="flex justify-between px-2 mt-1 text-xs text-slate-400">
                        {result.histogram
                          .filter((_, i) => i % Math.max(1, Math.floor(result.histogram.length / 6)) === 0)
                          .map((b, i) => <span key={i}>{b.durationLabel}</span>)}
                        <span>{t('requestExplorer.chart.durationLabel')}</span>
                      </div>
                    </>
                  ) : (
                    <div className="flex items-center justify-center h-32 text-sm text-slate-400">
                      {t('requestExplorer.noRecords')}
                    </div>
                  )}
                </CardBody>
              </Card>

              {/* Table card */}
              <Card>
                <CardBody>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                      <thead>
                        <tr className="border-b border-slate-200 dark:border-slate-700">
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
                              className="px-3 py-2 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide whitespace-nowrap cursor-pointer hover:text-slate-700 dark:hover:text-slate-200"
                              onClick={() => handleSort(col.key)}
                            >
                              <div className="flex items-center gap-1">
                                {col.label}
                                <ArrowUpDown size={12} className={sortBy === col.key ? 'text-blue-500' : 'opacity-40'} />
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
                            <td colSpan={9} className="px-3 py-8 text-center text-sm text-slate-400">
                              {t('requestExplorer.noRecords')}
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>

                  {/* Pagination */}
                  {result && result.total > PAGE_SIZE && (
                    <div className="flex items-center justify-between mt-4 text-sm text-slate-500">
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
  return (
    <tr className={`border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors ${isFailure ? 'border-l-2 border-l-red-500' : ''}`}>
      <td className="px-3 py-2 text-xs text-slate-500 whitespace-nowrap font-mono">
        {fmtDateTime(row.startTime)}
      </td>
      <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-300 max-w-[180px] truncate" title={row.endpoint}>
        {row.endpoint}
      </td>
      <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-300 whitespace-nowrap">
        {row.service}
      </td>
      <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-300 whitespace-nowrap">
        {fmtDuration(row.durationMs)}
      </td>
      <td className="px-3 py-2">
        <Badge variant={isFailure ? 'danger' : 'success'} className="text-xs">
          {isFailure ? t('requestExplorer.status.failure') : t('requestExplorer.status.success')}
        </Badge>
      </td>
      <td className="px-3 py-2 text-xs whitespace-nowrap">
        {row.httpCode != null ? (
          <span className={
            row.httpCode >= 500 ? 'text-red-600 dark:text-red-400 font-medium' :
            row.httpCode >= 400 ? 'text-orange-600 dark:text-orange-400 font-medium' :
            'text-slate-600 dark:text-slate-300'
          }>
            {row.httpCode}
          </span>
        ) : '—'}
      </td>
      <td className="px-3 py-2 text-xs text-slate-500 whitespace-nowrap max-w-[120px] truncate" title={row.processGroup}>
        {row.processGroup ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-slate-500 whitespace-nowrap">
        {row.k8sWorkload ?? '—'}
      </td>
      <td className="px-3 py-2 text-xs text-slate-500 whitespace-nowrap">
        {row.k8sNamespace ?? '—'}
      </td>
    </tr>
  );
}
