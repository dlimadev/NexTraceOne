/**
 * TraceExplorerPage — Explorador de traces distribuído unificado.
 *
 * Layout inspirado no Dynatrace Distributed Tracing Explorer:
 * - Vista de lista: tabela de traces com filtros e badges de tipo de serviço.
 * - Vista de detalhe: split horizontal — waterfall (esquerda) + painel de span (direita).
 *   - Waterfall com régua de tempo (timeline ruler) e profundidade real (parent-child).
 *   - Painel de detalhe com secções colapsáveis: Core, HTTP, Code, Networking, etc.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  Calendar,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock,
  Database,
  FileCode,
  Globe,
  Layers,
  MessageSquare,
  Search,
  Server,
  X,
  XCircle,
  Zap,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  queryTraces,
  getTraceDetail,
  type TraceSummary,
  type SpanDetail,
} from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Service Kind constants (mirror backend ServiceKindValues) ─────────────────

const SK_REST = 'REST';
const SK_SOAP = 'SOAP';
const SK_KAFKA = 'Kafka';
const SK_BACKGROUND = 'Background';
const SK_DB = 'DB';
const SK_GRPC = 'gRPC';
const SK_MESSAGING = 'Messaging';

// ── Service Kind UI helpers ───────────────────────────────────────────────────

interface ServiceKindMeta {
  label: string;
  icon: React.ReactNode;
  barClass: string;
  textClass: string;
}

function getServiceKindMeta(kind: string | undefined): ServiceKindMeta {
  switch (kind) {
    case SK_REST:
      return { label: 'REST', icon: <Globe className="w-3 h-3" />, barClass: 'bg-blue-500/70', textClass: 'text-blue-600 dark:text-blue-400' };
    case SK_SOAP:
      return { label: 'SOAP', icon: <FileCode className="w-3 h-3" />, barClass: 'bg-orange-500/70', textClass: 'text-orange-600 dark:text-orange-400' };
    case SK_KAFKA:
      return { label: 'Kafka', icon: <Layers className="w-3 h-3" />, barClass: 'bg-purple-500/70', textClass: 'text-purple-600 dark:text-purple-400' };
    case SK_BACKGROUND:
      return { label: 'Background', icon: <Clock className="w-3 h-3" />, barClass: 'bg-slate-500/70', textClass: 'text-slate-500 dark:text-slate-400' };
    case SK_DB:
      return { label: 'DB', icon: <Database className="w-3 h-3" />, barClass: 'bg-emerald-500/70', textClass: 'text-emerald-600 dark:text-emerald-400' };
    case SK_GRPC:
      return { label: 'gRPC', icon: <Zap className="w-3 h-3" />, barClass: 'bg-yellow-500/70', textClass: 'text-yellow-600 dark:text-yellow-400' };
    case SK_MESSAGING:
      return { label: 'Messaging', icon: <MessageSquare className="w-3 h-3" />, barClass: 'bg-indigo-500/70', textClass: 'text-indigo-600 dark:text-indigo-400' };
    default:
      return { label: 'Unknown', icon: <Server className="w-3 h-3" />, barClass: 'bg-primary/60', textClass: 'text-muted-foreground' };
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatDuration(ms: number): string {
  if (ms < 1) return '<1ms';
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(2)}s`;
}

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function statusBadge(statusCode?: string, hasErrors?: boolean): 'success' | 'danger' | 'default' {
  if (hasErrors || statusCode === 'Error') return 'danger';
  if (statusCode === 'Ok') return 'success';
  return 'default';
}

function getDefaultTimeRange(): { from: string; until: string } {
  const now = new Date();
  const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
  return { from: oneHourAgo.toISOString(), until: now.toISOString() };
}

/**
 * Constrói um mapa spanId → profundidade a partir da hierarquia parent-child.
 * Depth 0 = root span. Guarda contra ciclos via visited set.
 */
function buildDepthMap(spans: SpanDetail[]): Map<string, number> {
  const spanById = new Map<string, SpanDetail>(spans.map((s) => [s.spanId, s]));
  const depthCache = new Map<string, number>();

  function getDepth(spanId: string, visited = new Set<string>()): number {
    if (depthCache.has(spanId)) return depthCache.get(spanId)!;
    if (visited.has(spanId)) return 0;
    const span = spanById.get(spanId);
    if (!span || !span.parentSpanId) {
      depthCache.set(spanId, 0);
      return 0;
    }
    visited.add(spanId);
    const depth = getDepth(span.parentSpanId, visited) + 1;
    depthCache.set(spanId, depth);
    return depth;
  }

  for (const span of spans) getDepth(span.spanId);
  return depthCache;
}

// ── Attribute partition helpers ───────────────────────────────────────────────

function filterAttrs(
  attrs: Record<string, string>,
  pred: (k: string) => boolean,
): Record<string, string> {
  return Object.fromEntries(Object.entries(attrs).filter(([k]) => pred(k)));
}

// ── Timeline ruler ────────────────────────────────────────────────────────────

function buildTimeMarks(durationMs: number): Array<{ label: string; pct: number }> {
  if (durationMs <= 0) return [{ label: '0', pct: 0 }];
  const steps = 5;
  return Array.from({ length: steps + 1 }, (_, i) => ({
    label: formatDuration((i / steps) * durationMs),
    pct: (i / steps) * 100,
  }));
}

// ── Main Component ────────────────────────────────────────────────────────────

export function TraceExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const defaults = getDefaultTimeRange();

  const [environment, setEnvironment] = useState(activeEnvironmentId ?? 'production');
  const [serviceName, setServiceName] = useState('');
  const [operationName, setOperationName] = useState('');
  const [serviceKind, setServiceKind] = useState('');
  const [minDurationMs, setMinDurationMs] = useState('');
  const [errorsOnly, setErrorsOnly] = useState(false);
  const [from] = useState(defaults.from);
  const [until] = useState(defaults.until);
  const [limit] = useState(50);

  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(null);
  const [selectedSpan, setSelectedSpan] = useState<SpanDetail | null>(null);

  const tracesQuery = useQuery({
    queryKey: ['telemetry', 'traces', environment, from, until, serviceName, operationName, serviceKind, minDurationMs, errorsOnly, limit],
    queryFn: () =>
      queryTraces({
        environment,
        from,
        until,
        serviceName: serviceName || undefined,
        operationName: operationName || undefined,
        serviceKind: serviceKind || undefined,
        minDurationMs: minDurationMs ? parseFloat(minDurationMs) : undefined,
        hasErrors: errorsOnly || undefined,
        limit,
      }),
    enabled: !!environment,
  });

  const detailQuery = useQuery({
    queryKey: ['telemetry', 'trace-detail', selectedTraceId],
    queryFn: () => getTraceDetail(selectedTraceId!),
    enabled: !!selectedTraceId,
  });

  const [spanSearch, setSpanSearch] = useState('');

  const { sortedSpans, depthMap } = useMemo(() => {
    if (!detailQuery.data?.spans) return { sortedSpans: [], depthMap: new Map<string, number>() };
    const sorted = [...detailQuery.data.spans].sort(
      (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
    );
    return { sortedSpans: sorted, depthMap: buildDepthMap(sorted) };
  }, [detailQuery.data]);

  const filteredSpans = useMemo(() => {
    if (!spanSearch.trim()) return sortedSpans;
    const q = spanSearch.toLowerCase();
    return sortedSpans.filter(
      (s) =>
        s.operationName.toLowerCase().includes(q) ||
        s.serviceName.toLowerCase().includes(q),
    );
  }, [sortedSpans, spanSearch]);

  // ── Detail view (Dynatrace-style split layout) ───────────────────────────────
  if (selectedTraceId) {
    const traceData = detailQuery.data;
    const rootSpan = sortedSpans[0];
    const traceStartMs = rootSpan ? new Date(rootSpan.startTime).getTime() : 0;

    return (
      <div className="flex flex-col" style={{ height: 'calc(100vh - 64px)' }}>
        {/* ── Header bar ─────────────────────────────────────────────────────── */}
        <div className="flex items-center justify-between px-4 py-2.5 border-b border-border bg-card flex-shrink-0">
          <div className="flex items-center gap-2 min-w-0">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => { setSelectedTraceId(null); setSelectedSpan(null); setSpanSearch(''); }}
            >
              <ArrowLeft className="w-4 h-4 mr-1" />
              {t('telemetryExplorer.traces.title')}
            </Button>
            <ChevronRight className="w-4 h-4 text-muted-foreground flex-shrink-0" />
            <span className="text-sm font-semibold truncate">
              {detailQuery.isLoading
                ? t('telemetryExplorer.loading')
                : `${t('telemetryExplorer.traces.detail.title')}: ${rootSpan?.operationName ?? selectedTraceId}`}
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => { setSelectedTraceId(null); setSelectedSpan(null); setSpanSearch(''); }}
          >
            <X className="w-4 h-4 mr-1.5" />
            {t('telemetryExplorer.traces.detail.closeDetails')}
          </Button>
        </div>

        {/* ── Trace metadata bar ──────────────────────────────────────────────── */}
        {traceData && rootSpan && (
          <div className="flex flex-wrap items-center gap-x-6 gap-y-1 px-4 py-2 border-b border-border bg-card/50 flex-shrink-0 text-sm">
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <Clock className="w-3.5 h-3.5" />
              <span className="text-xs">{t('telemetryExplorer.traces.duration')}:</span>
              <span className="font-medium text-foreground">{formatDuration(traceData.durationMs)}</span>
            </span>
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <Activity className="w-3.5 h-3.5" />
              <span className="text-xs">{t('telemetryExplorer.traces.detail.responseTime')}:</span>
              <span className="font-medium text-foreground">{formatDuration(rootSpan.durationMs)}</span>
            </span>
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <Calendar className="w-3.5 h-3.5" />
              <span className="font-medium text-foreground text-xs">
                {new Date(rootSpan.startTime).toLocaleString()}
              </span>
            </span>
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <span className="text-xs">{t('telemetryExplorer.traces.detail.serviceName')}:</span>
              <span className="font-medium text-primary">{rootSpan.serviceName}</span>
            </span>
          </div>
        )}

        {/* ── Trace ID + span count + span search ─────────────────────────────── */}
        {traceData && (
          <div className="flex items-center gap-3 px-4 py-2 border-b border-border flex-shrink-0 bg-muted/10">
            <span className="font-mono text-xs text-muted-foreground truncate max-w-[320px]">
              {t('telemetryExplorer.traces.detail.traceIdLabel')}: {selectedTraceId}
            </span>
            <Badge variant="neutral" className="text-xs font-medium flex-shrink-0">
              {traceData.spans.length} {t('telemetryExplorer.traces.detail.spans').toLowerCase()}
            </Badge>
            <div className="relative ml-auto w-72">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground pointer-events-none" />
              <input
                className="w-full h-7 pl-8 pr-3 rounded-md border border-input bg-background text-xs focus:outline-none focus:ring-1 focus:ring-ring"
                placeholder={t('telemetryExplorer.traces.detail.searchSpans')}
                value={spanSearch}
                onChange={(e) => setSpanSearch(e.target.value)}
              />
            </div>
          </div>
        )}

        {/* ── Loading / error ──────────────────────────────────────────────────── */}
        {detailQuery.isLoading && <PageLoadingState message={t('telemetryExplorer.loading')} />}
        {detailQuery.isError && <PageErrorState message={t('telemetryExplorer.error')} />}

        {/* ── Main content: waterfall + span detail panel ──────────────────────── */}
        {traceData && (
          <div className="flex flex-1 min-h-0 overflow-hidden">
            {/* Waterfall panel */}
            <div
              className={`flex flex-col min-h-0 overflow-hidden border-r border-border transition-all ${
                selectedSpan ? 'flex-1' : 'w-full'
              }`}
            >
              {/* Column header + timeline ruler */}
              <div className="flex items-stretch border-b border-border bg-muted/20 flex-shrink-0 select-none">
                <div className="flex items-center px-3 py-1.5 border-r border-border/40 text-xs font-semibold text-muted-foreground"
                  style={{ width: '46%', flexShrink: 0 }}>
                  {t('telemetryExplorer.traces.detail.spans')} / {t('telemetryExplorer.traces.service')}
                </div>
                <div className="flex items-center px-2 py-1.5 text-xs font-semibold text-muted-foreground"
                  style={{ width: '9%', flexShrink: 0 }}>
                  {t('telemetryExplorer.traces.duration')}
                </div>
                <div className="relative flex items-center flex-1 px-2 py-1.5">
                  {buildTimeMarks(traceData.durationMs).map(({ label, pct }) => (
                    <span
                      key={pct}
                      className="absolute text-[10px] text-muted-foreground/60"
                      style={{ left: `${pct}%`, transform: pct > 0 ? 'translateX(-50%)' : undefined }}
                    >
                      {label}
                    </span>
                  ))}
                </div>
              </div>

              {/* Span rows */}
              <div className="flex-1 overflow-y-auto divide-y divide-border/40">
                {filteredSpans.length === 0 && (
                  <div className="text-center py-10 text-sm text-muted-foreground">
                    {t('telemetryExplorer.traces.noTraces')}
                  </div>
                )}
                {filteredSpans.map((span) => {
                  const spanStartMs = new Date(span.startTime).getTime();
                  const traceDuration = traceData.durationMs || 1;
                  const offsetPct = Math.min(((spanStartMs - traceStartMs) / traceDuration) * 100, 99);
                  const widthPct = Math.max((span.durationMs / traceDuration) * 100, 0.5);
                  const depth = depthMap.get(span.spanId) ?? 0;
                  const isSelected = selectedSpan?.spanId === span.spanId;
                  const hasError = span.statusCode === 'Error';
                  const kindMeta = getServiceKindMeta(span.serviceKind);

                  return (
                    <button
                      key={span.spanId}
                      type="button"
                      className={`w-full text-left flex items-center hover:bg-muted/40 transition-colors ${
                        isSelected ? 'bg-primary/10 border-l-2 border-l-primary' : ''
                      }`}
                      onClick={() => setSelectedSpan(span)}
                    >
                      {/* Name + service column */}
                      <div
                        className="flex items-center gap-1 px-2 py-1.5 border-r border-border/30 min-w-0"
                        style={{ width: '46%', flexShrink: 0 }}
                      >
                        <div style={{ width: `${depth * 12}px`, flexShrink: 0 }} />
                        <ChevronRight className="w-3 h-3 text-muted-foreground/40 flex-shrink-0" />
                        <span className={`flex-shrink-0 ${kindMeta.textClass}`} title={kindMeta.label}>
                          {kindMeta.icon}
                        </span>
                        <span className="text-xs font-medium text-foreground truncate">{span.serviceName}</span>
                        <span className="text-xs text-muted-foreground truncate ml-0.5">{span.operationName}</span>
                        {hasError && <XCircle className="w-3 h-3 text-destructive flex-shrink-0 ml-auto" />}
                      </div>
                      {/* Duration column */}
                      <div
                        className="flex items-center px-2 py-1.5 text-xs text-muted-foreground tabular-nums"
                        style={{ width: '9%', flexShrink: 0 }}
                      >
                        {formatDuration(span.durationMs)}
                      </div>
                      {/* Timeline bar column */}
                      <div className="flex items-center flex-1 px-2 py-1.5 min-w-0">
                        <div className="relative w-full h-4">
                          <div
                            className={`absolute top-0.5 bottom-0.5 rounded-sm ${
                              hasError ? 'bg-destructive/70' : kindMeta.barClass
                            }`}
                            style={{
                              left: `${offsetPct}%`,
                              width: `${Math.min(widthPct, 100 - offsetPct)}%`,
                              minWidth: '3px',
                            }}
                          />
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Span detail panel */}
            {selectedSpan && (
              <div className="flex-shrink-0 flex flex-col overflow-hidden border-l border-border" style={{ width: '380px' }}>
                <SpanDetailPanel span={selectedSpan} t={t} onClose={() => setSelectedSpan(null)} />
              </div>
            )}
          </div>
        )}
      </div>
    );
  }

  // ── List view ─────────────────────────────────────────────────────────────────
  return (
    <PageContainer>
      <PageHeader
        title={t('telemetryExplorer.traces.title')}
        subtitle={t('telemetryExplorer.traces.subtitle')}
        icon={<Activity className="w-6 h-6 text-primary" />}
      />

      <PageSection>
        <Card>
          <CardBody className="p-4">
            <div className="flex flex-wrap items-end gap-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.filters.environment')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm"
                  value={environment}
                  onChange={(e) => setEnvironment(e.target.value)}
                >
                  <option value="production">Production</option>
                  <option value="staging">Staging</option>
                  <option value="development">Development</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.filters.service')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-36"
                  placeholder={t('telemetryExplorer.traces.service')}
                  value={serviceName}
                  onChange={(e) => setServiceName(e.target.value)}
                />
              </div>

              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.traces.operation')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-36"
                  placeholder={t('telemetryExplorer.traces.operation')}
                  value={operationName}
                  onChange={(e) => setOperationName(e.target.value)}
                />
              </div>

              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.traces.serviceKind')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm"
                  value={serviceKind}
                  onChange={(e) => setServiceKind(e.target.value)}
                  data-testid="service-kind-filter"
                >
                  <option value="">{t('telemetryExplorer.serviceKind.all')}</option>
                  <option value="REST">{t('telemetryExplorer.serviceKind.rest')}</option>
                  <option value="SOAP">{t('telemetryExplorer.serviceKind.soap')}</option>
                  <option value="Kafka">{t('telemetryExplorer.serviceKind.kafka')}</option>
                  <option value="Background">{t('telemetryExplorer.serviceKind.background')}</option>
                  <option value="DB">{t('telemetryExplorer.serviceKind.db')}</option>
                  <option value="gRPC">{t('telemetryExplorer.serviceKind.grpc')}</option>
                  <option value="Messaging">{t('telemetryExplorer.serviceKind.messaging')}</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.traces.minDuration')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-28"
                  type="number"
                  placeholder="0"
                  value={minDurationMs}
                  onChange={(e) => setMinDurationMs(e.target.value)}
                />
              </div>

              <div className="flex items-center gap-2 h-9">
                <input
                  type="checkbox"
                  id="errorsOnly"
                  checked={errorsOnly}
                  onChange={(e) => setErrorsOnly(e.target.checked)}
                  className="h-4 w-4 rounded border-input"
                />
                <label htmlFor="errorsOnly" className="text-sm">{t('telemetryExplorer.traces.errorsOnly')}</label>
              </div>

              <Button size="sm" onClick={() => tracesQuery.refetch()}>
                <Search className="w-4 h-4 mr-1" />
                {t('telemetryExplorer.filters.apply')}
              </Button>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      <PageSection>
        {tracesQuery.isLoading && <PageLoadingState message={t('telemetryExplorer.loading')} />}
        {tracesQuery.isError && <PageErrorState message={t('telemetryExplorer.error')} />}

        {tracesQuery.data && (
          <Card>
            <CardBody className="p-0">
              {tracesQuery.data.length === 0 ? (
                <div className="text-center py-12">
                  <Activity className="w-10 h-10 text-muted-foreground mx-auto mb-3" />
                  <p className="text-sm font-medium">{t('telemetryExplorer.traces.noTraces')}</p>
                  <p className="text-xs text-muted-foreground mt-1">{t('telemetryExplorer.traces.noTracesDescription')}</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-border bg-muted/30">
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.traceId')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.serviceKind')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.service')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.operation')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.duration')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.status')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.spanCount')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.startTime')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {tracesQuery.data.map((trace: TraceSummary) => {
                        const kindMeta = getServiceKindMeta(trace.rootServiceKind);
                        return (
                          <tr
                            key={trace.traceId}
                            className="hover:bg-muted/50 cursor-pointer transition-colors"
                            onClick={() => setSelectedTraceId(trace.traceId)}
                          >
                            <td className="px-4 py-2 font-mono text-xs truncate max-w-[160px]">{trace.traceId}</td>
                            <td className="px-4 py-2">
                              <span className={`inline-flex items-center gap-1.5 text-xs font-medium ${kindMeta.textClass}`}>
                                {kindMeta.icon}
                                {kindMeta.label}
                              </span>
                            </td>
                            <td className="px-4 py-2">{trace.serviceName}</td>
                            <td className="px-4 py-2 text-muted-foreground truncate max-w-[180px]">{trace.operationName}</td>
                            <td className="px-4 py-2">{formatDuration(trace.durationMs)}</td>
                            <td className="px-4 py-2">
                              <Badge variant={statusBadge(trace.statusCode, trace.hasErrors)}>
                                {trace.hasErrors
                                  ? <><AlertTriangle className="w-3 h-3 mr-1" />{t('telemetryExplorer.traces.statusError')}</>
                                  : <><CheckCircle2 className="w-3 h-3 mr-1" />{t('telemetryExplorer.traces.statusOk')}</>
                                }
                              </Badge>
                            </td>
                            <td className="px-4 py-2 text-center">{trace.spanCount}</td>
                            <td className="px-4 py-2 text-muted-foreground">{formatTimestamp(trace.startTime)}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>
        )}
      </PageSection>
    </PageContainer>
  );
}

// ── ExpandableSection ─────────────────────────────────────────────────────────

interface ExpandableSectionProps {
  title: string;
  children: React.ReactNode;
  defaultOpen?: boolean;
}

function ExpandableSection({ title, children, defaultOpen = false }: ExpandableSectionProps) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border-b border-border/50 last:border-b-0">
      <button
        type="button"
        className="w-full flex items-center justify-between px-3 py-2 text-xs font-semibold text-foreground hover:bg-muted/30 transition-colors"
        onClick={() => setOpen(!open)}
      >
        <span className="text-left">{title}</span>
        <ChevronDown
          className={`w-3.5 h-3.5 text-muted-foreground flex-shrink-0 transition-transform ${open ? '' : '-rotate-90'}`}
        />
      </button>
      {open && (
        <div className="px-3 pb-2 pt-0 space-y-0.5">
          {children}
        </div>
      )}
    </div>
  );
}

// ── Span Detail Panel (Dynatrace-style) ───────────────────────────────────────

interface TFunc {
  (key: string): string;
}

interface SpanDetailPanelProps {
  span: SpanDetail;
  t: TFunc;
  onClose: () => void;
}

function SpanDetailPanel({ span, t, onClose }: SpanDetailPanelProps) {
  const [detailSearch, setDetailSearch] = useState('');
  const kindMeta = getServiceKindMeta(span.serviceKind);
  const hasError = span.statusCode === 'Error';
  const attrs = span.spanAttributes ?? {};
  const resAttrs = span.resourceAttributes ?? {};

  // Partition span attributes into semantic groups
  const httpAttrs = filterAttrs(attrs, (k) =>
    k.startsWith('http.') || k.startsWith('url.') || k === 'network.protocol.name',
  );
  const codeAttrs = filterAttrs(attrs, (k) => k.startsWith('code.') || k.startsWith('thread.'));
  const netAttrs = filterAttrs(attrs, (k) =>
    k.startsWith('net.') || k.startsWith('server.') || k.startsWith('client.'),
  );
  const msgAttrs = filterAttrs(attrs, (k) => k.startsWith('messaging.'));
  const dbAttrs = filterAttrs(attrs, (k) => k.startsWith('db.') || k.startsWith('rpc.'));
  const hostAttrs = filterAttrs(resAttrs, (k) => k.startsWith('host.'));
  const deployAttrs = filterAttrs(resAttrs, (k) =>
    k.startsWith('service.') || k.startsWith('deployment.') || k.startsWith('process.'),
  );
  const knownKeys = new Set([
    ...Object.keys(httpAttrs),
    ...Object.keys(codeAttrs),
    ...Object.keys(netAttrs),
    ...Object.keys(msgAttrs),
    ...Object.keys(dbAttrs),
  ]);
  const otherAttrs = filterAttrs(attrs, (k) => !knownKeys.has(k));

  // Filter all entries by detailSearch
  const filterEntries = (obj: Record<string, string>) => {
    if (!detailSearch.trim()) return Object.entries(obj);
    const q = detailSearch.toLowerCase();
    return Object.entries(obj).filter(
      ([k, v]) => k.toLowerCase().includes(q) || v.toLowerCase().includes(q),
    );
  };

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Panel header: title + close */}
      <div className="flex items-start justify-between px-3 py-2.5 border-b border-border flex-shrink-0 bg-card">
        <div className="min-w-0 pr-2">
          <div className="text-sm font-semibold truncate">
            Span {span.operationName}
          </div>
          <div className="flex flex-wrap items-center gap-x-1.5 mt-0.5 text-xs text-muted-foreground">
            <span>{t('telemetryExplorer.traces.detail.endpoint')}:</span>
            <span className="text-primary font-medium truncate max-w-[120px]">{span.operationName}</span>
            <span className="opacity-40">|</span>
            <span>{t('telemetryExplorer.traces.detail.serviceName')}:</span>
            <span className="text-primary font-medium truncate max-w-[120px]">{span.serviceName}</span>
          </div>
        </div>
        <button
          type="button"
          className="flex-shrink-0 p-1 rounded hover:bg-muted transition-colors mt-0.5"
          onClick={onClose}
          aria-label="Close span detail"
        >
          <X className="w-4 h-4 text-muted-foreground" />
        </button>
      </div>

      {/* Duration + failure badge + service kind */}
      <div className="flex items-center gap-2 px-3 py-2 border-b border-border flex-shrink-0">
        <Clock className="w-3.5 h-3.5 text-muted-foreground flex-shrink-0" />
        <span className="text-sm font-medium">{formatDuration(span.durationMs)}</span>
        {hasError && (
          <Badge variant="danger" className="text-xs">
            <AlertTriangle className="w-3 h-3 mr-1" />
            {t('telemetryExplorer.traces.detail.failure')}
          </Badge>
        )}
        <span className={`ml-auto inline-flex items-center gap-1 text-xs font-medium ${kindMeta.textClass}`}>
          {kindMeta.icon}
          {kindMeta.label}
        </span>
      </div>

      {/* Search details */}
      <div className="px-3 py-2 border-b border-border flex-shrink-0">
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground pointer-events-none" />
          <input
            className="w-full h-7 pl-8 pr-3 rounded-md border border-input bg-background text-xs focus:outline-none focus:ring-1 focus:ring-ring"
            placeholder={t('telemetryExplorer.traces.detail.searchDetails')}
            value={detailSearch}
            onChange={(e) => setDetailSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Scrollable sections */}
      <div className="flex-1 overflow-y-auto">
        {/* Core */}
        <ExpandableSection title={t('telemetryExplorer.traces.detail.section.core')} defaultOpen>
          <DetailRow label={t('telemetryExplorer.traces.detail.endpoint')} value={span.operationName} />
          <DetailRow label={t('telemetryExplorer.traces.detail.responseTime')} value={formatDuration(span.durationMs)} />
          <DetailRow label={t('telemetryExplorer.traces.detail.serviceName')} value={span.serviceName} />
          {span.spanKind && (
            <DetailRow label={t('telemetryExplorer.traces.detail.spanKind')} value={span.spanKind} />
          )}
          {span.statusCode && (
            <div className="flex justify-between items-center py-0.5 text-xs">
              <span className="text-muted-foreground">{t('telemetryExplorer.traces.detail.spanStatus')}</span>
              <Badge variant={hasError ? 'danger' : 'success'} className="text-xs">
                {span.statusCode.toLowerCase()}
              </Badge>
            </div>
          )}
          <DetailRow label={t('telemetryExplorer.traces.detail.startTime')} value={new Date(span.startTime).toLocaleString()} />
          <DetailRow label={t('telemetryExplorer.traces.detail.endTime')} value={new Date(span.endTime).toLocaleString()} />
          <DetailRow label={t('telemetryExplorer.traces.detail.spanId')} value={span.spanId} mono />
          {span.parentSpanId && (
            <DetailRow label={t('telemetryExplorer.traces.detail.parentSpan')} value={span.parentSpanId} mono />
          )}
        </ExpandableSection>

        {/* HTTP */}
        {filterEntries(httpAttrs).length > 0 && (
          <ExpandableSection title="HTTP" defaultOpen>
            {filterEntries(httpAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Messaging / Kafka */}
        {filterEntries(msgAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.context.kafka.title')} defaultOpen>
            {filterEntries(msgAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* DB / RPC */}
        {filterEntries(dbAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.context.db.title')} defaultOpen>
            {filterEntries(dbAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Code attributes */}
        {filterEntries(codeAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.codeAttributes')}>
            {filterEntries(codeAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Networking */}
        {filterEntries(netAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.networking')}>
            {filterEntries(netAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Host */}
        {filterEntries(hostAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.host')}>
            {filterEntries(hostAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Deployment information */}
        {filterEntries(deployAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.deployment')}>
            {filterEntries(deployAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {/* Events */}
        {span.events && span.events.length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.events')}>
            {span.events.map((evt, idx) => (
              // eslint-disable-next-line react/no-array-index-key
              <div key={idx} className="border-l-2 border-primary/30 pl-2 mb-2">
                <div className="text-xs font-medium">{evt.name}</div>
                <div className="text-xs text-muted-foreground">{new Date(evt.timestamp).toLocaleString()}</div>
                {evt.attributes &&
                  Object.entries(evt.attributes).map(([k, v]) => (
                    <div key={k} className="text-xs font-mono text-muted-foreground">
                      {k}: {v}
                    </div>
                  ))}
              </div>
            ))}
          </ExpandableSection>
        )}

        {/* Other */}
        {filterEntries(otherAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.other')}>
            {filterEntries(otherAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}
      </div>
    </div>
  );
}

// ── Detail Row ────────────────────────────────────────────────────────────────

function DetailRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between items-start gap-2 py-0.5 text-xs">
      <span className="text-muted-foreground flex-shrink-0 max-w-[140px] truncate">{label}</span>
      <span className={`text-right break-all max-w-[180px] ${mono ? 'font-mono' : ''}`}>{value}</span>
    </div>
  );
}
