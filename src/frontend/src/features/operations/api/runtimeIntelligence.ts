import client from '../../../api/client';

/** Tipos para Runtime Intelligence. */

export interface MetricsSummary {
  avgLatencyMs: number;
  p99LatencyMs: number;
  errorRate: number;
  requestsPerSecond: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
}

export interface CompareReleaseRuntimeResponse {
  serviceName: string;
  environment: string;
  beforeMetrics: MetricsSummary;
  afterMetrics: MetricsSummary;
  beforeDataPoints: number;
  afterDataPoints: number;
  latencyDeltaPercent: number;
  errorRateDeltaPercent: number;
  throughputDeltaPercent: number;
}

export interface DriftFindingItem {
  id: string;
  serviceName: string;
  environment: string;
  metricName: string;
  expectedValue: number;
  actualValue: number;
  deviationPercent: number;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  detectedAt: string;
  acknowledgedAt: string | null;
}

export interface DriftFindingsResponse {
  items: DriftFindingItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ReleaseHealthPoint {
  releaseId: string | null;
  releaseName: string | null;
  periodStart: string;
  periodEnd: string;
  avgLatencyMs: number;
  errorRate: number;
  requestsPerSecond: number;
  snapshotCount: number;
}

export interface ReleaseHealthTimelineResponse {
  serviceName: string;
  environment: string;
  points: ReleaseHealthPoint[];
}

export interface ObservabilityScoreResponse {
  serviceName: string;
  environment: string;
  score: number;
  grade: string;
  level: string;
  breakdown: {
    latencyScore: number;
    errorScore: number;
    throughputScore: number;
    resourceScore: number;
  };
  computedAt: string;
}

// Novos tipos adicionados para o Dashboard completo

export interface RuntimeSnapshot {
  id: string;
  serviceName: string;
  environment: string;
  timestamp: string;
  healthStatus: 'Healthy' | 'Degraded' | 'Unhealthy';
  avgLatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  errorRate: number;
  requestsPerSecond: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
  diskUsagePercent?: number;
  activeConnections?: number;
}

export interface RuntimeSnapshotsResponse {
  items: RuntimeSnapshot[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IngestSnapshotRequest {
  serviceName: string;
  environment: string;
  timestamp: string;
  avgLatencyMs: number;
  p95LatencyMs?: number;
  p99LatencyMs?: number;
  errorRate: number;
  requestsPerSecond: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
  diskUsagePercent?: number;
  activeConnections?: number;
}

export interface ObservabilityScoreItem {
  serviceName: string;
  environment: string;
  score: number;
  hasCriticalDrift: boolean;
  computedAt: string;
}

export interface ObservabilityScoresResponse {
  items: ObservabilityScoreItem[];
  totalCount: number;
}

export interface LogSearchRequest {
  serviceName?: string;
  environment?: string;
  severity?: string;
  searchText?: string;
  from: string;
  to: string;
  page: number;
  pageSize: number;
}

export interface LogEntry {
  id: string;
  timestamp: string;
  severity: string;
  message: string;
  serviceName?: string;
  environment?: string;
  attributes: Record<string, any>;
}

export interface LogSearchResponse {
  entries: LogEntry[];
  total: number;
}

export interface TelemetryBackendStats {
  backendType: string;
  totalDocuments: number;
  totalSizeBytes: number;
  activeIndices: number;
  lastIndexTime: string;
}

/** Cliente de API para Runtime Intelligence. */
export const runtimeIntelligenceApi = {
  compareReleaseRuntime: (params: {
    serviceName: string;
    environment: string;
    beforeStart: string;
    beforeEnd: string;
    afterStart: string;
    afterEnd: string;
  }) =>
    client.get<CompareReleaseRuntimeResponse>('/runtime/compare', { params }).then((r) => r.data),

  getDriftFindings: (params?: {
    serviceName?: string;
    environment?: string;
    unacknowledgedOnly?: boolean;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<DriftFindingsResponse>('/runtime/drift', { params }).then((r) => r.data),

  getReleaseHealthTimeline: (params: {
    serviceName: string;
    environment: string;
    windowStart: string;
    windowEnd: string;
  }) =>
    client.get<ReleaseHealthTimelineResponse>('/runtime/timeline', { params }).then((r) => r.data),

  getObservabilityScore: (params: {
    serviceName: string;
    environment: string;
  }) =>
    client.get<ObservabilityScoreResponse>('/runtime/observability', { params }).then((r) => r.data),

  // Novos métodos para o Dashboard completo
  
  /**
   * Obtém snapshots de runtime filtrados por serviço e ambiente
   */
  getSnapshots: (serviceName?: string, environment?: string, page: number = 1, pageSize: number = 50) =>
    client.get<RuntimeSnapshotsResponse>('/runtime/snapshots', { 
      params: { serviceName, environment, page, pageSize } 
    }).then((r) => r.data),

  /**
   * Ingerir um novo snapshot de runtime
   */
  ingestSnapshot: (data: IngestSnapshotRequest) =>
    client.post('/runtime/snapshots', data).then((r) => r.data),

  /**
   * Obtém scores de observabilidade para múltiplos serviços
   */
  getObservabilityScores: (serviceName?: string, environment?: string) =>
    client.get<ObservabilityScoresResponse>('/runtime/observability/scores', { 
      params: { serviceName, environment } 
    }).then((r) => r.data),

  /**
   * Pesquisa logs no backend de telemetria (ClickHouse ou Elasticsearch)
   */
  searchLogs: (request: LogSearchRequest) =>
    client.post<LogSearchResponse>('/runtime/logs/search', request).then((r) => r.data),

  /**
   * Obtém estatísticas do backend de telemetria
   */
  getTelemetryStats: () =>
    client.get<TelemetryBackendStats>('/runtime/telemetry/stats').then((r) => r.data),

  /**
   * Verifica saúde do backend de telemetria
   */
  checkTelemetryHealth: () =>
    client.get<{ isHealthy: boolean; backend: string }>('/runtime/telemetry/health').then((r) => r.data),

  /**
   * Obtém métricas de requisições do ClickHouse (se configurado)
   */
  getRequestMetrics: (from: string, to: string, endpoint?: string) =>
    client.get('/runtime/clickhouse/request-metrics', { 
      params: { from, to, endpoint } 
    }).then((r) => r.data),

  /**
   * Obtém analytics de erros do ClickHouse (se configurado)
   */
  getErrorAnalytics: (from: string, to: string, errorType?: string) =>
    client.get('/runtime/clickhouse/error-analytics', { 
      params: { from, to, errorType } 
    }).then((r) => r.data),

  /**
   * Obtém métricas de saúde do sistema do ClickHouse (se configurado)
   */
  getSystemHealth: (from: string, to: string, serviceName?: string) =>
    client.get('/runtime/clickhouse/system-health', { 
      params: { from, to, serviceName } 
    }).then((r) => r.data),
};
