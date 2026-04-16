import client from '../../../api/client';
import type {
  Release,
  BlastRadiusReport,
  ChangeScore,
  PagedList,
  DeploymentState,
  ChangeLevel,
} from '../../../types';

// ─── Intelligence Summary DTOs ───────────────────────────────────────────────

export interface IntelligenceSummary {
  release: ReleaseDto;
  score: ScoreDto | null;
  blastRadius: BlastRadiusDto | null;
  markers: MarkerDto[];
  baseline: BaselineDto | null;
  postReleaseReview: ReviewDto | null;
  rollbackAssessment: RollbackDto | null;
  timeline: TimelineEventDto[];
}

export interface ReleaseDto {
  id: string;
  apiAssetId: string;
  serviceName: string;
  version: string;
  environment: string;
  status: string;
  changeLevel: string;
  changeScore: number;
  workItemReference: string | null;
  createdAt: string;
}

export interface ScoreDto {
  score: number;
  breakingChangeWeight: number;
  blastRadiusWeight: number;
  environmentWeight: number;
  computedAt: string;
}

export interface BlastRadiusDto {
  totalAffectedConsumers: number;
  directConsumers: string[];
  transitiveConsumers: string[];
  calculatedAt: string;
}

export interface MarkerDto {
  id: string;
  markerType: string;
  sourceSystem: string;
  externalId: string;
  occurredAt: string;
}

export interface BaselineDto {
  requestsPerMinute: number;
  errorRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  throughput: number;
  collectedFrom: string;
  collectedTo: string;
}

export interface ReviewDto {
  currentPhase: string;
  outcome: string;
  confidenceScore: number;
  summary: string;
  isCompleted: boolean;
  startedAt: string;
  completedAt: string | null;
}

export interface RollbackDto {
  isViable: boolean;
  readinessScore: number;
  previousVersion: string | null;
  hasReversibleMigrations: boolean;
  consumersAlreadyMigrated: number;
  totalConsumersImpacted: number;
  recommendation: string;
  assessedAt: string;
}

export interface TimelineEventDto {
  id: string;
  eventType: string;
  description: string;
  source: string;
  occurredAt: string;
}

export interface FreezeWindowDto {
  id: string;
  name: string;
  reason: string;
  scope: string;
  scopeValue: string | null;
  startsAt: string;
  endsAt: string;
}

export interface FreezeWindowListDto {
  id: string;
  name: string;
  reason: string;
  scope: string;
  scopeValue: string | null;
  startsAt: string;
  endsAt: string;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
}

// ─── Pre-production Comparison DTOs ─────────────────────────────────────────

export interface MetricDiff {
  metric: string;
  preProductionValue: number | null;
  productionValue: number | null;
  relativeChangePercent: number | null;
  trend: 'Improved' | 'Degraded' | 'Stable' | 'Unknown';
}

export interface PreProdComparisonResponse {
  preProductionReleaseId: string;
  productionReleaseId: string;
  preProductionServiceName: string;
  productionServiceName: string;
  hasBaselineData: boolean;
  overallSignal: 'Positive' | 'Neutral' | 'Concerning';
  overallRationale: string;
  errorRate: MetricDiff | null;
  avgLatencyMs: MetricDiff | null;
  p95LatencyMs: MetricDiff | null;
  p99LatencyMs: MetricDiff | null;
  requestsPerMinute: MetricDiff | null;
  throughput: MetricDiff | null;
}

// ─── Deploy Readiness DTOs ───────────────────────────────────────────────────

export interface DeployReadinessCheck {
  checkId: string;
  description: string;
  passed: boolean;
  message: string;
}

export interface DeployReadinessResponse {
  releaseId: string;
  releaseName: string;
  isReady: boolean;
  totalChecks: number;
  passedChecks: number;
  failedChecks: number;
  checks: DeployReadinessCheck[];
  evaluatedAt: string;
}

// ─── Release Notes DTOs ──────────────────────────────────────────────────────

export interface ReleaseNotesResponse {
  releaseNotesId: string;
  releaseId: string;
  technicalSummary: string;
  executiveSummary: string | null;
  newEndpointsSection: string | null;
  breakingChangesSection: string | null;
  affectedServicesSection: string | null;
  confidenceMetricsSection: string | null;
  evidenceLinksSection: string | null;
  modelUsed: string;
  tokensUsed: number;
  status: string;
  generatedAt: string;
  lastRegeneratedAt: string | null;
  regenerationCount: number;
}

// ─── Trace Correlation DTOs ──────────────────────────────────────────────────

export interface TraceCorrelationDto {
  traceId: string;
  correlatedAt: string;
  description: string | null;
}

export interface TraceCorrelationsResponse {
  releaseId: string;
  serviceName: string;
  correlationCount: number;
  correlations: TraceCorrelationDto[];
}

export interface FreezeWindowListDto {
  id: string;
  name: string;
  reason: string;
  scope: string;
  scopeValue: string | null;
  startsAt: string;
  endsAt: string;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
}

export interface CalendarReleaseDto {
  releaseId: string;
  serviceName: string;
  version: string;
  environment: string;
  status: string;
  changeType: string;
  confidenceStatus: string;
  changeScore: number;
  changeLevel: number;
  teamName: string | null;
  createdAt: string;
}

export interface CalendarFreezeDto {
  freezeWindowId: string;
  name: string;
  reason: string;
  scope: string;
  scopeValue: string | null;
  startsAt: string;
  endsAt: string;
  isActive: boolean;
}

export interface DailySummaryDto {
  date: string;
  totalReleases: number;
  highRiskReleases: number;
  averageScore: number;
}

export interface ReleaseCalendarResponse {
  releases: CalendarReleaseDto[];
  freezeWindows: CalendarFreezeDto[];
  dailySummary: DailySummaryDto[];
}

export interface CreateFreezeWindowRequest {
  name: string;
  reason: string;
  scope: number;
  scopeValue?: string | null;
  startsAt: string;
  endsAt: string;
}

export interface UpdateFreezeWindowRequest {
  name: string;
  reason: string;
  scope: number;
  scopeValue?: string | null;
  startsAt: string;
  endsAt: string;
}

// ─── API Client ──────────────────────────────────────────────────────────────

export const changeIntelligenceApi = {
  notifyDeployment: (data: {
    apiAssetId: string;
    version: string;
    environment: string;
    commitSha?: string;
    pipelineUrl?: string;
  }) =>
    client.post<{ id: string }>('/releases', data).then((r) => r.data),

  getRelease: (id: string) =>
    client.get<Release>(`/releases/${id}`).then((r) => r.data),

  listReleases: (apiAssetId: string, page = 1, pageSize = 20) =>
    client
      .get<PagedList<Release>>('/releases', { params: { apiAssetId, page, pageSize } })
      .then((r) => r.data),

  listRecentReleases: (page = 1, pageSize = 50) =>
    client
      .get<PagedList<Release>>('/releases', { params: { page, pageSize } })
      .then((r) => r.data),

  getReleaseHistory: (apiAssetId: string, page = 1, pageSize = 20) =>
    client
      .get<PagedList<Release>>(`/releases/${apiAssetId}/history`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  classifyChangeLevel: (releaseId: string, changeLevel: ChangeLevel) =>
    client
      .put(`/releases/${releaseId}/classify`, { releaseId, changeLevel })
      .then((r) => r.data),

  updateDeploymentState: (releaseId: string, state: DeploymentState) =>
    client
      .put(`/releases/${releaseId}/status`, { releaseId, newState: state })
      .then((r) => r.data),

  registerRollback: (releaseId: string, reason: string) =>
    client
      .post(`/releases/${releaseId}/rollback`, { releaseId, reason })
      .then((r) => r.data),

  calculateBlastRadius: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/blast-radius`, { releaseId })
      .then((r) => r.data),

  getBlastRadius: (releaseId: string) =>
    client.get<BlastRadiusReport>(`/releases/${releaseId}/blast-radius`).then((r) => r.data),

  computeScore: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/score`, { releaseId })
      .then((r) => r.data),

  getScore: (releaseId: string) =>
    client.get<ChangeScore>(`/releases/${releaseId}/score`).then((r) => r.data),

  attachWorkItem: (
    releaseId: string,
    data: { provider: string; workItemId: string; url: string },
  ) =>
    client.put(`/releases/${releaseId}/workitem`, { releaseId, ...data }).then((r) => r.data),

  getIntelligenceSummary: (releaseId: string) =>
    client
      .get<IntelligenceSummary>(`/releases/${releaseId}/intelligence`)
      .then((r) => r.data),

  registerMarker: (
    releaseId: string,
    data: {
      markerType: string;
      sourceSystem: string;
      externalId: string;
      payload?: string;
      occurredAt: string;
    },
  ) =>
    client.post(`/releases/${releaseId}/markers`, data).then((r) => r.data),

  recordBaseline: (
    releaseId: string,
    data: {
      requestsPerMinute: number;
      errorRate: number;
      avgLatencyMs: number;
      p95LatencyMs: number;
      p99LatencyMs: number;
      throughput: number;
      collectedFrom: string;
      collectedTo: string;
    },
  ) =>
    client.post(`/releases/${releaseId}/baseline`, data).then((r) => r.data),

  startReview: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/review/start`, { releaseId })
      .then((r) => r.data),

  checkFreezeConflict: (at: string, environment?: string) => {
    const params = new URLSearchParams({ at });
    if (environment) params.append('environment', environment);
    return client
      .get<{ hasConflict: boolean; activeFreezes: FreezeWindowDto[] }>(
        `/freeze-windows/check?${params.toString()}`,
      )
      .then((r) => r.data);
  },

  listFreezeWindows: (from: string, to: string, environment?: string, isActive?: boolean) => {
    const params: Record<string, string> = { from, to };
    if (environment) params.environment = environment;
    if (isActive !== undefined) params.isActive = String(isActive);
    return client
      .get<{ items: FreezeWindowListDto[] }>('/freeze-windows', { params })
      .then((r) => r.data);
  },

  createFreezeWindow: (data: CreateFreezeWindowRequest) =>
    client.post<{ id: string }>('/freeze-windows', data).then((r) => r.data),

  updateFreezeWindow: (id: string, data: UpdateFreezeWindowRequest) =>
    client.put(`/freeze-windows/${id}`, data).then((r) => r.data),

  deactivateFreezeWindow: (id: string) =>
    client.post(`/freeze-windows/${id}/deactivate`).then((r) => r.data),

  getReleaseCalendar: (from: string, to: string, environment?: string) => {
    const params: Record<string, string> = { from, to };
    if (environment) params.environment = environment;
    return client
      .get<ReleaseCalendarResponse>('/release-calendar', { params })
      .then((r) => r.data);
  },

  /** Compara baseline de pré-produção com baseline de produção antes da promoção. */
  getPreProdComparison: (preProdReleaseId: string, productionReleaseId: string) =>
    client
      .get<PreProdComparisonResponse>(`/releases/${preProdReleaseId}/pre-prod-comparison`, {
        params: { productionReleaseId },
      })
      .then((r) => r.data),

  /** Verifica pré-condições de deploy de uma release num ambiente. */
  getDeployReadiness: (releaseId: string, environmentName?: string) =>
    client
      .get<DeployReadinessResponse>(`/releases/${releaseId}/deploy-readiness`, {
        params: environmentName ? { environmentName } : undefined,
      })
      .then((r) => r.data),

  /** Obtém release notes geradas por IA para uma release. */
  getReleaseNotes: (releaseId: string) =>
    client
      .get<ReleaseNotesResponse>(`/releases/${releaseId}/notes`)
      .then((r) => r.data),

  /** Gera release notes por IA para uma release. */
  generateReleaseNotes: (releaseId: string, personaMode?: string) =>
    client
      .post<{ releaseNotesId: string }>(`/releases/${releaseId}/notes`, { releaseId, personaMode })
      .then((r) => r.data),

  /** Regenera release notes existentes com dados atualizados. */
  regenerateReleaseNotes: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/notes/regenerate`)
      .then((r) => r.data),

  /** Obtém traces correlacionados com uma release. */
  getTraceCorrelations: (releaseId: string) =>
    client
      .get<TraceCorrelationsResponse>(`/releases/${releaseId}/traces`)
      .then((r) => r.data),
};
