/**
 * TraceExplorerPage — Explorador de traces distribuído unificado.
 *
 * Suporta todos os tipos de serviço: REST, SOAP, Kafka (Producer/Consumer),
 * Background Services, DB e gRPC. A diferenciação é feita por serviceKind
 * inferido das convenções semânticas OpenTelemetry pelo backend (SpanKindResolver).
 *
 * Funcionalidades:
 * - Filtro por tipo de serviço
 * - Waterfall hierárquico com profundidade real (parent-child)
 * - Cores e ícones por tipo de span
 * - Painel de detalhe semântico por tipo (HTTP Context, Kafka Context, etc.)
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
  CheckCircle2,
  ChevronRight,
  Clock,
  Database,
  FileCode,
  Globe,
  Layers,
  MessageSquare,
  Search,
  Server,
  XCircle,
  Zap,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
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

// ── Semantic attribute groups ─────────────────────────────────────────────────

const SEMANTIC_HTTP_KEYS = [
  'http.method', 'http.request.method', 'http.url', 'http.target',
  'http.status_code', 'http.response.status_code', 'http.request_content_length',
  'net.peer.name', 'net.peer.port', 'server.address', 'server.port',
];
const SEMANTIC_KAFKA_KEYS = [
  'messaging.system', 'messaging.topic', 'messaging.destination.name',
  'messaging.operation', 'messaging.kafka.consumer_group', 'messaging.kafka.partition',
  'messaging.message_id',
];
const SEMANTIC_SOAP_KEYS = [
  'rpc.system', 'rpc.service', 'rpc.method', 'rpc.grpc.status_code', 'net.peer.name',
];
const SEMANTIC_DB_KEYS = [
  'db.system', 'db.name', 'db.operation', 'db.statement', 'db.sql.table',
  'db.user', 'net.peer.name', 'net.peer.port',
];
const SEMANTIC_BACKGROUND_KEYS = [
  'code.namespace', 'code.function', 'code.filepath',
  'thread.id', 'thread.name', 'process.pid', 'process.executable.name',
];
const SEMANTIC_GRPC_KEYS = [
  'rpc.system', 'rpc.service', 'rpc.method', 'rpc.grpc.status_code',
  'net.peer.name', 'net.peer.port',
];
const SEMANTIC_MESSAGING_KEYS = [
  'messaging.system', 'messaging.destination.name', 'messaging.operation',
  'messaging.protocol', 'messaging.url', 'messaging.message_id',
];

function semanticKeysForKind(kind: string | undefined): string[] {
  switch (kind) {
    case SK_REST: return SEMANTIC_HTTP_KEYS;
    case SK_SOAP: return SEMANTIC_SOAP_KEYS;
    case SK_KAFKA: return SEMANTIC_KAFKA_KEYS;
    case SK_DB: return SEMANTIC_DB_KEYS;
    case SK_BACKGROUND: return SEMANTIC_BACKGROUND_KEYS;
    case SK_GRPC: return SEMANTIC_GRPC_KEYS;
    case SK_MESSAGING: return SEMANTIC_MESSAGING_KEYS;
    default: return [];
  }
}

function contextTitleForKind(kind: string | undefined): string {
  switch (kind) {
    case SK_REST: return 'telemetryExplorer.context.http.title';
    case SK_SOAP: return 'telemetryExplorer.context.soap.title';
    case SK_KAFKA: return 'telemetryExplorer.context.kafka.title';
    case SK_DB: return 'telemetryExplorer.context.db.title';
    case SK_BACKGROUND: return 'telemetryExplorer.context.background.title';
    case SK_GRPC: return 'telemetryExplorer.context.soap.title';
    case SK_MESSAGING: return 'telemetryExplorer.context.messaging.title';
    default: return 'telemetryExplorer.context.generic.title';
  }
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

  const { sortedSpans, depthMap } = useMemo(() => {
    if (!detailQuery.data?.spans) return { sortedSpans: [], depthMap: new Map<string, number>() };
    const sorted = [...detailQuery.data.spans].sort(
      (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
    );
    return { sortedSpans: sorted, depthMap: buildDepthMap(sorted) };
  }, [detailQuery.data]);

  // ── Detail view ─────────────────────────────────────────────────────────────
  if (selectedTraceId) {
    return (
      <PageContainer>
        <div className="mb-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => { setSelectedTraceId(null); setSelectedSpan(null); }}
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            {t('telemetryExplorer.traces.title')}
          </Button>
        </div>

        <PageHeader
          title={t('telemetryExplorer.traces.detail.title')}
          subtitle={selectedTraceId}
          icon={<Layers className="w-6 h-6 text-primary" />}
        />

        {detailQuery.isLoading && <PageLoadingState message={t('telemetryExplorer.loading')} />}
        {detailQuery.isError && <PageErrorState message={t('telemetryExplorer.error')} />}

        {detailQuery.data && (
          <PageSection>
            <div className="flex flex-wrap gap-4 mb-6">
              <Card className="flex-1 min-w-[160px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.totalDuration')}</div>
                  <div className="text-2xl font-bold">{formatDuration(detailQuery.data.durationMs)}</div>
                </CardBody>
              </Card>
              <Card className="flex-1 min-w-[160px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.spans')}</div>
                  <div className="text-2xl font-bold">{detailQuery.data.spans.length}</div>
                </CardBody>
              </Card>
              <Card className="flex-1 min-w-[160px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.services')}</div>
                  <div className="text-2xl font-bold">{detailQuery.data.services.length}</div>
                  <div className="text-xs text-muted-foreground mt-1">{detailQuery.data.services.join(', ')}</div>
                </CardBody>
              </Card>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
              <div className="lg:col-span-2">
                <Card>
                  <CardHeader>
                    <h3 className="text-sm font-semibold">{t('telemetryExplorer.traces.detail.waterfall')}</h3>
                  </CardHeader>
                  <CardBody className="p-0">
                    <div className="divide-y divide-border">
                      {sortedSpans.map((span) => {
                        const traceStart = new Date(sortedSpans[0]?.startTime ?? span.startTime).getTime();
                        const traceDuration = detailQuery.data!.durationMs || 1;
                        const spanStart = new Date(span.startTime).getTime();
                        const offsetPct = Math.min(((spanStart - traceStart) / traceDuration) * 100, 99);
                        const widthPct = Math.max((span.durationMs / traceDuration) * 100, 0.5);
                        const depth = depthMap.get(span.spanId) ?? 0;
                        const isSelected = selectedSpan?.spanId === span.spanId;
                        const hasError = span.statusCode === 'Error';
                        const kindMeta = getServiceKindMeta(span.serviceKind);

                        return (
                          <button
                            key={span.spanId}
                            type="button"
                            className={`w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors ${isSelected ? 'bg-primary/10' : ''}`}
                            onClick={() => setSelectedSpan(span)}
                          >
                            <div className="flex items-center gap-1.5">
                              <div style={{ width: `${depth * 14}px`, flexShrink: 0 }} />
                              <ChevronRight className="w-3 h-3 text-muted-foreground flex-shrink-0 opacity-60" />
                              <span className={kindMeta.textClass} title={kindMeta.label}>
                                {kindMeta.icon}
                              </span>
                              <span className="text-xs font-medium truncate max-w-[140px]">{span.serviceName}</span>
                              <span className="text-xs text-muted-foreground truncate max-w-[140px]">{span.operationName}</span>
                              {hasError && <XCircle className="w-3 h-3 text-destructive flex-shrink-0 ml-auto" />}
                              <span className="text-xs text-muted-foreground ml-auto flex-shrink-0">{formatDuration(span.durationMs)}</span>
                            </div>
                            <div className="mt-1 h-1.5 relative bg-muted/30 rounded-sm overflow-hidden">
                              <div
                                className={`absolute h-full rounded-sm ${hasError ? 'bg-destructive/70' : kindMeta.barClass}`}
                                style={{ left: `${offsetPct}%`, width: `${Math.min(widthPct, 100 - offsetPct)}%` }}
                              />
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  </CardBody>
                </Card>
              </div>

              <div>
                <Card>
                  <CardHeader>
                    <h3 className="text-sm font-semibold">{t('telemetryExplorer.traces.detail.spanDetail')}</h3>
                  </CardHeader>
                  <CardBody>
                    {selectedSpan ? (
                      <SpanDetailPanel span={selectedSpan} t={t} />
                    ) : (
                      <div className="text-sm text-muted-foreground text-center py-8">
                        {t('telemetryExplorer.traces.detail.selectSpanHint')}
                      </div>
                    )}
                  </CardBody>
                </Card>
              </div>
            </div>
          </PageSection>
        )}
      </PageContainer>
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

// ── Span Detail Panel ──────────────────────────────────────────────────────────

interface TFunc {
  (key: string): string;
}

interface SpanDetailPanelProps {
  span: SpanDetail;
  t: TFunc;
}

function SpanDetailPanel({ span, t }: SpanDetailPanelProps) {
  const kindMeta = getServiceKindMeta(span.serviceKind);
  const semanticKeys = semanticKeysForKind(span.serviceKind);
  const attrs = span.spanAttributes ?? {};

  const semanticEntries = semanticKeys
    .filter((k) => k in attrs)
    .map((k) => [k, attrs[k]] as [string, string]);

  const contextTitleKey = contextTitleForKind(span.serviceKind);

  return (
    <div className="space-y-2 text-sm">
      <DetailRow label={t('telemetryExplorer.traces.detail.operationName')} value={span.operationName} />
      <DetailRow label={t('telemetryExplorer.traces.detail.serviceName')} value={span.serviceName} />
      <DetailRow label={t('telemetryExplorer.traces.detail.spanId')} value={span.spanId} mono />
      {span.parentSpanId && (
        <DetailRow label={t('telemetryExplorer.traces.detail.parentSpan')} value={span.parentSpanId} mono />
      )}
      {span.spanKind && (
        <DetailRow label={t('telemetryExplorer.traces.detail.spanKind')} value={span.spanKind} />
      )}
      <div className="flex justify-between gap-2">
        <span className="text-muted-foreground flex-shrink-0">{t('telemetryExplorer.traces.serviceKind')}</span>
        <span className={`inline-flex items-center gap-1 text-xs font-medium ${kindMeta.textClass}`}>
          {kindMeta.icon} {kindMeta.label}
        </span>
      </div>
      <DetailRow label={t('telemetryExplorer.traces.detail.duration')} value={formatDuration(span.durationMs)} />
      <DetailRow label={t('telemetryExplorer.traces.detail.startTime')} value={formatTimestamp(span.startTime)} />
      <DetailRow label={t('telemetryExplorer.traces.detail.endTime')} value={formatTimestamp(span.endTime)} />
      {span.statusCode && (
        <DetailRow label={t('telemetryExplorer.traces.detail.statusCode')} value={span.statusCode} />
      )}
      {span.statusMessage && (
        <DetailRow label={t('telemetryExplorer.traces.detail.statusMessage')} value={span.statusMessage} />
      )}

      {semanticEntries.length > 0 && (
        <div className="mt-3">
          <div className="text-xs font-semibold text-muted-foreground mb-1 flex items-center gap-1">
            {kindMeta.icon}
            {t(contextTitleKey)}
          </div>
          <div className="bg-muted/20 rounded-md px-2 py-1.5 space-y-1">
            {semanticEntries.map(([k, v]) => (
              <DetailRow key={k} label={k} value={v} mono />
            ))}
          </div>
        </div>
      )}

      {Object.keys(attrs).length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
            {t('telemetryExplorer.traces.detail.spanAttributes')}
          </div>
          {Object.entries(attrs).map(([k, v]) => (
            <DetailRow key={k} label={k} value={v} mono />
          ))}
        </div>
      )}

      {span.resourceAttributes && Object.keys(span.resourceAttributes).length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
            {t('telemetryExplorer.traces.detail.resourceAttributes')}
          </div>
          {Object.entries(span.resourceAttributes).map(([k, v]) => (
            <DetailRow key={k} label={k} value={v} mono />
          ))}
        </div>
      )}

      {span.events && span.events.length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
            {t('telemetryExplorer.traces.detail.events')}
          </div>
          {span.events.map((evt, idx) => (
            <div key={idx} className="border-l-2 border-border pl-2 mb-2">
              <div className="text-xs font-medium">{evt.name}</div>
              <div className="text-xs text-muted-foreground">{formatTimestamp(evt.timestamp)}</div>
              {evt.attributes &&
                Object.entries(evt.attributes).map(([k, v]) => (
                  <div key={k} className="text-xs font-mono text-muted-foreground">{k}: {v}</div>
                ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ── Detail Row ────────────────────────────────────────────────────────────────

function DetailRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-2">
      <span className="text-muted-foreground flex-shrink-0 max-w-[140px] truncate">{label}</span>
      <span className={`text-right truncate max-w-[180px] ${mono ? 'font-mono text-xs' : ''}`}>{value}</span>
    </div>
  );
}
