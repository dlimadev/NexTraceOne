/**
 * TraceExplorerPage — Explorador de traces distribuído unificado.
 *
 * Layout inspirado no Dynatrace Distributed Tracing Explorer:
 * - Vista de lista: tabela de traces com filtros, badges e mini barra de duração relativa.
 * - Vista de detalhe: split horizontal — waterfall (esquerda) + painel de span (direita).
 *   - Waterfall com árvore expand/collapse, régua de tempo e grade vertical.
 *   - Painel de detalhe com secções colapsáveis: Core, HTTP, Code, Networking, etc.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useMemo, useCallback } from 'react';
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
  Search,
  X,
  XCircle,
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

// ── Service Kind brand icons (inline SVG, 12×12 viewBox) ─────────────────────

const IconRest = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <path d="M1.5 3.5h9M1.5 6h9M1.5 8.5H7" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round"/>
    <circle cx="9.5" cy="8.5" r="1.2" fill="currentColor"/>
  </svg>
);

const IconSoap = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <path d="M3.5 4L1.5 6l2 2M8.5 4l2 2-2 2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M7 3.5l-2 5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
  </svg>
);

const IconKafka = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <circle cx="6" cy="6" r="1.8" fill="currentColor"/>
    <circle cx="2" cy="2.5" r="1.1" fill="currentColor" opacity=".65"/>
    <circle cx="10" cy="2.5" r="1.1" fill="currentColor" opacity=".65"/>
    <circle cx="2" cy="9.5" r="1.1" fill="currentColor" opacity=".65"/>
    <circle cx="10" cy="9.5" r="1.1" fill="currentColor" opacity=".65"/>
    <line x1="3.1" y1="3.4" x2="4.7" y2="5" stroke="currentColor" strokeWidth="1"/>
    <line x1="8.9" y1="3.4" x2="7.3" y2="5" stroke="currentColor" strokeWidth="1"/>
    <line x1="3.1" y1="8.6" x2="4.7" y2="7" stroke="currentColor" strokeWidth="1"/>
    <line x1="8.9" y1="8.6" x2="7.3" y2="7" stroke="currentColor" strokeWidth="1"/>
  </svg>
);

const IconBackground = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <circle cx="6" cy="6.5" r="4" stroke="currentColor" strokeWidth="1.1"/>
    <path d="M6 4V6.5l1.5 1.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const IconDb = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <ellipse cx="6" cy="3.5" rx="3.5" ry="1.3" stroke="currentColor" strokeWidth="1.1"/>
    <path d="M2.5 3.5v5c0 .7 1.6 1.3 3.5 1.3s3.5-.6 3.5-1.3v-5" stroke="currentColor" strokeWidth="1.1"/>
    <path d="M2.5 6c0 .7 1.6 1.3 3.5 1.3S9.5 6.7 9.5 6" stroke="currentColor" strokeWidth="1.1"/>
  </svg>
);

const IconGrpc = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <path d="M1 3.5h7.5m0 0L6.5 2M8.5 3.5L6.5 5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M11 8.5H3.5m0 0L5.5 7M3.5 8.5l2 1.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
  </svg>
);

const IconMessaging = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <rect x="1" y="2.5" width="8" height="5" rx="1" stroke="currentColor" strokeWidth="1.1"/>
    <path d="M9.5 5l1.5 1-1.5 1" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" strokeLinejoin="round"/>
    <path d="M3 5h4M3 7h2" stroke="currentColor" strokeWidth="1" strokeLinecap="round"/>
  </svg>
);

const IconServiceUnknown = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 12 12" fill="none" aria-hidden="true">
    <rect x="1.5" y="2" width="9" height="3" rx=".8" stroke="currentColor" strokeWidth="1.1"/>
    <rect x="1.5" y="7" width="9" height="3" rx=".8" stroke="currentColor" strokeWidth="1.1"/>
    <circle cx="9.5" cy="3.5" r=".7" fill="currentColor" opacity=".6"/>
    <circle cx="9.5" cy="8.5" r=".7" fill="currentColor" opacity=".6"/>
  </svg>
);

// ── Service Kind constants ────────────────────────────────────────────────────

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
  dotStyle: React.CSSProperties;
}

function getServiceKindMeta(kind: string | undefined): ServiceKindMeta {
  switch (kind) {
    case SK_REST:
      return { label: 'REST', icon: <IconRest className="w-3 h-3" />, barClass: 'bg-blue-500/80', textClass: 'text-blue-400', dotStyle: { background: 'var(--t-data-1)' } };
    case SK_SOAP:
      return { label: 'SOAP', icon: <IconSoap className="w-3 h-3" />, barClass: 'bg-orange-500/80', textClass: 'text-orange-400', dotStyle: { background: 'var(--t-data-4)' } };
    case SK_KAFKA:
      return { label: 'Kafka', icon: <IconKafka className="w-3 h-3" />, barClass: 'bg-purple-500/80', textClass: 'text-purple-400', dotStyle: { background: 'var(--t-data-6)' } };
    case SK_BACKGROUND:
      return { label: 'Background', icon: <IconBackground className="w-3 h-3" />, barClass: 'bg-slate-500/70', textClass: 'text-slate-400', dotStyle: { background: 'var(--t-data-7)' } };
    case SK_DB:
      return { label: 'DB', icon: <IconDb className="w-3 h-3" />, barClass: 'bg-emerald-500/80', textClass: 'text-emerald-400', dotStyle: { background: 'var(--t-data-2)' } };
    case SK_GRPC:
      return { label: 'gRPC', icon: <IconGrpc className="w-3 h-3" />, barClass: 'bg-yellow-500/80', textClass: 'text-yellow-400', dotStyle: { background: 'var(--t-data-4)' } };
    case SK_MESSAGING:
      return { label: 'Messaging', icon: <IconMessaging className="w-3 h-3" />, barClass: 'bg-indigo-500/80', textClass: 'text-indigo-400', dotStyle: { background: 'var(--t-data-8)' } };
    default:
      return { label: 'Unknown', icon: <IconServiceUnknown className="w-3 h-3" />, barClass: 'bg-accent/60', textClass: 'text-muted', dotStyle: { background: 'var(--t-muted)' } };
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

// ── Span tree helpers ─────────────────────────────────────────────────────────

interface SpanNode {
  span: SpanDetail;
  depth: number;
  children: SpanNode[];
  hasChildren: boolean;
}

function buildSpanTree(spans: SpanDetail[]): SpanNode[] {
  const sorted = [...spans].sort(
    (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
  );
  const nodeMap = new Map<string, SpanNode>();
  for (const span of sorted) {
    nodeMap.set(span.spanId, { span, depth: 0, children: [], hasChildren: false });
  }
  const roots: SpanNode[] = [];
  for (const span of sorted) {
    const node = nodeMap.get(span.spanId)!;
    if (span.parentSpanId && nodeMap.has(span.parentSpanId)) {
      const parent = nodeMap.get(span.parentSpanId)!;
      parent.children.push(node);
      parent.hasChildren = true;
    } else {
      roots.push(node);
    }
  }
  function assignDepth(node: SpanNode, depth: number) {
    node.depth = depth;
    for (const child of node.children) assignDepth(child, depth + 1);
  }
  for (const root of roots) assignDepth(root, 0);
  return roots;
}

function flattenVisible(roots: SpanNode[], collapsed: Set<string>): SpanNode[] {
  const result: SpanNode[] = [];
  function visit(node: SpanNode) {
    result.push(node);
    if (!collapsed.has(node.span.spanId)) {
      for (const child of node.children) visit(child);
    }
  }
  for (const root of roots) visit(root);
  return result;
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

// Vertical grid line positions matching time ruler marks
const GRID_PCTS = [20, 40, 60, 80, 100];

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
  const [collapsedSpans, setCollapsedSpans] = useState<Set<string>>(new Set());

  const toggleCollapse = useCallback((spanId: string) => {
    setCollapsedSpans(prev => {
      const next = new Set(prev);
      if (next.has(spanId)) next.delete(spanId);
      else next.add(spanId);
      return next;
    });
  }, []);

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

  const { spanRoots, rootSpan } = useMemo(() => {
    if (!detailQuery.data?.spans) return { spanRoots: [] as SpanNode[], rootSpan: undefined };
    const roots = buildSpanTree(detailQuery.data.spans);
    return { spanRoots: roots, rootSpan: roots[0]?.span };
  }, [detailQuery.data]);

  const visibleNodes = useMemo(() => {
    if (!spanSearch.trim()) return flattenVisible(spanRoots, collapsedSpans);
    // When searching: expand all, filter flat
    const q = spanSearch.toLowerCase();
    return flattenVisible(spanRoots, new Set()).filter(
      node =>
        node.span.operationName.toLowerCase().includes(q) ||
        node.span.serviceName.toLowerCase().includes(q),
    );
  }, [spanRoots, collapsedSpans, spanSearch]);

  const maxTraceDuration = useMemo(
    () => Math.max(...(tracesQuery.data ?? []).map(t => t.durationMs), 1),
    [tracesQuery.data],
  );

  // ── Detail view (Dynatrace-style waterfall) ───────────────────────────────────
  if (selectedTraceId) {
    const traceData = detailQuery.data;
    const traceStartMs = rootSpan ? new Date(rootSpan.startTime).getTime() : 0;

    function closeDetail() {
      setSelectedTraceId(null);
      setSelectedSpan(null);
      setSpanSearch('');
      setCollapsedSpans(new Set());
    }

    return (
      <div className="flex flex-col" style={{ height: 'calc(100vh - 64px)' }}>
        {/* ── Header bar ─────────────────────────────────────────────────────── */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-edge bg-card flex-shrink-0">
          <div className="flex items-center gap-2 min-w-0">
            <Button variant="ghost" size="sm" onClick={closeDetail}>
              <ArrowLeft className="w-4 h-4 mr-1" />
              {t('telemetryExplorer.traces.title')}
            </Button>
            <ChevronRight className="w-3.5 h-3.5 text-muted flex-shrink-0" />
            <span className="text-sm font-semibold truncate">
              {detailQuery.isLoading
                ? t('telemetryExplorer.loading')
                : (rootSpan?.operationName ?? selectedTraceId)}
            </span>
          </div>
          <div className="flex items-center gap-4">
            {traceData && rootSpan && (
              <>
                <span className="flex items-center gap-1.5 text-xs text-muted">
                  <Clock className="w-3.5 h-3.5 flex-shrink-0" />
                  <span className="font-medium text-foreground tabular-nums">{formatDuration(traceData.durationMs)}</span>
                </span>
                <span className="flex items-center gap-1.5 text-xs text-muted">
                  <Calendar className="w-3.5 h-3.5 flex-shrink-0" />
                  <span className="text-foreground">{new Date(rootSpan.startTime).toLocaleString()}</span>
                </span>
                <code className="font-mono text-[11px] text-muted bg-muted/30 px-2 py-0.5 rounded-sm border border-edge/40">
                  {selectedTraceId?.slice(0, 16)}…
                </code>
              </>
            )}
            <Button variant="ghost" size="sm" onClick={closeDetail}>
              <X className="w-4 h-4" />
            </Button>
          </div>
        </div>

        {/* ── Span count + search ──────────────────────────────────────────────── */}
        {traceData && (
          <div className="flex items-center gap-3 px-4 py-1.5 border-b border-edge flex-shrink-0 bg-muted/5">
            <Badge variant="neutral" className="text-xs font-medium flex-shrink-0">
              {traceData.spans.length} {t('telemetryExplorer.traces.detail.spans').toLowerCase()}
            </Badge>
            {rootSpan && (
              <span className="text-xs text-muted">
                {t('telemetryExplorer.traces.detail.serviceName')}:{' '}
                <span className="text-accent font-medium">{rootSpan.serviceName}</span>
              </span>
            )}
            <div className="relative ml-auto w-72">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted pointer-events-none" />
              <input
                className="w-full h-7 pl-8 pr-3 rounded-sm border border-edge bg-elevated text-xs focus:outline-none focus:ring-1 focus:ring-accent"
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
              className={`flex flex-col min-h-0 overflow-hidden border-r border-edge transition-all ${
                selectedSpan ? 'flex-1' : 'w-full'
              }`}
            >
              {/* Column headers + timeline ruler */}
              <div className="flex items-stretch border-b border-edge bg-muted/20 flex-shrink-0 select-none">
                <div
                  className="flex items-center px-3 py-1.5 border-r border-edge/40 text-xs font-semibold text-muted"
                  style={{ width: '46%', flexShrink: 0 }}
                >
                  {t('telemetryExplorer.traces.detail.spans')} / {t('telemetryExplorer.traces.service')}
                </div>
                <div
                  className="flex items-center px-2 py-1.5 border-r border-edge/40 text-xs font-semibold text-muted"
                  style={{ width: '9%', flexShrink: 0 }}
                >
                  {t('telemetryExplorer.traces.duration')}
                </div>
                <div className="relative flex items-center flex-1 px-2 py-1.5 overflow-hidden">
                  {buildTimeMarks(traceData.durationMs).map(({ label, pct }) => (
                    <span
                      key={pct}
                      className="absolute text-[10px] text-muted/60"
                      style={{ left: `${pct}%`, transform: pct > 0 ? 'translateX(-50%)' : undefined }}
                    >
                      {label}
                    </span>
                  ))}
                </div>
              </div>

              {/* Span rows */}
              <div className="flex-1 overflow-y-auto">
                {visibleNodes.length === 0 && (
                  <div className="text-center py-10 text-sm text-muted">
                    {t('telemetryExplorer.traces.noTraces')}
                  </div>
                )}
                {visibleNodes.map((node) => {
                  const { span } = node;
                  const spanStartMs = new Date(span.startTime).getTime();
                  const traceDuration = traceData.durationMs || 1;
                  const offsetPct = Math.min(((spanStartMs - traceStartMs) / traceDuration) * 100, 99);
                  const widthPct = Math.max((span.durationMs / traceDuration) * 100, 0.4);
                  const isSelected = selectedSpan?.spanId === span.spanId;
                  const hasError = span.statusCode === 'Error';
                  const kindMeta = getServiceKindMeta(span.serviceKind);
                  const isCollapsed = collapsedSpans.has(span.spanId);

                  return (
                    <div
                      key={span.spanId}
                      className={`flex items-center border-b border-edge/30 hover:bg-muted/20 transition-colors cursor-pointer ${
                        isSelected ? 'bg-accent/8 border-l-2 border-l-accent' : ''
                      }`}
                      style={{ height: '28px' }}
                      onClick={() => setSelectedSpan(span)}
                    >
                      {/* Name + service column */}
                      <div
                        className="flex items-center gap-1 px-2 border-r border-edge/30 h-full min-w-0"
                        style={{ width: '46%', flexShrink: 0 }}
                      >
                        {/* Depth indent */}
                        <div style={{ width: `${node.depth * 12}px`, flexShrink: 0 }} />
                        {/* Expand/collapse toggle */}
                        <button
                          type="button"
                          className="flex-shrink-0 w-4 h-4 flex items-center justify-center rounded hover:bg-muted/30 transition-colors"
                          onClick={(e) => {
                            e.stopPropagation();
                            if (node.hasChildren) toggleCollapse(span.spanId);
                          }}
                          tabIndex={-1}
                        >
                          {node.hasChildren ? (
                            <ChevronRight
                              className={`w-3 h-3 text-muted/60 transition-transform duration-150 ${
                                isCollapsed ? '' : 'rotate-90'
                              }`}
                            />
                          ) : (
                            <span className="w-3 h-3" />
                          )}
                        </button>
                        {/* Service kind dot */}
                        <span
                          className="w-1.5 h-1.5 rounded-full flex-shrink-0"
                          style={kindMeta.dotStyle}
                        />
                        {/* Names */}
                        <span className="text-xs font-medium text-foreground truncate">{span.serviceName}</span>
                        <span className="text-xs text-muted/70 truncate ml-0.5 flex-shrink-0" style={{ maxWidth: '40%' }}>{span.operationName}</span>
                        {hasError && <XCircle className="w-3 h-3 text-destructive flex-shrink-0 ml-auto" />}
                      </div>
                      {/* Duration column */}
                      <div
                        className="flex items-center px-2 border-r border-edge/30 text-xs text-muted tabular-nums h-full"
                        style={{ width: '9%', flexShrink: 0 }}
                      >
                        {formatDuration(span.durationMs)}
                      </div>
                      {/* Timeline Gantt column */}
                      <div className="relative flex items-center flex-1 px-2 h-full overflow-hidden">
                        {/* Vertical grid lines at 20% intervals */}
                        {GRID_PCTS.map(pct => (
                          <div
                            key={pct}
                            className="absolute inset-y-0 pointer-events-none"
                            style={{ left: `${pct}%`, width: '1px', background: 'var(--t-divider)' }}
                          />
                        ))}
                        {/* Gantt bar */}
                        <div className="relative w-full h-full">
                          <div
                            className={`absolute rounded-sm ${hasError ? 'bg-danger/80' : kindMeta.barClass}`}
                            style={{
                              left: `${offsetPct}%`,
                              width: `${Math.min(widthPct, 100 - offsetPct)}%`,
                              minWidth: '4px',
                              top: '7px',
                              bottom: '7px',
                            }}
                          />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Span detail panel */}
            {selectedSpan && (
              <div className="flex-shrink-0 flex flex-col overflow-hidden border-l border-edge" style={{ width: '380px' }}>
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
                <label className="text-xs font-medium text-muted block mb-1">
                  {t('telemetryExplorer.filters.environment')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-elevated px-3 text-sm"
                  value={environment}
                  onChange={(e) => setEnvironment(e.target.value)}
                >
                  <option value="production">Production</option>
                  <option value="staging">Staging</option>
                  <option value="development">Development</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-medium text-muted block mb-1">
                  {t('telemetryExplorer.filters.service')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-elevated px-3 text-sm w-36"
                  placeholder={t('telemetryExplorer.traces.service')}
                  value={serviceName}
                  onChange={(e) => setServiceName(e.target.value)}
                />
              </div>

              <div>
                <label className="text-xs font-medium text-muted block mb-1">
                  {t('telemetryExplorer.traces.operation')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-elevated px-3 text-sm w-36"
                  placeholder={t('telemetryExplorer.traces.operation')}
                  value={operationName}
                  onChange={(e) => setOperationName(e.target.value)}
                />
              </div>

              <div>
                <label className="text-xs font-medium text-muted block mb-1">
                  {t('telemetryExplorer.traces.serviceKind')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-elevated px-3 text-sm"
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
                <label className="text-xs font-medium text-muted block mb-1">
                  {t('telemetryExplorer.traces.minDuration')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-elevated px-3 text-sm w-28"
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
                  <Activity className="w-10 h-10 text-muted mx-auto mb-3" />
                  <p className="text-sm font-medium">{t('telemetryExplorer.traces.noTraces')}</p>
                  <p className="text-xs text-muted mt-1">{t('telemetryExplorer.traces.noTracesDescription')}</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge bg-muted/30">
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.traceId')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.serviceKind')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.service')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.operation')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted" style={{ minWidth: '160px' }}>{t('telemetryExplorer.traces.duration')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.status')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.spanCount')}</th>
                        <th className="text-left px-4 py-2 font-medium text-xs text-muted">{t('telemetryExplorer.traces.startTime')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {tracesQuery.data.map((trace: TraceSummary) => {
                        const kindMeta = getServiceKindMeta(trace.rootServiceKind);
                        const durationPct = (trace.durationMs / maxTraceDuration) * 100;
                        return (
                          <tr
                            key={trace.traceId}
                            className="hover:bg-muted/50 cursor-pointer transition-colors"
                            onClick={() => setSelectedTraceId(trace.traceId)}
                          >
                            <td className="px-4 py-2.5 font-mono text-xs text-muted truncate max-w-[140px]">
                              {trace.traceId.slice(0, 12)}…
                            </td>
                            <td className="px-4 py-2.5">
                              <span className={`inline-flex items-center gap-1.5 text-xs font-medium ${kindMeta.textClass}`}>
                                {kindMeta.icon}
                                {kindMeta.label}
                              </span>
                            </td>
                            <td className="px-4 py-2.5">{trace.serviceName}</td>
                            <td className="px-4 py-2.5 text-muted truncate max-w-[180px]">{trace.operationName}</td>
                            <td className="px-4 py-2.5" style={{ minWidth: '160px' }}>
                              <div className="flex items-center gap-2">
                                <span className="tabular-nums text-xs w-14 flex-shrink-0">{formatDuration(trace.durationMs)}</span>
                                <div className="flex-1 h-1.5 rounded-full overflow-hidden" style={{ background: 'var(--t-divider)' }}>
                                  <div
                                    className={`h-full rounded-full ${trace.hasErrors ? 'bg-danger/70' : kindMeta.barClass}`}
                                    style={{ width: `${durationPct}%` }}
                                  />
                                </div>
                              </div>
                            </td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusBadge(trace.statusCode, trace.hasErrors)}>
                                {trace.hasErrors
                                  ? <><AlertTriangle className="w-3 h-3 mr-1" />{t('telemetryExplorer.traces.statusError')}</>
                                  : <><CheckCircle2 className="w-3 h-3 mr-1" />{t('telemetryExplorer.traces.statusOk')}</>
                                }
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5 text-center">{trace.spanCount}</td>
                            <td className="px-4 py-2.5 text-muted">{formatTimestamp(trace.startTime)}</td>
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
    <div className="border-b border-edge/50 last:border-b-0">
      <button
        type="button"
        className="w-full flex items-center justify-between px-3 py-2 text-xs font-semibold text-foreground hover:bg-muted/30 transition-colors"
        onClick={() => setOpen(!open)}
      >
        <span className="text-left">{title}</span>
        <ChevronDown
          className={`w-3.5 h-3.5 text-muted flex-shrink-0 transition-transform ${open ? '' : '-rotate-90'}`}
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

  const filterEntries = (obj: Record<string, string>) => {
    if (!detailSearch.trim()) return Object.entries(obj);
    const q = detailSearch.toLowerCase();
    return Object.entries(obj).filter(
      ([k, v]) => k.toLowerCase().includes(q) || v.toLowerCase().includes(q),
    );
  };

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Panel header */}
      <div className="flex items-start justify-between px-3 py-2.5 border-b border-edge flex-shrink-0 bg-card">
        <div className="min-w-0 pr-2">
          <div className="text-sm font-semibold truncate">
            Span {span.operationName}
          </div>
          <div className="flex flex-wrap items-center gap-x-1.5 mt-0.5 text-xs text-muted">
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
          <X className="w-4 h-4 text-muted" />
        </button>
      </div>

      {/* Duration + status + kind */}
      <div className="flex items-center gap-2 px-3 py-2 border-b border-edge flex-shrink-0">
        <Clock className="w-3.5 h-3.5 text-muted flex-shrink-0" />
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
      <div className="px-3 py-2 border-b border-edge flex-shrink-0">
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted pointer-events-none" />
          <input
            className="w-full h-7 pl-8 pr-3 rounded-md border border-input bg-elevated text-xs focus:outline-none focus:ring-1 focus:ring-ring"
            placeholder={t('telemetryExplorer.traces.detail.searchDetails')}
            value={detailSearch}
            onChange={(e) => setDetailSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Scrollable attribute sections */}
      <div className="flex-1 overflow-y-auto">
        <ExpandableSection title={t('telemetryExplorer.traces.detail.section.core')} defaultOpen>
          <DetailRow label={t('telemetryExplorer.traces.detail.endpoint')} value={span.operationName} />
          <DetailRow label={t('telemetryExplorer.traces.detail.responseTime')} value={formatDuration(span.durationMs)} />
          <DetailRow label={t('telemetryExplorer.traces.detail.serviceName')} value={span.serviceName} />
          {span.spanKind && (
            <DetailRow label={t('telemetryExplorer.traces.detail.spanKind')} value={span.spanKind} />
          )}
          {span.statusCode && (
            <div className="flex justify-between items-center py-0.5 text-xs">
              <span className="text-muted">{t('telemetryExplorer.traces.detail.spanStatus')}</span>
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

        {filterEntries(httpAttrs).length > 0 && (
          <ExpandableSection title="HTTP" defaultOpen>
            {filterEntries(httpAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(msgAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.context.kafka.title')} defaultOpen>
            {filterEntries(msgAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(dbAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.context.db.title')} defaultOpen>
            {filterEntries(dbAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(codeAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.codeAttributes')}>
            {filterEntries(codeAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(netAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.networking')}>
            {filterEntries(netAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(hostAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.host')}>
            {filterEntries(hostAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {filterEntries(deployAttrs).length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.section.deployment')}>
            {filterEntries(deployAttrs).map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </ExpandableSection>
        )}

        {span.events && span.events.length > 0 && (
          <ExpandableSection title={t('telemetryExplorer.traces.detail.events')}>
            {span.events.map((evt, idx) => (
              // eslint-disable-next-line react/no-array-index-key
              <div key={idx} className="border-l-2 border-primary/30 pl-2 mb-2">
                <div className="text-xs font-medium">{evt.name}</div>
                <div className="text-xs text-muted">{new Date(evt.timestamp).toLocaleString()}</div>
                {evt.attributes &&
                  Object.entries(evt.attributes).map(([k, v]) => (
                    <div key={k} className="text-xs font-mono text-muted">
                      {k}: {v}
                    </div>
                  ))}
              </div>
            ))}
          </ExpandableSection>
        )}

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
      <span className="text-muted flex-shrink-0 max-w-[140px] truncate">{label}</span>
      <span className={`text-right break-all max-w-[180px] ${mono ? 'font-mono' : ''}`}>{value}</span>
    </div>
  );
}
