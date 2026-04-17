/**
 * TraceExplorerPage — Distributed trace search with span hierarchy.
 *
 * Provides real-time trace exploration with filtering by service, operation,
 * duration and error status. Clicking a trace shows full span waterfall.
 * Connected to /api/v1/telemetry/traces and /api/v1/telemetry/traces/:traceId.
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
  Filter,
  Layers,
  Search,
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
  queryTraces,
  getTraceDetail,
  type TraceSummary,
  type TraceDetail,
  type SpanDetail,
} from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Helpers ──────────────────────────────────────────────────────────────────

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
    fractionalSecondDigits: 3,
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
  return {
    from: oneHourAgo.toISOString(),
    until: now.toISOString(),
  };
}

// ── Component ────────────────────────────────────────────────────────────────

export function TraceExplorerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const defaults = getDefaultTimeRange();

  // Filter state
  const [environment, setEnvironment] = useState(activeEnvironmentId ?? 'production');
  const [serviceName, setServiceName] = useState('');
  const [operationName, setOperationName] = useState('');
  const [minDurationMs, setMinDurationMs] = useState('');
  const [errorsOnly, setErrorsOnly] = useState(false);
  const [from, setFrom] = useState(defaults.from);
  const [until, setUntil] = useState(defaults.until);
  const [limit, setLimit] = useState(50);

  // Detail view state
  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(null);
  const [selectedSpan, setSelectedSpan] = useState<SpanDetail | null>(null);

  // Traces list query
  const tracesQuery = useQuery({
    queryKey: ['telemetry', 'traces', environment, from, until, serviceName, operationName, minDurationMs, errorsOnly, limit],
    queryFn: () =>
      queryTraces({
        environment,
        from,
        until,
        serviceName: serviceName || undefined,
        operationName: operationName || undefined,
        minDurationMs: minDurationMs ? parseFloat(minDurationMs) : undefined,
        hasErrors: errorsOnly || undefined,
        limit,
      }),
    enabled: !!environment && !!from && !!until,
  });

  // Trace detail query
  const detailQuery = useQuery({
    queryKey: ['telemetry', 'trace-detail', selectedTraceId],
    queryFn: () => getTraceDetail(selectedTraceId!),
    enabled: !!selectedTraceId,
  });

  // Build span tree for waterfall
  const spanTree = useMemo(() => {
    if (!detailQuery.data?.spans) return [];
    const spans = [...detailQuery.data.spans].sort(
      (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
    );
    return spans;
  }, [detailQuery.data]);

  // ── Detail view ────────────────────────────────────────────────────────
  if (selectedTraceId) {
    return (
      <PageContainer>
        <div className="mb-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setSelectedTraceId(null);
              setSelectedSpan(null);
            }}
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
            {/* Trace summary bar */}
            <div className="flex flex-wrap gap-4 mb-6">
              <Card className="flex-1 min-w-[200px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.totalDuration')}</div>
                  <div className="text-2xl font-bold">{formatDuration(detailQuery.data.durationMs)}</div>
                </CardBody>
              </Card>
              <Card className="flex-1 min-w-[200px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.spans')}</div>
                  <div className="text-2xl font-bold">{detailQuery.data.spans.length}</div>
                </CardBody>
              </Card>
              <Card className="flex-1 min-w-[200px]">
                <CardBody className="p-4">
                  <div className="text-xs text-muted-foreground">{t('telemetryExplorer.traces.detail.services')}</div>
                  <div className="text-2xl font-bold">{detailQuery.data.services.length}</div>
                  <div className="text-xs text-muted-foreground mt-1">
                    {detailQuery.data.services.join(', ')}
                  </div>
                </CardBody>
              </Card>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
              {/* Span waterfall */}
              <div className="lg:col-span-2">
                <Card>
                  <CardHeader>
                    <h3 className="text-sm font-semibold">{t('telemetryExplorer.traces.detail.waterfall')}</h3>
                  </CardHeader>
                  <CardBody className="p-0">
                    <div className="divide-y divide-border">
                      {spanTree.map((span) => {
                        const traceStart = new Date(spanTree[0]?.startTime ?? span.startTime).getTime();
                        const traceDuration = detailQuery.data!.durationMs || 1;
                        const spanStart = new Date(span.startTime).getTime();
                        const offsetPercent = ((spanStart - traceStart) / traceDuration) * 100;
                        const widthPercent = Math.max((span.durationMs / traceDuration) * 100, 0.5);
                        const depth = span.parentSpanId ? 1 : 0;
                        const isSelected = selectedSpan?.spanId === span.spanId;
                        const hasError = span.statusCode === 'Error';

                        return (
                          <button
                            key={span.spanId}
                            className={`w-full text-left px-3 py-2 hover:bg-muted/50 transition-colors ${isSelected ? 'bg-primary/10' : ''}`}
                            onClick={() => setSelectedSpan(span)}
                            type="button"
                          >
                            <div className="flex items-center gap-2">
                              <div style={{ width: `${depth * 16}px` }} />
                              <ChevronRight className={`w-3 h-3 text-muted-foreground ${depth > 0 ? 'opacity-60' : ''}`} />
                              <span className="text-xs font-medium truncate max-w-[200px]">
                                {span.serviceName}
                              </span>
                              <span className="text-xs text-muted-foreground truncate max-w-[200px]">
                                {span.operationName}
                              </span>
                              {hasError && <XCircle className="w-3 h-3 text-destructive flex-shrink-0" />}
                              <span className="text-xs text-muted-foreground ml-auto flex-shrink-0">
                                {formatDuration(span.durationMs)}
                              </span>
                            </div>
                            {/* Waterfall bar */}
                            <div className="mt-1 h-2 relative bg-muted/30 rounded-sm overflow-hidden">
                              <div
                                className={`absolute h-full rounded-sm ${hasError ? 'bg-destructive/70' : 'bg-primary/60'}`}
                                style={{
                                  left: `${Math.min(offsetPercent, 99)}%`,
                                  width: `${Math.min(widthPercent, 100 - offsetPercent)}%`,
                                }}
                              />
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  </CardBody>
                </Card>
              </div>

              {/* Span detail panel */}
              <div>
                <Card>
                  <CardHeader>
                    <h3 className="text-sm font-semibold">{t('telemetryExplorer.traces.detail.spanDetail')}</h3>
                  </CardHeader>
                  <CardBody>
                    {selectedSpan ? (
                      <div className="space-y-3 text-sm">
                        <DetailRow label={t('telemetryExplorer.traces.detail.operationName')} value={selectedSpan.operationName} />
                        <DetailRow label={t('telemetryExplorer.traces.detail.serviceName')} value={selectedSpan.serviceName} />
                        <DetailRow label={t('telemetryExplorer.traces.detail.spanId')} value={selectedSpan.spanId} mono />
                        {selectedSpan.parentSpanId && (
                          <DetailRow label={t('telemetryExplorer.traces.detail.parentSpan')} value={selectedSpan.parentSpanId} mono />
                        )}
                        <DetailRow label={t('telemetryExplorer.traces.detail.duration')} value={formatDuration(selectedSpan.durationMs)} />
                        <DetailRow label={t('telemetryExplorer.traces.detail.startTime')} value={formatTimestamp(selectedSpan.startTime)} />
                        <DetailRow label={t('telemetryExplorer.traces.detail.endTime')} value={formatTimestamp(selectedSpan.endTime)} />
                        {selectedSpan.statusCode && (
                          <DetailRow label={t('telemetryExplorer.traces.detail.statusCode')} value={selectedSpan.statusCode} />
                        )}
                        {selectedSpan.statusMessage && (
                          <DetailRow label={t('telemetryExplorer.traces.detail.statusMessage')} value={selectedSpan.statusMessage} />
                        )}

                        {selectedSpan.spanAttributes && Object.keys(selectedSpan.spanAttributes).length > 0 && (
                          <div>
                            <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
                              {t('telemetryExplorer.traces.detail.spanAttributes')}
                            </div>
                            {Object.entries(selectedSpan.spanAttributes).map(([k, v]) => (
                              <DetailRow key={k} label={k} value={v} mono />
                            ))}
                          </div>
                        )}

                        {selectedSpan.resourceAttributes && Object.keys(selectedSpan.resourceAttributes).length > 0 && (
                          <div>
                            <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
                              {t('telemetryExplorer.traces.detail.resourceAttributes')}
                            </div>
                            {Object.entries(selectedSpan.resourceAttributes).map(([k, v]) => (
                              <DetailRow key={k} label={k} value={v} mono />
                            ))}
                          </div>
                        )}

                        {selectedSpan.events && selectedSpan.events.length > 0 && (
                          <div>
                            <div className="text-xs font-semibold text-muted-foreground mt-3 mb-1">
                              {t('telemetryExplorer.traces.detail.events')}
                            </div>
                            {selectedSpan.events.map((evt, idx) => (
                              <div key={idx} className="border-l-2 border-border pl-2 mb-2">
                                <div className="text-xs font-medium">{evt.name}</div>
                                <div className="text-xs text-muted-foreground">{formatTimestamp(evt.timestamp)}</div>
                                {evt.attributes && Object.entries(evt.attributes).map(([k, v]) => (
                                  <div key={k} className="text-xs font-mono text-muted-foreground">
                                    {k}: {v}
                                  </div>
                                ))}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="text-sm text-muted-foreground text-center py-8">
                        {t('telemetryExplorer.traces.detail.spanDetail')}
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

  // ── List view ──────────────────────────────────────────────────────────
  return (
    <PageContainer>
      <PageHeader
        title={t('telemetryExplorer.traces.title')}
        subtitle={t('telemetryExplorer.traces.subtitle')}
        icon={<Activity className="w-6 h-6 text-primary" />}
      />

      {/* Filters */}
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
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-40"
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
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-40"
                  placeholder={t('telemetryExplorer.traces.operation')}
                  value={operationName}
                  onChange={(e) => setOperationName(e.target.value)}
                />
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

      {/* Results */}
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
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.service')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.operation')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.duration')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.status')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.spanCount')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.traces.startTime')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {tracesQuery.data.map((trace: TraceSummary) => (
                        <tr
                          key={trace.traceId}
                          className="hover:bg-muted/50 cursor-pointer transition-colors"
                          onClick={() => setSelectedTraceId(trace.traceId)}
                        >
                          <td className="px-4 py-2 font-mono text-xs truncate max-w-[180px]">{trace.traceId}</td>
                          <td className="px-4 py-2">{trace.serviceName}</td>
                          <td className="px-4 py-2 text-muted-foreground truncate max-w-[200px]">{trace.operationName}</td>
                          <td className="px-4 py-2">{formatDuration(trace.durationMs)}</td>
                          <td className="px-4 py-2">
                            <Badge variant={statusBadge(trace.statusCode, trace.hasErrors)}>
                              {trace.hasErrors ? (
                                <><AlertTriangle className="w-3 h-3 mr-1" /> Error</>
                              ) : (
                                <><CheckCircle2 className="w-3 h-3 mr-1" /> Ok</>
                              )}
                            </Badge>
                          </td>
                          <td className="px-4 py-2 text-center">{trace.spanCount}</td>
                          <td className="px-4 py-2 text-muted-foreground">{formatTimestamp(trace.startTime)}</td>
                        </tr>
                      ))}
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

// ── Detail Row helper ────────────────────────────────────────────────────────

function DetailRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-2">
      <span className="text-muted-foreground flex-shrink-0">{label}</span>
      <span className={`text-right truncate max-w-[200px] ${mono ? 'font-mono text-xs' : ''}`}>{value}</span>
    </div>
  );
}
