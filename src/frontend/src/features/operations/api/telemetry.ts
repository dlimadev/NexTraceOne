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

// ── Request Explorer types ────────────────────────────────────────────────────

export type RequestViewMode = 'requests' | 'spans';
export type RequestStatus = 'Success' | 'Failure';
export type SpanStatusFilter = 'Ok' | 'Error';
export type SpanKindFilter = 'client' | 'server' | 'consumer' | 'producer' | 'internal' | 'link';
export type ChartMode = 'timeseries' | 'histogram';

export interface RequestSpan {
  startTime: string;
  endpoint: string;
  service: string;
  durationMs: number;
  requestStatus: RequestStatus;
  httpCode?: number;
  processGroup?: string;
  k8sWorkload?: string;
  k8sNamespace?: string;
  spanKind?: string;
  spanStatus?: string;
  traceId?: string;
  spanId?: string;
}

export interface RequestHistogramBucket {
  durationLabel: string;
  successCount: number;
  failureCount: number;
}

export interface RequestFacets {
  services: string[];
  endpoints: string[];
  processGroups: string[];
  k8sNamespaces: string[];
  k8sWorkloads: string[];
}

export interface RequestsResult {
  items: RequestSpan[];
  total: number;
  page: number;
  pageSize: number;
  histogram: RequestHistogramBucket[];
}

export interface RequestQueryParams {
  environment: string;
  from: string;
  until: string;
  viewMode?: RequestViewMode;
  service?: string;
  endpoint?: string;
  requestStatus?: RequestStatus;
  spanStatus?: SpanStatusFilter;
  spanKind?: SpanKindFilter;
  httpMethod?: string;
  durationMin?: number;
  durationMax?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export async function getRequests(params: RequestQueryParams): Promise<RequestsResult> {
  const { data } = await client.get<RequestsResult>('/telemetry/requests', { params });
  return data;
}

export async function getRequestFacets(
  environment: string,
  from: string,
  until: string,
): Promise<RequestFacets> {
  const { data } = await client.get<RequestFacets>('/telemetry/requests/facets', {
    params: { environment, from, until },
  });
  return data;
}

// ── Profiling Explorer ────────────────────────────────────────────────────────

export interface ProfilingSession {
  id: string;
  serviceName: string;
  version: string;
  environment: string;
  cpuPercent: number;
  memoryMb: number;
  heapMb: number;
  sampleCount: number;
  durationMs: number;
  deployCorrelated: boolean;
  deployId?: string;
  capturedAt: string;
  profileType: 'cpu' | 'memory' | 'heap';
}

export interface ProfilingParams {
  environment: string;
  from: string;
  until: string;
  service?: string;
  version?: string;
  profileType?: string;
}

export async function getProfilingSessions(params: ProfilingParams): Promise<ProfilingSession[]> {
  const { data } = await client.get<ProfilingSession[]>('/telemetry/profiling/sessions', { params });
  return data;
}

// ── Error Tracking ────────────────────────────────────────────────────────────

export type ErrorGroupStatus = 'new' | 'regressing' | 'resolved' | 'ignored';

export interface ErrorGroup {
  id: string;
  fingerprint: string;
  message: string;
  serviceName: string;
  count: number;
  affectedUsers: number;
  status: ErrorGroupStatus;
  firstSeen: string;
  lastSeen: string;
  deployCorrelated: boolean;
  deployId?: string;
  environment: string;
  stackTraceSummary?: string;
}

export interface ErrorGroupParams {
  environment: string;
  from: string;
  until: string;
  service?: string;
  status?: ErrorGroupStatus;
}

export async function getErrorGroups(params: ErrorGroupParams): Promise<ErrorGroup[]> {
  const { data } = await client.get<ErrorGroup[]>('/telemetry/errors/groups', { params });
  return data;
}

// ── Synthetic Monitoring ──────────────────────────────────────────────────────

export type ProbeType = 'httpSingle' | 'httpMultiStep';
export type ProbeStatus = 'healthy' | 'degraded' | 'down';
export type ContractValidationStatus = 'pass' | 'fail' | 'skipped';

export interface SyntheticProbe {
  id: string;
  name: string;
  type: ProbeType;
  target: string;
  status: ProbeStatus;
  uptimePercent: number;
  lastCheck: string;
  lastResult: string;
  schedule: string;
  contractValidation: ContractValidationStatus;
  environment: string;
}

export interface SyntheticParams {
  environment: string;
  from: string;
  until: string;
}

export async function getSyntheticProbes(params: SyntheticParams): Promise<SyntheticProbe[]> {
  const { data } = await client.get<SyntheticProbe[]>('/telemetry/synthetic/probes', { params });
  return data;
}

// ── DB Performance Explorer ───────────────────────────────────────────────────

export type DbSortMode = 'totalTime' | 'executions' | 'lockWait';

export interface SlowQuery {
  id: string;
  fingerprint: string;
  database: string;
  avgDurationMs: number;
  maxDurationMs: number;
  executionCount: number;
  totalTimeMs: number;
  lockWaitMs: number;
  hasIndexMiss: boolean;
  indexMissCount: number;
  recommendation?: string;
  environment: string;
}

export interface DbExplorerParams {
  environment: string;
  from: string;
  until: string;
  sortBy?: DbSortMode;
}

export async function getSlowQueries(params: DbExplorerParams): Promise<SlowQuery[]> {
  const { data } = await client.get<SlowQuery[]>('/telemetry/db/slow-queries', { params });
  return data;
}

// ── SLO Burn Rate ─────────────────────────────────────────────────────────────

export type SloBurnStatus = 'critical' | 'warning' | 'healthy';

export interface SloBurnRate {
  id: string;
  sloName: string;
  serviceName: string;
  budgetRemainingPercent: number;
  burnRate1h: number;
  burnRate6h: number;
  burnRate24h: number;
  burnRate72h: number;
  depletedInHours?: number;
  alertThreshold: number;
  status: SloBurnStatus;
  environment: string;
}

export interface SloBurnParams {
  environment: string;
  from: string;
  until: string;
}

export async function getSloBurnRates(params: SloBurnParams): Promise<SloBurnRate[]> {
  const { data } = await client.get<SloBurnRate[]>('/telemetry/slo/burn-rates', { params });
  return data;
}

// ── Post-Incident Learning ────────────────────────────────────────────────────

export type PostMortemStatus = 'draft' | 'review' | 'published' | 'archived';

export interface PostMortem {
  id: string;
  title: string;
  incidentId: string;
  incidentTitle: string;
  status: PostMortemStatus;
  author: string;
  severity: string;
  actionItemsCount: number;
  openActionItemsCount: number;
  createdAt: string;
  publishedAt?: string;
  patternCount: number;
  environment: string;
}

export interface PostIncidentParams {
  environment: string;
  from: string;
  until: string;
  status?: PostMortemStatus;
}

export async function getPostMortems(params: PostIncidentParams): Promise<PostMortem[]> {
  const { data } = await client.get<PostMortem[]>('/operations/post-mortems', { params });
  return data;
}

// ── On-Call Schedule ──────────────────────────────────────────────────────────

export type RotationType = 'weekly' | 'followTheSun' | 'custom';

export interface OnCallSchedule {
  id: string;
  name: string;
  teamName: string;
  serviceName: string;
  currentOnCall: string;
  nextOnCall: string;
  rotationType: RotationType;
  timezone: string;
  escalationLevels: number;
  activeOverrides: number;
  environment: string;
}

export interface OnCallParams {
  environment: string;
  from: string;
  until: string;
}

export async function getOnCallSchedules(params: OnCallParams): Promise<OnCallSchedule[]> {
  const { data } = await client.get<OnCallSchedule[]>('/operations/on-call/schedules', { params });
  return data;
}

// ── API Regression ────────────────────────────────────────────────────────────

export type RegressionStatus = 'regressed' | 'improved' | 'stable';
export type ChangeConfidence = 'high' | 'medium' | 'low';

export interface ApiRegressionEntry {
  id: string;
  endpoint: string;
  serviceName: string;
  p50BaselineMs: number;
  p50CurrentMs: number;
  p95BaselineMs: number;
  p95CurrentMs: number;
  p99BaselineMs: number;
  p99CurrentMs: number;
  regressionPercent: number;
  status: RegressionStatus;
  deployId?: string;
  changeConfidence: ChangeConfidence;
  environment: string;
}

export interface ApiRegressionParams {
  environment: string;
  from: string;
  until: string;
  service?: string;
}

export async function getApiRegressions(params: ApiRegressionParams): Promise<ApiRegressionEntry[]> {
  const { data } = await client.get<ApiRegressionEntry[]>('/telemetry/api/regressions', { params });
  return data;
}

// ── SLO Marketplace ───────────────────────────────────────────────────────────

export type SloTemplateCategory = 'restApi' | 'kafka' | 'database' | 'backgroundJob';

export interface SloTemplate {
  id: string;
  name: string;
  category: SloTemplateCategory;
  sliType: string;
  target: string;
  window: string;
  compliancePreset?: string;
  uses: number;
  author: string;
  description?: string;
}

export interface SloMarketplaceParams {
  category?: SloTemplateCategory;
}

export async function getSloTemplates(params: SloMarketplaceParams): Promise<SloTemplate[]> {
  const { data } = await client.get<SloTemplate[]>('/operations/slo/templates', { params });
  return data;
}

// ── Dependency Risk ───────────────────────────────────────────────────────────

export type RiskLevel = 'critical' | 'high' | 'medium' | 'low';

export interface DependencyRiskEntry {
  id: string;
  serviceName: string;
  riskScore: number;
  riskLevel: RiskLevel;
  failureCount30d: number;
  sloHealthPercent: number;
  blastRadius: number;
  deployFrequency: number;
  dependentsCount: number;
  trendDirection: 'up' | 'down' | 'stable';
  environment: string;
}

export interface DependencyRiskParams {
  environment: string;
  from: string;
  until: string;
}

export async function getDependencyRisks(params: DependencyRiskParams): Promise<DependencyRiskEntry[]> {
  const { data } = await client.get<DependencyRiskEntry[]>('/operations/dependency-risk', { params });
  return data;
}

// ── Load Testing ──────────────────────────────────────────────────────────────

export type LoadTestSource = 'k6' | 'gatling' | 'jmeter';
export type LoadTestStatus = 'running' | 'passed' | 'failed' | 'cancelled';

export interface LoadTestRun {
  id: string;
  name: string;
  serviceName: string;
  source: LoadTestSource;
  status: LoadTestStatus;
  vus: number;
  durationMs: number;
  p95LatencyMs: number;
  errorRate: number;
  maxCapacityVus?: number;
  maxRps?: number;
  executedAt: string;
  environment: string;
}

export interface LoadTestParams {
  environment: string;
  from: string;
  until: string;
  service?: string;
}

export async function getLoadTestRuns(params: LoadTestParams): Promise<LoadTestRun[]> {
  const { data } = await client.get<LoadTestRun[]>('/operations/load-tests', { params });
  return data;
}

// ── Service Maturity SRE ──────────────────────────────────────────────────────

export type MaturityLevel = 'advanced' | 'intermediate' | 'basic' | 'initial';

export interface ServiceMaturityEntry {
  id: string;
  serviceName: string;
  teamName: string;
  score: number;
  maturityLevel: MaturityLevel;
  hasSlo: boolean;
  hasRunbook: boolean;
  hasOnCall: boolean;
  hasAlerts: boolean;
  hasProfiling: boolean;
  hasRecentPostMortem: boolean;
  environment: string;
}

export interface ServiceMaturityParams {
  environment: string;
  from: string;
  until: string;
}

export async function getServiceMaturities(params: ServiceMaturityParams): Promise<ServiceMaturityEntry[]> {
  const { data } = await client.get<ServiceMaturityEntry[]>('/operations/service-maturity', { params });
  return data;
}

// ── AI Anomaly Baseline ───────────────────────────────────────────────────────

export type AnomalySeverity = 'critical' | 'high' | 'medium' | 'low';
export type AnomalyStatus = 'open' | 'acknowledged' | 'resolved';

export interface AnomalyDetection {
  id: string;
  serviceName: string;
  metric: string;
  observedValue: number;
  baselineValue: number;
  sigmaDeviation: number;
  severity: AnomalySeverity;
  explanation: string;
  detectedAt: string;
  status: AnomalyStatus;
  modelVersion: string;
  environment: string;
}

export interface AnomalyParams {
  environment: string;
  from: string;
  until: string;
  service?: string;
}

export async function getAnomalyDetections(params: AnomalyParams): Promise<AnomalyDetection[]> {
  const { data } = await client.get<AnomalyDetection[]>('/ai/anomaly/detections', { params });
  return data;
}

// ── AI Incident Summarizer ────────────────────────────────────────────────────

export interface AiIncidentSummary {
  id: string;
  incidentId: string;
  incidentTitle: string;
  severity: string;
  serviceName: string;
  summaryText: string;
  generatedAt: string;
  modelName: string;
  confidencePercent: number;
  tokensUsed: number;
  requestedBy: string;
  environment: string;
}

export interface AiIncidentSummaryParams {
  environment: string;
  from: string;
  until: string;
}

export async function getAiIncidentSummaries(params: AiIncidentSummaryParams): Promise<AiIncidentSummary[]> {
  const { data } = await client.get<AiIncidentSummary[]>('/ai/incident-summarizer/summaries', { params });
  return data;
}

// ── AI Runbook Suggester ──────────────────────────────────────────────────────

export interface AiRunbookSuggestion {
  id: string;
  incidentId: string;
  incidentTitle: string;
  serviceName: string;
  environment: string;
  version: string;
  runbookTitle: string;
  runbookId?: string;
  confidencePercent: number;
  reasoning: string;
  modelName: string;
  suggestedAt: string;
  status: 'pending' | 'accepted' | 'rejected';
  tokensUsed: number;
  knowledgeSources: string[];
}

export interface AiRunbookSuggesterParams {
  environment: string;
  from: string;
  until: string;
}

export async function getAiRunbookSuggestions(params: AiRunbookSuggesterParams): Promise<AiRunbookSuggestion[]> {
  const { data } = await client.get<AiRunbookSuggestion[]>('/ai/runbook-suggester/suggestions', { params });
  return data;
}
