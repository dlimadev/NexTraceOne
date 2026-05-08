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

/** Ponto de dado na série temporal de risk score (Gap 12). */
export interface RiskScoreDataPoint {
  releaseId: string;
  version: string;
  environment: string;
  changeLevel: string;
  status: string;
  score: number | null;
  createdAt: string;
}

/** Resposta da série temporal de risk scores por serviço (Gap 12). */
export interface RiskScoreTrendResponse {
  serviceName: string;
  environment: string | null;
  dataPoints: RiskScoreDataPoint[];
  generatedAt: string;
}

/** Item de release dentro de um Release Train (Gap 1). */
export interface TrainReleaseItem {
  releaseId: string;
  serviceName: string;
  version: string;
  environment: string;
  status: string;
  changeLevel: string;
  riskScore: number | null;
  isHighRisk: boolean;
  totalAffectedConsumers: number;
  createdAt: string;
}

/** Request para avaliar um Release Train (Gap 1). */
export interface EvaluateReleaseTrainRequest {
  trainName: string;
  releaseIds: string[];
}

/** Resposta da avaliação de um Release Train (Gap 1). */
export interface ReleaseTrainEvaluationResponse {
  trainName: string;
  requestedCount: number;
  foundCount: number;
  notFoundIds: string[];
  releases: TrainReleaseItem[];
  aggregateRiskScore: number | null;
  combinedAffectedConsumers: number;
  blockingServices: string[];
  readiness: string;
  evaluatedAt: string;
}

// ─── Promotion Readiness Delta types ─────────────────────────────────────────

export interface PromotionReadinessDeltaResponse {
  serviceName: string;
  environmentFrom: string;
  environmentTo: string;
  windowDays: number;
  errorRateDelta: number | null;
  latencyP95DeltaMs: number | null;
  throughputDelta: number | null;
  costDelta: number | null;
  incidentsDelta: number | null;
  dataQuality: number;
  readiness: 'Ready' | 'Review' | 'Blocked' | 'Unknown';
  simulatedNote: string | null;
}

// ─── Commit Pool & Work Item Association types ───────────────────────────────

export interface CommitAssociationItem {
  id: string;
  commitSha: string;
  commitMessage: string;
  commitAuthor: string;
  committedAt: string;
  branchName: string;
  serviceName: string;
  assignmentStatus: string;
  assignedAt: string | null;
  assignedBy: string | null;
  extractedWorkItemRefs: string | null;
}

export interface CommitsByReleaseResponse {
  releaseId: string;
  commits: CommitAssociationItem[];
}

export interface WorkItemAssociationItem {
  id: string;
  externalWorkItemId: string;
  externalSystem: string;
  title: string;
  workItemType: string;
  externalStatus: string | null;
  externalUrl: string | null;
  addedBy: string;
  addedAt: string;
  isActive: boolean;
}

export interface WorkItemsByReleaseResponse {
  releaseId: string;
  workItems: WorkItemAssociationItem[];
}

export interface AddWorkItemRequest {
  externalWorkItemId: string;
  externalSystem: string;
  title: string;
  workItemType: string;
  externalStatus?: string;
  externalUrl?: string;
}

export interface AddWorkItemResponse {
  workItemAssociationId: string;
  releaseId: string;
}

// ─── External Approval Gateway types ─────────────────────────────────────────

export interface RequestApprovalData {
  approvalType: string;
  targetEnvironment: string;
  externalSystem?: string;
  webhookUrl?: string;
  tokenExpiryHours?: number;
}

export interface RequestApprovalResponse {
  approvalRequestId: string;
  releaseId: string;
  status: string;
  callbackTokenPlainText: string | null;
  expiresAt: string;
  webhookSent: boolean;
}

export interface ApprovalRequestItem {
  id: string;
  approvalType: string;
  externalSystem: string | null;
  targetEnvironment: string;
  status: string;
  requestedAt: string;
  respondedAt: string | null;
  respondedBy: string | null;
  comments: string | null;
  externalRequestId: string | null;
  callbackTokenExpiresAt: string;
}

export interface ApprovalRequestsResponse {
  releaseId: string;
  approvalRequests: ApprovalRequestItem[];
}

// ─── Ingest Release from External System types ───────────────────────────────

export interface IngestExternalReleaseRequest {
  externalReleaseId: string;
  externalSystem: string;
  serviceName: string;
  version: string;
  targetEnvironment: string;
  description?: string;
  commitShas?: string[];
  workItems?: Array<{ id: string; system: string }>;
  triggerPromotion?: boolean;
}

export interface IngestExternalReleaseResponse {
  releaseId: string;
  externalReleaseId: string;
  isNew: boolean;
  status: string;
}

// ─── Impact Report types ──────────────────────────────────────────────────────

export interface BlastRadiusSectionDto {
  totalAffectedConsumers: number;
  directConsumers: string[];
  transitiveConsumers: string[];
  calculatedAt: string;
}

export interface WorkItemsSummaryDto {
  total: number;
  stories: number;
  bugs: number;
  features: number;
  others: number;
}

export interface CommitAuthorGroupDto {
  author: string;
  count: number;
}

export interface CommitsSummaryDto {
  total: number;
  byAuthor: CommitAuthorGroupDto[];
}

export interface ReleaseImpactReport {
  releaseId: string;
  serviceName: string;
  version: string;
  environment: string;
  status: string;
  riskScore: number | null;
  changeLevel: string;
  blastRadius: BlastRadiusSectionDto | null;
  workItemsSummary: WorkItemsSummaryDto;
  commitsSummary: CommitsSummaryDto;
  pendingApprovals: number;
  generatedAt: string;
}

export interface PromotionGateItem {
  gateId: string;
  name: string;
  description: string | null;
  environmentFrom: string;
  environmentTo: string;
  isActive: boolean;
  blockOnFailure: boolean;
  createdAt: string;
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

  /** Obtém série temporal de risk scores de um serviço (Gap 12). */
  getRiskScoreTrend: (serviceName: string, environment?: string, limit?: number) =>
    client
      .get<RiskScoreTrendResponse>('/releases/risk-trend', {
        params: {
          serviceName,
          ...(environment ? { environment } : {}),
          ...(limit ? { limit } : {}),
        },
      })
      .then((r) => r.data),

  /** Avalia um Release Train coordenado entre múltiplos serviços (Gap 1). */
  evaluateReleaseTrain: (data: EvaluateReleaseTrainRequest) =>
    client
      .post<ReleaseTrainEvaluationResponse>('/releases/train-evaluation', data)
      .then((r) => r.data),

  // ─── Phase 2: Commit Pool & Work Item Association ──────────────────────────

  /** Lista commits associados a uma release. */
  listCommitsByRelease: (releaseId: string) =>
    client
      .get<CommitsByReleaseResponse>(`/releases/${releaseId}/commits`)
      .then((r) => r.data),

  /** Lista work items activos de uma release. */
  listWorkItemsByRelease: (releaseId: string, includeRemoved = false) =>
    client
      .get<WorkItemsByReleaseResponse>(`/releases/${releaseId}/work-items`, {
        params: { includeRemoved },
      })
      .then((r) => r.data),

  /** Adiciona um work item externo a uma release. */
  addWorkItemToRelease: (releaseId: string, data: AddWorkItemRequest) =>
    client
      .post<AddWorkItemResponse>(`/releases/${releaseId}/work-items`, data)
      .then((r) => r.data),

  /** Remove (lógico) um work item de uma release. */
  removeWorkItemFromRelease: (workItemAssociationId: string) =>
    client
      .delete(`/releases/work-items-associations/${workItemAssociationId}`)
      .then((r) => r.data),

  // ─── Phase 3: External Approval Gateway ───────────────────────────────────

  /** Cria um pedido de aprovação de release (interno ou externo). */
  requestExternalApproval: (releaseId: string, data: RequestApprovalData) =>
    client
      .post<RequestApprovalResponse>(`/releases/${releaseId}/approval-requests`, data)
      .then((r) => r.data),

  /** Lista pedidos de aprovação de uma release. */
  listApprovalRequests: (releaseId: string) =>
    client
      .get<ApprovalRequestsResponse>(`/releases/${releaseId}/approval-requests`)
      .then((r) => r.data),

  // ─── Phase 4: Ingest Release from External System ─────────────────────────

  /** Ingere uma release criada por um sistema externo. */
  ingestExternalRelease: (data: IngestExternalReleaseRequest) =>
    client
      .post<IngestExternalReleaseResponse>('/releases/ingest', data)
      .then((r) => r.data),

  // ─── Phase 5: Impact Report ────────────────────────────────────────────────

  /** Obtém o relatório de impacto calculado de uma release. */
  getReleaseImpactReport: (releaseId: string) =>
    client
      .get<ReleaseImpactReport>(`/releases/${releaseId}/impact-report`)
      .then((r) => r.data),

  // ─── Approval Policies ────────────────────────────────────────────────────

  /** Lista políticas de aprovação activas do tenant. */
  listApprovalPolicies: (environmentId?: string, serviceId?: string) =>
    client
      .get<any[]>('/releases/approval-policies', { params: { environmentId, serviceId } })
      .then((r) => r.data),

  /** Cria uma nova política de aprovação. */
  createApprovalPolicy: (data: {
    name: string;
    approvalType: string;
    environmentId?: string;
    serviceId?: string;
    serviceTag?: string;
    externalWebhookUrl?: string;
    minApprovers?: number;
    expirationHours?: number;
    requireEvidencePack?: boolean;
    requireChecklistCompletion?: boolean;
    minRiskScoreForManualApproval?: number;
    priority?: number;
  }) =>
    client
      .post<{ policyId: string; name: string }>('/releases/approval-policies', data)
      .then((r) => r.data),

  /** Desactiva (soft-delete) uma política de aprovação. */
  deleteApprovalPolicy: (policyId: string) =>
    client
      .delete(`/releases/approval-policies/${policyId}`)
      .then((r) => r.data),

  // ─── Post-Release Review ───────────────────────────────────────────────────

  /** Obtém a review pós-release de uma release. */
  getPostReleaseReview: (releaseId: string) =>
    client
      .get<any>(`/releases/${releaseId}/review`)
      .then((r) => r.data),

  /** Inicia uma review pós-release para uma release. */
  startPostReleaseReview: (releaseId: string) =>
    client
      .post<{ reviewId: string }>(`/releases/${releaseId}/review/start`, { releaseId })
      .then((r) => r.data),

  /** Progride a review pós-release para a próxima fase. */
  progressPostReleaseReview: (releaseId: string) =>
    client
      .post<any>(`/releases/${releaseId}/review/progress`, { releaseId })
      .then((r) => r.data),

  // ─── Rollback Assessment ───────────────────────────────────────────────────

  /** Obtém a avaliação de viabilidade de rollback para uma release. */
  getRollbackAssessment: (releaseId: string) =>
    client
      .get<any>(`/releases/${releaseId}/rollback-assessment`)
      .then((r) => r.data),

  /** Regista ou actualiza a avaliação de viabilidade de rollback de uma release. */
  assessRollbackViability: (
    releaseId: string,
    data: {
      isViable: boolean;
      previousVersion?: string;
      hasReversibleMigrations: boolean;
      consumersAlreadyMigrated: number;
      totalConsumersImpacted: number;
      inviabilityReason?: string;
      recommendation: string;
    },
  ) =>
    client
      .post<any>(`/releases/${releaseId}/rollback-assessment`, { releaseId, ...data })
      .then((r) => r.data),

  // ─── Promotion Readiness Delta ────────────────────────────────────────────

  /** Obtém os deltas de runtime entre dois ambientes para decisão de promoção. */
  getPromotionReadinessDelta: (params: {
    service: string;
    from: string;
    to: string;
    days?: number;
  }) =>
    client
      .get<PromotionReadinessDeltaResponse>('/changes/promotion-readiness-delta', { params })
      .then((r) => r.data),

  // ─── Promotion Gates ───────────────────────────────────────────────────────

  /** Lista os gates de promoção entre dois ambientes. */
  listPromotionGatesByEnvironment: (environmentFrom: string, environmentTo: string) =>
    client
      .get<{ gates: PromotionGateItem[] }>('/releases/promotion-gates', {
        params: { environmentFrom, environmentTo },
      })
      .then((r) => r.data),

  /** Obtém o estado de um gate de promoção específico e as suas avaliações recentes. */
  getPromotionGateStatus: (gateId: string) =>
    client
      .get<any>(`/releases/promotion-gates/${gateId}/status`)
      .then((r) => r.data),
};
