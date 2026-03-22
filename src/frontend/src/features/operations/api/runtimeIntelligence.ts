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
};
