/**
 * Cliente API para endpoints de Telemetria (logs, traces, métricas).
 * Consome /api/v1/telemetry/* exposto pelo TelemetryEndpointModule.
 *
 * Utilizado pelas páginas TraceExplorerPage e LogExplorerPage
 * para pesquisa, filtragem e correlação de sinais de observabilidade.
 */
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

export interface LogEntry {
  timestamp: string;
  environment: string;
  serviceName: string;
  applicationName?: string;
  moduleName?: string;
  level: string;
  message: string;
  exception?: string;
  traceId?: string;
  spanId?: string;
  correlationId?: string;
  hostName?: string;
  containerName?: string;
  tenantId?: string;
  attributes?: Record<string, string>;
}

export interface TraceSummary {
  traceId: string;
  serviceName: string;
  operationName: string;
  startTime: string;
  durationMs: number;
  statusCode?: string;
  environment: string;
  spanCount: number;
  hasErrors: boolean;
  /** Tipo de serviço inferido do span raiz (REST, SOAP, Kafka, Background, DB, gRPC, Unknown). */
  rootServiceKind: string;
}

export interface SpanEvent {
  name: string;
  timestamp: string;
  attributes?: Record<string, string>;
}

export interface SpanDetail {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  serviceName: string;
  operationName: string;
  startTime: string;
  endTime: string;
  durationMs: number;
  statusCode?: string;
  statusMessage?: string;
  environment: string;
  /** SpanKind OTel: Internal, Server, Client, Producer, Consumer. */
  spanKind?: string;
  /** Tipo de serviço inferido: REST, SOAP, Kafka, Background, DB, gRPC, Unknown. */
  serviceKind?: string;
  resourceAttributes?: Record<string, string>;
  spanAttributes?: Record<string, string>;
  events?: SpanEvent[];
}

export interface TraceDetail {
  traceId: string;
  spans: SpanDetail[];
  durationMs: number;
  services: string[];
}

export interface TelemetryMetricPoint {
  timestamp: string;
  metricName: string;
  value: number;
  serviceName: string;
  environment: string;
  labels?: Record<string, string>;
}

export interface ErrorFrequency {
  errorMessage: string;
  count: number;
  serviceName: string;
  lastSeen: string;
  level: string;
}

export interface LatencyComparison {
  serviceName: string;
  environmentA: string;
  environmentB: string;
  latencyP50MsA: number;
  latencyP50MsB: number;
  latencyP95MsA: number;
  latencyP95MsB: number;
  latencyP99MsA: number;
  latencyP99MsB: number;
  driftPercentP95: number;
}

export interface CorrelatedSignals {
  traceId: string;
  logs: LogEntry[];
  spans: SpanDetail[];
}

export interface TelemetryHealthStatus {
  provider: string;
  healthy: boolean;
}

// ── Query Filters ──────────────────────────────────────────────────────────

export interface LogQueryParams {
  environment: string;
  from: string;
  until: string;
  serviceName?: string;
  level?: string;
  messageContains?: string;
  traceId?: string;
  limit?: number;
}

export interface TraceQueryParams {
  environment: string;
  from: string;
  until: string;
  serviceName?: string;
  operationName?: string;
  minDurationMs?: number;
  hasErrors?: boolean;
  /** Filtrar por tipo de serviço (REST, SOAP, Kafka, Background, DB, gRPC). */
  serviceKind?: string;
  limit?: number;
}

export interface MetricQueryParams {
  environment: string;
  from: string;
  until: string;
  metricName: string;
  serviceName?: string;
}

// ── API Functions ──────────────────────────────────────────────────────────

export async function queryLogs(params: LogQueryParams): Promise<LogEntry[]> {
  const { data } = await client.get<LogEntry[]>('/telemetry/logs', { params });
  return data;
}

export async function queryTraces(params: TraceQueryParams): Promise<TraceSummary[]> {
  const { data } = await client.get<TraceSummary[]>('/telemetry/traces', { params });
  return data;
}

export async function getTraceDetail(traceId: string): Promise<TraceDetail> {
  const { data } = await client.get<TraceDetail>(`/telemetry/traces/${traceId}`);
  return data;
}

export async function queryMetrics(params: MetricQueryParams): Promise<TelemetryMetricPoint[]> {
  const { data } = await client.get<TelemetryMetricPoint[]>('/telemetry/metrics', { params });
  return data;
}

export async function getTopErrors(
  environment: string,
  from: string,
  until: string,
  top = 10,
): Promise<ErrorFrequency[]> {
  const { data } = await client.get<ErrorFrequency[]>('/telemetry/errors/top', {
    params: { environment, from, until, top },
  });
  return data;
}

export async function compareLatency(
  serviceName: string,
  environmentA: string,
  environmentB: string,
  from: string,
  until: string,
): Promise<LatencyComparison> {
  const { data } = await client.get<LatencyComparison>('/telemetry/latency/compare', {
    params: { serviceName, environmentA, environmentB, from, until },
  });
  return data;
}

export async function correlateByTraceId(traceId: string): Promise<CorrelatedSignals> {
  const { data } = await client.get<CorrelatedSignals>(`/telemetry/correlate/${traceId}`);
  return data;
}

export async function getTelemetryHealth(): Promise<TelemetryHealthStatus> {
  const { data } = await client.get<TelemetryHealthStatus>('/telemetry/health');
  return data;
}

// ── SRE Dashboard types ───────────────────────────────────────────────────────

export interface SreSummary {
  problems: { open: number; total: number };
  slo: { errorCompliancePct: number; latencyCompliancePct: number };
  traffic: { requestCount: number; queryCount: number };
  latency: { requestAvgMs: number; queryAvgMs: number };
  errors: { http5xx: number; http4xx: number; queryErrors: number; logErrors: number };
}

export interface SreTimeSeriesPoint {
  timestamp: string;
  value: number;
}

export interface SreTimeSeries {
  requests: SreTimeSeriesPoint[];
  requestLatency: SreTimeSeriesPoint[];
  requestErrors: SreTimeSeriesPoint[];
  queries: SreTimeSeriesPoint[];
  queryLatency: SreTimeSeriesPoint[];
  queryErrors: SreTimeSeriesPoint[];
}

export interface SreTopRequest {
  service: string;
  request: string;
  count: number;
  avgLatencyMs: number;
  errors: number;
}

export interface SreTopQuery {
  database: string;
  query: string;
  count: number;
  avgLatencyMs: number;
}

export interface SreParams {
  environment: string;
  from: string;
  until: string;
  serviceId?: string;
}

export async function getSreSummary(params: SreParams): Promise<SreSummary> {
  const { data } = await client.get<SreSummary>('/telemetry/sre/summary', { params });
  return data;
}

export async function getSreTimeSeries(params: SreParams): Promise<SreTimeSeries> {
  const { data } = await client.get<SreTimeSeries>('/telemetry/sre/timeseries', { params });
  return data;
}

export async function getSreTopRequests(params: SreParams & { top?: number }): Promise<SreTopRequest[]> {
  const { data } = await client.get<SreTopRequest[]>('/telemetry/sre/top-requests', { params });
  return data;
}

export async function getSreTopQueries(params: SreParams & { top?: number }): Promise<SreTopQuery[]> {
  const { data } = await client.get<SreTopQuery[]>('/telemetry/sre/top-queries', { params });
  return data;
}
