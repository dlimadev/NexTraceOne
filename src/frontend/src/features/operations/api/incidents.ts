import client from '../../../api/client';

// ── Incident List ────────────────────────────────────────────────────

export interface IncidentListItem {
  incidentId: string;
  reference: string;
  title: string;
  incidentType: string;
  severity: string;
  status: string;
  serviceId: string;
  serviceDisplayName: string;
  ownerTeam: string;
  environment: string;
  createdAt: string;
  hasCorrelatedChanges: boolean;
  correlationConfidence: string;
  mitigationStatus: string;
}

export interface IncidentListResponse {
  items: IncidentListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IncidentListFilters {
  teamId?: string;
  serviceId?: string;
  environment?: string;
  severity?: string;
  status?: string;
  incidentType?: string;
  search?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// ── Incident Detail ──────────────────────────────────────────────────

export interface IncidentIdentity {
  incidentId: string;
  reference: string;
  title: string;
  summary: string;
  incidentType: string;
  severity: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface LinkedServiceItem {
  serviceId: string;
  displayName: string;
  serviceType: string;
  criticality: string;
}

export interface TimelineEntry {
  timestamp: string;
  description: string;
}

export interface RelatedChangeItem {
  changeId: string;
  description: string;
  changeType: string;
  confidenceStatus: string;
  deployedAt: string;
}

export interface RelatedServiceItem {
  serviceId: string;
  displayName: string;
  impactDescription: string;
}

export interface CorrelationSummary {
  confidence: string;
  reason: string;
  relatedChanges: RelatedChangeItem[];
  relatedServices: RelatedServiceItem[];
}

export interface EvidenceItem {
  title: string;
  description: string;
}

export interface EvidenceSummary {
  operationalSignalsSummary: string;
  degradationSummary: string;
  observations: EvidenceItem[];
}

export interface RelatedContractItem {
  contractVersionId: string;
  name: string;
  version: string;
  protocol: string;
  lifecycleState: string;
}

export interface RunbookItem {
  title: string;
  url?: string;
}

export interface MitigationActionItem {
  description: string;
  status: string;
  completed: boolean;
}

export interface MitigationSummary {
  status: string;
  actions: MitigationActionItem[];
  rollbackGuidance?: string;
  rollbackRelevant: boolean;
  escalationGuidance?: string;
}

export interface IncidentDetailResponse {
  identity: IncidentIdentity;
  linkedServices: LinkedServiceItem[];
  ownerTeam: string;
  impactedDomain: string;
  impactedEnvironment: string;
  timeline: TimelineEntry[];
  correlation: CorrelationSummary;
  evidence: EvidenceSummary;
  relatedContracts: RelatedContractItem[];
  runbooks: RunbookItem[];
  mitigation: MitigationSummary;
}

// ── Incident Summary ─────────────────────────────────────────────────

export interface SeverityBreakdown {
  critical: number;
  major: number;
  minor: number;
  warning: number;
}

export interface StatusBreakdown {
  open: number;
  investigating: number;
  mitigating: number;
  monitoring: number;
  resolved: number;
  closed: number;
}

export interface IncidentSummaryResponse {
  totalOpen: number;
  criticalIncidents: number;
  withCorrelatedChanges: number;
  withMitigationAvailable: number;
  servicesImpacted: number;
  severityBreakdown: SeverityBreakdown;
  statusBreakdown: StatusBreakdown;
}

// ── Correlation ──────────────────────────────────────────────────────

export interface CorrelatedChange {
  changeId: string;
  description: string;
  changeType: string;
  confidenceStatus: string;
  deployedAt: string;
}

export interface CorrelatedService {
  serviceId: string;
  displayName: string;
  impactDescription: string;
}

export interface CorrelatedDependency {
  serviceId: string;
  displayName: string;
  relationship: string;
}

export interface ImpactedContract {
  contractVersionId: string;
  name: string;
  version: string;
  protocol: string;
}

export interface IncidentCorrelationResponse {
  incidentId: string;
  confidence: string;
  reason: string;
  relatedChanges: CorrelatedChange[];
  relatedServices: CorrelatedService[];
  relatedDependencies: CorrelatedDependency[];
  possibleImpactedContracts: ImpactedContract[];
}

// ── Evidence ─────────────────────────────────────────────────────────

export interface EvidenceObservation {
  title: string;
  description: string;
}

export interface IncidentEvidenceResponse {
  incidentId: string;
  operationalSignalsSummary: string;
  degradationSummary: string;
  observations: EvidenceObservation[];
  anomalySummary: string;
  notes?: string;
}

// ── Mitigation ───────────────────────────────────────────────────────

export interface SuggestedAction {
  description: string;
  status: string;
  completed: boolean;
}

export interface RecommendedRunbook {
  title: string;
  url?: string;
  description?: string;
}

export interface IncidentMitigationResponse {
  incidentId: string;
  mitigationStatus: string;
  suggestedActions: SuggestedAction[];
  recommendedRunbooks: RecommendedRunbook[];
  rollbackGuidance?: string;
  rollbackRelevant: boolean;
  escalationGuidance?: string;
}

// ── Mitigation Workflow ──────────────────────────────────────────────

export interface WorkflowStepDto {
  stepOrder: number;
  title: string;
  description?: string;
  isCompleted: boolean;
  completedBy?: string;
  completedAt?: string;
  notes?: string;
}

export interface WorkflowDecisionDto {
  decisionType: string;
  decidedBy: string;
  decidedAt: string;
  reason?: string;
}

export interface MitigationWorkflowResponse {
  workflowId: string;
  incidentId: string;
  title: string;
  status: string;
  actionType: string;
  riskLevel: string;
  requiresApproval: boolean;
  approvedBy?: string;
  approvedAt?: string;
  createdBy: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  outcome?: string;
  outcomeNotes?: string;
  linkedRunbookId?: string;
  steps: WorkflowStepDto[];
  decisions: WorkflowDecisionDto[];
}

export interface CreateMitigationWorkflowRequest {
  title: string;
  actionType: string;
  riskLevel: string;
  requiresApproval: boolean;
  linkedRunbookId?: string;
  steps?: { stepOrder: number; title: string; description?: string }[];
}

export interface CreateMitigationWorkflowResponse {
  workflowId: string;
  status: string;
  createdAt: string;
}

export interface UpdateWorkflowActionRequest {
  action: string;
  performedBy?: string;
  reason?: string;
  notes?: string;
}

export interface UpdateWorkflowActionResponse {
  workflowId: string;
  newStatus: string;
  actionPerformed: string;
  performedAt: string;
}

// ── Mitigation Validation ────────────────────────────────────────────

export interface ValidationCheckDto {
  checkName: string;
  description?: string;
  isPassed: boolean;
  observedValue?: string;
}

export interface MitigationValidationResponse {
  workflowId: string;
  status: string;
  expectedChecks: ValidationCheckDto[];
  observedOutcome?: string;
  postMitigationSignalsSummary?: string;
  validatedAt?: string;
  validatedBy?: string;
}

export interface RecordValidationRequest {
  status: string;
  observedOutcome?: string;
  validatedBy?: string;
  checks?: { checkName: string; isPassed: boolean; observedValue?: string }[];
}

export interface RecordValidationResponse {
  workflowId: string;
  status: string;
  validatedAt: string;
}

// ── Mitigation History ───────────────────────────────────────────────

export interface MitigationAuditEntry {
  entryId: string;
  workflowId?: string;
  action: string;
  performedBy: string;
  performedAt: string;
  notes?: string;
  outcome?: string;
  validationResult?: string;
  linkedEvidence: string[];
}

export interface MitigationHistoryResponse {
  incidentId: string;
  entries: MitigationAuditEntry[];
}

// ── Mitigation Recommendations ───────────────────────────────────────

export interface MitigationRecommendation {
  recommendationId: string;
  title: string;
  summary: string;
  recommendedActionType: string;
  rationaleSummary: string;
  evidenceSummary?: string;
  requiresApproval: boolean;
  riskLevel: string;
  linkedRunbookIds: string[];
  suggestedValidationSteps: string[];
}

export interface MitigationRecommendationsResponse {
  incidentId: string;
  recommendations: MitigationRecommendation[];
}

// ── API Service ──────────────────────────────────────────────────────

export const incidentsApi = {
  // Incidents
  listIncidents: (filters?: IncidentListFilters) =>
    client.get<IncidentListResponse>('/incidents', { params: filters }).then(r => r.data),

  getIncidentDetail: (incidentId: string) =>
    client.get<IncidentDetailResponse>(`/incidents/${incidentId}`).then(r => r.data),

  getIncidentSummary: () =>
    client.get<IncidentSummaryResponse>('/incidents/summary').then(r => r.data),

  getIncidentCorrelation: (incidentId: string) =>
    client.get<IncidentCorrelationResponse>(`/incidents/${incidentId}/correlation`).then(r => r.data),

  getIncidentEvidence: (incidentId: string) =>
    client.get<IncidentEvidenceResponse>(`/incidents/${incidentId}/evidence`).then(r => r.data),

  getIncidentMitigation: (incidentId: string) =>
    client.get<IncidentMitigationResponse>(`/incidents/${incidentId}/mitigation`).then(r => r.data),

  listIncidentsByService: (serviceId: string, page = 1, pageSize = 20) =>
    client.get(`/services/${serviceId}/incidents`, { params: { page, pageSize } }).then(r => r.data),

  listIncidentsByTeam: (teamId: string, page = 1, pageSize = 20) =>
    client.get(`/teams/${teamId}/incidents`, { params: { page, pageSize } }).then(r => r.data),

  // Mitigation
  getMitigationRecommendations: (incidentId: string) =>
    client.get<MitigationRecommendationsResponse>(`/incidents/${incidentId}/mitigation/recommendations`).then(r => r.data),

  getMitigationWorkflow: (incidentId: string, workflowId: string) =>
    client.get<MitigationWorkflowResponse>(`/incidents/${incidentId}/mitigation/workflows/${workflowId}`).then(r => r.data),

  createMitigationWorkflow: (incidentId: string, data: CreateMitigationWorkflowRequest) =>
    client.post<CreateMitigationWorkflowResponse>(`/incidents/${incidentId}/mitigation/workflows`, data).then(r => r.data),

  updateMitigationWorkflowAction: (incidentId: string, workflowId: string, data: UpdateWorkflowActionRequest) =>
    client.patch<UpdateWorkflowActionResponse>(`/incidents/${incidentId}/mitigation/workflows/${workflowId}/actions`, data).then(r => r.data),

  getMitigationHistory: (incidentId: string) =>
    client.get<MitigationHistoryResponse>(`/incidents/${incidentId}/mitigation/history`).then(r => r.data),

  getMitigationValidation: (incidentId: string, workflowId: string) =>
    client.get<MitigationValidationResponse>(`/incidents/${incidentId}/mitigation/workflows/${workflowId}/validation`).then(r => r.data),

  recordMitigationValidation: (incidentId: string, workflowId: string, data: RecordValidationRequest) =>
    client.post<RecordValidationResponse>(`/incidents/${incidentId}/mitigation/workflows/${workflowId}/validation`, data).then(r => r.data),

  // Runbooks
  listRunbooks: (filters?: { serviceId?: string; incidentType?: string; search?: string }) =>
    client.get('/runbooks', { params: filters }).then(r => r.data),

  getRunbookDetail: (runbookId: string) =>
    client.get(`/runbooks/${runbookId}`).then(r => r.data),
};
