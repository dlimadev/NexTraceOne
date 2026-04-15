// ─── Auth & Identity ─────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

/** Resposta de autenticação — alinhada com o backend LoginResponse. */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: LoginUser;
}

/** Resposta do login via cookie httpOnly + CSRF. */
export interface CookieSessionLoginResponse {
  csrfToken: string;
  expiresIn: number;
  user: LoginUser;
}

export type AuthLoginResponse = LoginResponse | CookieSessionLoginResponse;

/** Resumo do usuário retornado no login (nested em LoginResponse). */
export interface LoginUser {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  roleName: string;
  permissions: string[];
}

/** Perfil do usuário autenticado retornado por /auth/me. */
export interface CurrentUserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  lastLoginAt: string | null;
  tenantId: string;
  roleName: string;
  permissions: string[];
}

/** Informações amigáveis de um tenant para seleção. */
export interface TenantInfo {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  roleName: string;
}

/** Resposta da seleção de tenant após autenticação. */
export interface SelectTenantResponse {
  accessToken: string;
  expiresIn: number;
  tenantId: string;
  tenantName: string;
  roleName: string;
  permissions: string[];
  csrfToken?: string;
}

/** Perfil detalhado retornado por /users/{id} — inclui memberships multi-tenant. */
export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  lastLoginAt: string | null;
  memberships: UserMembership[];
}

export interface UserMembership {
  tenantId: string;
  roleId: string;
  roleName: string;
  isActive: boolean;
}

export interface TenantUser {
  userId: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roleName: string;
}

export interface RoleInfo {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  permissions: string[];
}

export interface PermissionInfo {
  id: string;
  code: string;
  name: string;
  module: string;
}

export interface ActiveSession {
  sessionId: string;
  ipAddress: string;
  userAgent: string;
  expiresAt: string;
}

// ─── Enterprise: Break Glass ─────────────────────────────────────────────────

/** Solicitação de acesso emergencial (Break Glass). */
export interface BreakGlassRequest {
  id: string;
  requestedBy: string;
  justification: string;
  status: 'Active' | 'Expired' | 'Revoked' | 'PostMortemCompleted';
  requestedAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  hasPostMortem: boolean;
}

/** Resposta da ativação de Break Glass. */
export interface BreakGlassActivationResponse {
  requestId: string;
  expiresAt: string;
  quarterlyUsageCount: number;
  quarterlyUsageLimit: number;
}

// ─── Enterprise: JIT Access ──────────────────────────────────────────────────

/** Solicitação de acesso privilegiado temporário (JIT). */
export interface JitAccessRequest {
  id: string;
  requestedBy: string;
  permissionCode: string;
  scope: string;
  justification: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';
  requestedAt: string;
  approvalDeadline: string;
  grantedFrom: string | null;
  grantedUntil: string | null;
}

/** Resposta da criação de solicitação JIT. */
export interface JitAccessCreatedResponse {
  requestId: string;
  approvalDeadline: string;
}

// ─── Enterprise: Delegation ──────────────────────────────────────────────────

/** Delegação formal de permissões entre usuários. */
export interface DelegationInfo {
  id: string;
  grantorId: string;
  delegateeId: string;
  permissions: string[];
  reason: string;
  status: 'Active' | 'Expired' | 'Revoked';
  validFrom: string;
  validUntil: string;
  createdAt: string;
}

/** Resposta da criação de delegação. */
export interface DelegationCreatedResponse {
  delegationId: string;
  validFrom: string;
  validUntil: string;
}

// ─── Enterprise: Access Review ───────────────────────────────────────────────

/** Campanha de revisão de acessos para compliance. */
export interface AccessReviewCampaign {
  id: string;
  name: string;
  scope: string;
  status: 'Open' | 'InProgress' | 'Completed' | 'Cancelled';
  totalItems: number;
  decidedItems: number;
  createdAt: string;
  completedAt: string | null;
}

/** Detalhe completo de uma campanha com itens pendentes. */
export interface AccessReviewCampaignDetail {
  id: string;
  name: string;
  scope: string;
  status: 'Open' | 'InProgress' | 'Completed' | 'Cancelled';
  items: AccessReviewItem[];
  createdAt: string;
}

/** Item individual de revisão de acesso. */
export interface AccessReviewItem {
  id: string;
  userId: string;
  userEmail: string;
  roleName: string;
  tenantName: string;
  decision: 'Pending' | 'Confirmed' | 'Revoked' | null;
  decidedBy: string | null;
  decidedAt: string | null;
  comment: string | null;
}

// ─── Engineering Graph ───────────────────────────────────────────────────────

/** Ativo de API no catálogo de engenharia. */
export interface ApiAsset {
  id: string;
  name: string;
  baseUrl: string;
  description?: string;
  ownerServiceId: string;
  createdAt: string;
}

/** Ativo de serviço no catálogo de engenharia. */
export interface ServiceAsset {
  id: string;
  name: string;
  team: string;
  description?: string;
  createdAt: string;
}

/** Relação de consumo entre API e consumidor. */
export interface ConsumerRelationship {
  apiAssetId: string;
  consumerServiceId: string;
  trustLevel: 'Inferred' | 'Low' | 'Medium' | 'High' | 'Confirmed';
}

/** Grafo completo com serviços, APIs e relações. */
export interface AssetGraph {
  services: ServiceNode[];
  apis: ApiNode[];
}

/** Nó de serviço no grafo de Engenharia. */
export interface ServiceNode {
  serviceAssetId: string;
  name: string;
  domain: string;
  teamName: string;
  serviceType: string;
  criticality: string;
  lifecycleStatus: string;
}

/** Tipo de serviço no catálogo. */
export type ServiceType =
  | 'RestApi'
  | 'SoapService'
  | 'KafkaProducer'
  | 'KafkaConsumer'
  | 'BackgroundService'
  | 'ScheduledProcess'
  | 'IntegrationComponent'
  | 'SharedPlatformService'
  | 'GraphqlApi'
  | 'GrpcService'
  | 'LegacySystem'
  | 'Gateway'
  | 'ThirdParty'
  | 'CobolProgram'
  | 'CicsTransaction'
  | 'ImsTransaction'
  | 'BatchJob'
  | 'MainframeSystem'
  | 'MqQueueManager'
  | 'ZosConnectApi'
  | 'Framework';

/** Nível de criticidade do serviço. */
export type Criticality = 'Low' | 'Medium' | 'High' | 'Critical';

/** Estado do ciclo de vida do serviço. */
export type LifecycleStatus = 'Planning' | 'Development' | 'Staging' | 'Active' | 'Deprecating' | 'Deprecated' | 'Retired';

/** Tipo de exposição do serviço. */
export type ExposureType = 'Internal' | 'External' | 'Partner';

/** Item de serviço na listagem do catálogo. */
export interface ServiceListItem {
  serviceId: string;
  name: string;
  displayName: string;
  description: string;
  serviceType: ServiceType;
  domain: string;
  systemArea: string;
  teamName: string;
  technicalOwner: string;
  criticality: Criticality;
  lifecycleStatus: LifecycleStatus;
  exposureType: ExposureType;
}

/** Resposta da listagem de serviços. */
export interface ServiceListResponse {
  items: ServiceListItem[];
  totalCount: number;
}

/** Resumo de API associada a um serviço. */
export interface ServiceApiSummary {
  apiId: string;
  name: string;
  routePattern: string;
  version: string;
  visibility: string;
  isDecommissioned: boolean;
  consumerCount: number;
}

/** Detalhe completo de um serviço do catálogo. */
export interface ServiceDetail {
  serviceId: string;
  name: string;
  displayName: string;
  description: string;
  serviceType: ServiceType;
  domain: string;
  systemArea: string;
  teamName: string;
  technicalOwner: string;
  businessOwner: string;
  criticality: Criticality;
  lifecycleStatus: LifecycleStatus;
  exposureType: ExposureType;
  documentationUrl: string;
  repositoryUrl: string;
  apis: ServiceApiSummary[];
  apiCount: number;
  totalConsumers: number;
}

/** Contagem agrupada para resumos. */
export interface GroupCount {
  key: string;
  count: number;
}

/** Resposta do resumo agregado de serviços. */
export interface ServicesSummary {
  totalCount: number;
  criticalCount: number;
  highCriticalityCount: number;
  activeCount: number;
  deprecatedCount: number;
  retiredCount: number;
  byServiceType: GroupCount[];
  byCriticality: GroupCount[];
  byLifecycle: GroupCount[];
  byDomain: GroupCount[];
  byTeam: GroupCount[];
}

/** Nó de API no grafo de engenharia. */
export interface ApiNode {
  apiAssetId: string;
  name: string;
  routePattern: string;
  version: string;
  visibility: string;
  ownerServiceAssetId: string;
  consumers: ConsumerEdge[];
}

/** Aresta de consumo no grafo. */
export interface ConsumerEdge {
  relationshipId: string;
  consumerName: string;
  sourceType: string;
  confidenceScore: number;
  lastObservedAt: string;
}

/** Nó impactado na propagação de impacto. */
export interface ImpactedNode {
  nodeId: string;
  nodeName: string;
  depth: number;
  confidenceScore: number;
  impactPath: string;
}

/** Resultado da propagação de impacto (blast radius). */
export interface ImpactPropagationResult {
  rootNodeId: string;
  rootNodeName: string;
  impactedNodes: ImpactedNode[];
  directCount: number;
  transitiveCount: number;
}

/** Resumo de snapshot temporal do grafo. */
export interface GraphSnapshotSummary {
  snapshotId: string;
  label: string;
  capturedAt: string;
  nodeCount: number;
  edgeCount: number;
  createdBy: string;
}

/** Resultado do diff temporal entre dois snapshots. */
export interface TemporalDiffResult {
  fromSnapshotId: string;
  toSnapshotId: string;
  addedNodesCount: number;
  removedNodesCount: number;
  addedEdgesCount: number;
  removedEdgesCount: number;
  fromNodesJson: string;
  toNodesJson: string;
  fromEdgesJson: string;
  toEdgesJson: string;
}

/** Registro de saúde de um nó para overlays. */
export interface NodeHealthItem {
  nodeId: string;
  nodeType: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unknown';
  score: number;
  factorsJson: string;
  calculatedAt: string;
  sourceSystem: string;
}

/** Resultado da consulta de saúde dos nós. */
export interface NodeHealthResult {
  overlayMode: string;
  items: NodeHealthItem[];
}

/** Nó de serviço no subgrafo. */
export interface SubgraphServiceNode {
  id: string;
  name: string;
  domain: string;
  teamName: string;
}

/** Nó de API no subgrafo. */
export interface SubgraphApiNode {
  id: string;
  name: string;
  routePattern: string;
  version: string;
  visibility: string;
  ownerServiceId: string;
}

/** Aresta no subgrafo. */
export interface SubgraphEdge {
  sourceId: string;
  targetId: string;
  edgeType: string;
}

/** Resultado do subgrafo contextual. */
export interface SubgraphResult {
  rootNodeId: string;
  services: SubgraphServiceNode[];
  apis: SubgraphApiNode[];
  edges: SubgraphEdge[];
  isTruncated: boolean;
}

// ─── Contracts ───────────────────────────────────────────────────────────────

/** Protocolo de contrato suportado pelo módulo multi-protocolo. */
export type ContractProtocol = 'OpenApi' | 'Swagger' | 'Wsdl' | 'AsyncApi' | 'Protobuf' | 'GraphQl' | 'WorkerService';

/** Estado do ciclo de vida de uma versão de contrato. */
export type ContractLifecycleState = 'Draft' | 'InReview' | 'Approved' | 'Locked' | 'Deprecated' | 'Sunset' | 'Retired';

/** Versão de contrato com suporte multi-protocolo, lifecycle e assinatura. */
export interface ContractVersion {
  id: string;
  apiAssetId: string;
  version: string;
  content: string;
  format: string;
  protocol: ContractProtocol;
  lifecycleState: ContractLifecycleState;
  isLocked: boolean;
  lockedAt?: string;
  lockedBy?: string;
  fingerprint?: string;
  signedBy?: string;
  signedAt?: string;
  isAiGenerated: boolean;
  deprecationNotice?: string;
  sunsetDate?: string;
  createdAt: string;
}

export interface SemanticDiff {
  fromVersion: string;
  toVersion: string;
  changes: ChangeEntry[];
  isBreaking: boolean;
  suggestedVersion: string;
  confidence?: number;
}

export interface ChangeEntry {
  path: string;
  changeType: 'Added' | 'Removed' | 'Modified';
  isBreaking: boolean;
  description: string;
}

/** Resultado do diff semântico entre duas versões de contrato. */
export interface ContractDiffResult {
  diffId: string;
  baseVersionId: string;
  targetVersionId: string;
  changeLevel: 'Breaking' | 'Additive' | 'NonBreaking';
  suggestedSemVer: string;
  breakingChanges: ChangeEntry[];
  nonBreakingChanges: ChangeEntry[];
  additiveChanges: ChangeEntry[];
}

/** Detalhe completo de uma versão de contrato com metadados e proveniência. */
export interface ContractVersionDetail {
  id: string;
  apiAssetId: string;
  semVer: string;
  specContent: string;
  format: string;
  protocol: ContractProtocol;
  lifecycleState: ContractLifecycleState;
  isLocked: boolean;
  lockedAt?: string;
  lockedBy?: string;
  fingerprint?: string;
  algorithm?: string;
  signedBy?: string;
  signedAt?: string;
  importedFrom?: string;
  provenance?: ContractProvenance;
  deprecationNotice?: string;
  deprecationDate?: string;
  sunsetDate?: string;
  createdAt: string;
  apiName?: string;
  routePattern?: string;
  apiVersion?: string;
  visibility?: string;
  serviceAssetId?: string;
  serviceName?: string;
  serviceDisplayName?: string;
  serviceDescription?: string;
  serviceType?: string;
  domain?: string;
  systemArea?: string;
  teamName?: string;
  technicalOwner?: string;
  businessOwner?: string;
  criticality?: string;
  exposureType?: string;
  documentationUrl?: string;
  repositoryUrl?: string;
  consumers?: ContractVersionDetailConsumer[];
  discoverySources?: ContractVersionDetailDiscoverySource[];
  ruleViolations?: ContractVersionDetailRuleViolation[];
  artifacts?: ContractVersionDetailArtifact[];
}

export interface ContractVersionDetailConsumer {
  id: string;
  name: string;
  kind: string;
  environment: string;
  externalReference: string;
  confidenceScore: number;
  lastObservedAt: string;
}

export interface ContractVersionDetailDiscoverySource {
  id: string;
  sourceType: string;
  externalReference: string;
  discoveredAt: string;
  confidenceScore: number;
}

export interface ContractVersionDetailArtifact {
  id: string;
  artifactType: string;
  name: string;
  contentFormat: string;
  isAiGenerated: boolean;
  generatedAt: string;
}

export interface ContractVersionDetailRuleViolation {
  ruleName: string;
  severity: string;
  message: string;
  path: string;
  suggestedFix?: string;
}

// ─── Shared Pagination ────────────────────────────────────────────────────────

export interface PagedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ─── Contract Support Types ───────────────────────────────────────────────────

export interface ContractProvenance {
  sourceType?: string;
  sourceSystem?: string;
  sourceReference?: string;
  importedAt?: string;
  importedBy?: string;
}

// ─── Governance ───────────────────────────────────────────────────────────────

export type GovernancePackCategory =
  | 'Contracts'
  | 'SourceOfTruth'
  | 'Changes'
  | 'Incidents'
  | 'AIGovernance'
  | 'Reliability'
  | 'Operations';

export type GovernancePackStatus = 'Draft' | 'Published' | 'Deprecated' | 'Archived';
export type EnforcementMode = 'Advisory' | 'SoftEnforce' | 'HardEnforce';
export type WaiverStatus = 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';
export type ComplianceStatusType = 'Compliant' | 'PartiallyCompliant' | 'NonCompliant';
export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';
export type GovernanceTrendDirection = 'Improving' | 'Stable' | 'Declining';
export type MaturityLevelType = 'Initial' | 'Developing' | 'Defined' | 'Managed' | 'Optimizing';
export type CostEfficiencyType = 'Efficient' | 'Acceptable' | 'Inefficient' | 'Wasteful';
export type PolicyCategoryType =
  | 'ServiceGovernance'
  | 'ContractGovernance'
  | 'ChangeGovernance'
  | 'OperationalReadiness'
  | 'AiGovernance'
  | 'SecurityCompliance'
  | 'DocumentationStandards';
export type PolicyStatusType = 'Draft' | 'Active' | 'Deprecated' | 'Archived';
export type ControlDimensionType =
  | 'ContractGovernance'
  | 'SourceOfTruthCompleteness'
  | 'ChangeGovernance'
  | 'IncidentMitigationEvidence'
  | 'AiGovernance'
  | 'DocumentationRunbookReadiness'
  | 'OwnershipCoverage';

export interface GovernancePackSummary {
  packId: string;
  name: string;
  displayName: string;
  description: string;
  category: GovernancePackCategory;
  status: GovernancePackStatus;
  currentVersion: string;
  scopeCount: number;
  ruleCount: number;
}

export interface GovernancePackRuleDto {
  ruleId: string;
  ruleName: string;
  description: string;
  defaultEnforcementMode: EnforcementMode;
  isRequired: boolean;
}

export interface GovernancePackScopeDto {
  scopeId: string;
  scopeType: string;
  scopeName: string;
  scopeValue: string;
  enforcementMode?: string;
  environment?: string;
}

export interface GovernancePackVersionDto {
  versionId: string;
  version: string;
  status: GovernancePackStatus;
  publishedAt?: string;
  publishedBy?: string;
  createdAt: string;
  createdBy: string;
  ruleCount: number;
}

export interface GovernancePackDetail {
  packId: string;
  name: string;
  displayName: string;
  description: string;
  category: GovernancePackCategory;
  status: GovernancePackStatus;
  currentVersion: string;
  ruleCount: number;
  scopeCount: number;
  createdAt: string;
  updatedAt: string;
  rules: GovernancePackRuleDto[];
  scopes: GovernancePackScopeDto[];
  recentVersions: GovernancePackVersionDto[];
}

export interface GovernanceWaiverDto {
  waiverId: string;
  packId?: string;
  packName: string;
  ruleName: string;
  scope: string;
  justification: string;
  requestedBy: string;
  status: WaiverStatus;
  requestedAt?: string;
  expiresAt?: string | null;
}

export interface GovernancePacksListResponse {
  packs: GovernancePackSummary[];
  totalPacks: number;
  publishedCount: number;
  draftCount: number;
}

export interface GovernanceWaiversListResponse {
  waivers: GovernanceWaiverDto[];
  totalWaivers: number;
  pendingCount: number;
  approvedCount: number;
}

export interface CreateGovernancePackRequest {
  name: string;
  displayName: string;
  description: string;
  category: GovernancePackCategory;
}

export interface CreateGovernanceWaiverRequest {
  packId: string;
  ruleId?: string;
  scope: string;
  justification: string;
  expiresAt?: string;
}

export interface ApproveWaiverRequest {
  comment?: string;
}

export interface RejectWaiverRequest {
  reason: string;
}

export interface CompliancePackRowDto {
  packId: string;
  packName: string;
  category: string;
  packStatus: string;
  status: ComplianceStatusType;
  score: number;
  rolloutCount: number;
  completedRollouts: number;
  failedRollouts: number;
  approvedWaivers: number;
  pendingWaivers: number;
}

export interface ComplianceSummaryResponse {
  overallScore: number;
  totalPacksAssessed: number;
  compliantCount: number;
  partiallyCompliantCount: number;
  nonCompliantCount: number;
  totalRollouts: number;
  completedRollouts: number;
  failedRollouts: number;
  totalWaivers: number;
  approvedWaivers: number;
  packs: CompliancePackRowDto[];
}

export interface RiskIndicatorDimensionDto {
  dimension: string;
  level: RiskLevel;
  explanation: string;
}

export interface RiskIndicatorDto {
  packId: string;
  packName: string;
  category: string;
  riskLevel: RiskLevel;
  dimensions: RiskIndicatorDimensionDto[];
}

export interface RiskSummaryResponse {
  totalPacksAssessed: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  indicators: RiskIndicatorDto[];
}

export interface PolicyDto {
  policyId: string;
  name: string;
  displayName: string;
  description: string;
  category: PolicyCategoryType;
  status: PolicyStatusType;
  severity: 'Low' | 'Medium' | 'High' | 'Critical' | null;
  enforcementMode: EnforcementMode;
  scope: string;
  effectiveEnvironments: string[];
  affectedAssetsCount: number;
  violationCount: number;
}

export interface PolicyListResponse {
  policies: PolicyDto[];
  totalPolicies: number;
  activeCount: number;
  draftCount: number;
}

export interface ExecutiveOperationalTrendDto {
  stabilityTrend: GovernanceTrendDirection;
  incidentRateChange: number;
  avgResolutionHours: number;
}

export interface ExecutiveRiskSummaryDto {
  overallRisk: RiskLevel;
  criticalDomains: number;
  highRiskServices: number;
  riskTrend: GovernanceTrendDirection;
}

export interface ExecutiveMaturitySummaryDto {
  ownershipCoverage: number;
  contractCoverage: number;
  documentationCoverage: number;
  runbookCoverage: number;
}

export interface IncidentTrendSummaryDto {
  openIncidents: number;
  resolvedLast30Days: number;
  avgResolutionHours: number;
  recurrenceRate: number;
  trend: GovernanceTrendDirection;
}

export interface CriticalFocusAreaDto {
  areaName: string;
  severity: RiskLevel;
  description: string;
  affectedServices: number;
}

export interface DomainAttentionDto {
  domainId: string;
  domainName: string;
  riskLevel: RiskLevel;
  reason: string;
}

export interface ChangeSafetySummaryDto {
  safeChanges: number;
  riskyChanges: number;
  rollbacks: number;
  confidenceTrend: GovernanceTrendDirection;
}

export interface ExecutiveOverviewResponse {
  operationalTrend: ExecutiveOperationalTrendDto;
  riskSummary: ExecutiveRiskSummaryDto;
  maturitySummary: ExecutiveMaturitySummaryDto;
  changeSafetySummary: ChangeSafetySummaryDto;
  incidentTrendSummary: IncidentTrendSummaryDto;
  criticalFocusAreas: CriticalFocusAreaDto[];
  topDomainsRequiringAttention: DomainAttentionDto[];
}

export interface ReportsSummaryResponse {
  totalPacks: number;
  publishedPacks: number;
  totalRollouts: number;
  completedRollouts: number;
  pendingRollouts: number;
  failedRollouts: number;
  complianceScore: number;
  changeConfidenceTrend: GovernanceTrendDirection;
  overallRiskLevel: RiskLevel;
  overallMaturity: MaturityLevelType;
  pendingWaivers: number;
  totalWaivers: number;
  packsWithRollout: number;
  packsWithCompletedRollout: number;
}

export interface ControlDimensionDto {
  dimension: ControlDimensionType;
  coveragePercent: number;
  maturity: MaturityLevelType;
  trend: GovernanceTrendDirection;
  totalAssessed: number;
  gapCount: number;
  summary: string;
}

export interface ControlsSummaryResponse {
  overallCoverage: number;
  overallMaturity: MaturityLevelType;
  totalDimensions: number;
  criticalGapCount: number;
  dimensions: ControlDimensionDto[];
}

export interface MaturityScorecardDimensionDto {
  dimension: string;
  level: MaturityLevelType;
  score: number;
  maxScore: number;
  explanation: string;
}

export interface MaturityScorecardDto {
  groupId: string;
  groupName: string;
  overallMaturity: MaturityLevelType;
  dimensions: MaturityScorecardDimensionDto[];
}

export interface MaturityScorecardsResponse {
  scorecards: MaturityScorecardDto[];
}

export interface RiskHeatmapCell {
  groupId: string;
  groupName: string;
  riskLevel: RiskLevel;
  riskScore: number;
  changeFailures: number;
  contractGaps: number;
  documentationGaps: number;
  runbookGaps: number;
  reliabilityDegradation: boolean;
  explanation: string;
}

export interface RiskHeatmapResponse {
  cells: RiskHeatmapCell[];
}

// ─── Organization / Governance Structure ──────────────────────────────────────

export interface TeamSummary {
  teamId: string;
  displayName: string;
  description?: string;
  parentOrganizationUnit?: string;
  status: 'Active' | 'Inactive' | 'Archived';
  maturityLevel: MaturityLevelType;
  serviceCount: number;
  contractCount: number;
  memberCount: number;
}

export interface TeamServiceDto {
  serviceId: string;
  name: string;
  domain: string;
  criticality: Criticality;
  ownershipType: string;
}

export interface TeamContractDto {
  contractId: string;
  name: string;
  type: string;
  version: string;
  status: string;
}

export interface GovernanceDimensionDto {
  dimension: string;
  level: MaturityLevelType;
  score: number;
  trend: GovernanceTrendDirection;
}

export interface GovernanceDimensionDto {
  dimension: string;
  level: MaturityLevelType;
  score: number;
  trend: GovernanceTrendDirection;
}

export interface GovernanceSummary {
  overallMaturity: MaturityLevelType;
  openRiskCount: number;
  policyViolationCount: number;
  ownershipCoverage: number;
  contractCoverage: number;
  documentationCoverage: number;
  runbookCoverage: number;
  dimensions: GovernanceDimensionDto[];
}

export interface CrossTeamDependencyDto {
  dependencyId: string;
  sourceServiceName: string;
  targetServiceName: string;
  targetTeamId: string;
  targetTeamName: string;
  dependencyType: string;
}

export interface CrossTeamDependencies {
  dependencies: CrossTeamDependencyDto[];
}

export interface TeamDetail extends TeamSummary {
  createdAt: string;
  activeIncidentCount: number;
  reliabilityScore: number;
  services: TeamServiceDto[];
  contracts: TeamContractDto[];
  crossTeamDependencies: CrossTeamDependencyDto[];
}

export interface DomainSummary {
  domainId: string;
  displayName: string;
  description?: string;
  capabilityClassification?: string;
  criticality: Criticality;
  maturityLevel: MaturityLevelType;
  teamCount: number;
  serviceCount: number;
  contractCount: number;
}

export interface DomainTeamDto {
  teamId: string;
  displayName: string;
  serviceCount: number;
  contractCount: number;
  memberCount: number;
  maturityLevel: MaturityLevelType;
  ownershipType?: string;
}

export interface DomainServiceDto {
  serviceId: string;
  name: string;
  criticality: Criticality;
  ownershipType: string;
  teamName: string;
  status?: string;
}

export interface CrossDomainDependencyDto {
  dependencyId: string;
  sourceServiceName: string;
  targetServiceName: string;
  targetDomainId: string;
  targetDomainName: string;
  dependencyType: string;
}

export interface CrossDomainDependencies {
  dependencies: CrossDomainDependencyDto[];
}

export interface DomainDetail extends DomainSummary {
  createdAt: string;
  activeIncidentCount: number;
  reliabilityScore: number;
  teams: DomainTeamDto[];
  services: DomainServiceDto[];
  crossDomainDependencies: CrossDomainDependencyDto[];
}

export interface ScopedContext {
  teamIds: string[];
  domainIds: string[];
  organizationUnitIds: string[];
}

export interface DelegatedAdminDto {
  delegationId: string;
  granteeDisplayName: string;
  scope: 'TeamAdmin' | 'DomainAdmin' | 'ReadOnly' | 'FullAdmin';
  isActive: boolean;
  teamId?: string | null;
  teamName?: string | null;
  domainId?: string | null;
  domainName?: string | null;
  reason: string;
  grantedAt: string;
  expiresAt?: string | null;
}

export interface CreateTeamRequest {
  displayName: string;
  description?: string;
  parentOrganizationUnit?: string;
}

export interface CreateDomainRequest {
  displayName: string;
  description?: string;
  capabilityClassification?: string;
  criticality?: Criticality;
}

export interface CreateDelegationRequest {
  granteeId: string;
  scope: 'TeamAdmin' | 'DomainAdmin' | 'ReadOnly' | 'FullAdmin';
  teamId?: string;
  domainId?: string;
  reason: string;
  expiresAt?: string;
}

// ─── Contract API / Studio Types ──────────────────────────────────────────────

export type ContractType =
  | 'RestApi'
  | 'SoapService'
  | 'EventContract'
  | 'BackgroundService'
  | 'SharedSchema';

export type ContractClassification = 'Breaking' | 'Additive' | 'NonBreaking' | 'Unknown';
export type DraftStatus = 'Editing' | 'InReview' | 'Approved' | 'Rejected' | 'Published';
export type ReviewDecision = 'Approved' | 'Rejected' | 'NeedsChanges';

export interface ContractRuleViolation {
  id?: string;
  ruleName: string;
  severity: string;
  message: string;
  path: string;
  suggestedFix?: string;
}

export interface ContractArtifact {
  artifactId: string;
  artifactType: string;
  name: string;
  contentFormat: string;
  isAiGenerated: boolean;
  generatedAt?: string;
}

export interface ContractIntegrityResult {
  isValid: boolean;
  errors?: string[];
  warnings?: string[];
  pathCount?: number;
  endpointCount?: number;
  schemaVersion?: string;
  validationError?: string;
}

export interface ContractSearchResult {
  contractVersionId: string;
  apiAssetId: string;
  version: string;
  protocol: ContractProtocol;
  lifecycleState: ContractLifecycleState;
}

export interface ContractSyncItem {
  contractVersionId: string;
  externalReference: string;
  status: string;
  synchronizedAt?: string;
}

export interface ContractSyncResponse {
  items: ContractSyncItem[];
  totalCount: number;
}

export interface ContractListItem {
  id?: string;
  versionId?: string;
  contractVersionId?: string;
  apiAssetId: string;
  serviceAssetId?: string;
  name?: string;
  apiName?: string;
  semVer?: string;
  version?: string;
  protocol: ContractProtocol;
  lifecycleState: ContractLifecycleState;
  isLocked?: boolean;
  format?: string;
  importedFrom?: string;
  createdAt?: string;
  updatedAt?: string;
  deprecationDate?: string;
  isSigned?: boolean;
  domain?: string;
  team?: string;
  teamName?: string;
  technicalOwner?: string;
  criticality?: string;
  exposure?: string;
  exposureType?: string;
  serviceType?: string;
  ruleViolationCount?: number;
  overallScore?: number;
}

export interface ContractListResponse {
  items: ContractListItem[];
  totalCount: number;
}

export interface ContractDeploymentItem {
  deploymentId: string;
  contractVersionId: string;
  apiAssetId: string;
  environment: string;
  semVer: string;
  status: 'Pending' | 'Success' | 'Failed' | 'Rollback';
  deployedAt: string;
  deployedBy: string;
  sourceSystem: string;
  notes?: string;
}

export interface ContractDeploymentsResponse {
  deployments: ContractDeploymentItem[];
}

export interface ContractSubscriber {
  subscriberId: string;
  subscriberEmail: string;
  consumerServiceName: string;
  consumerServiceVersion: string;
  subscriptionLevel: string;
  notificationChannel: string;
  isActive: boolean;
  subscribedAt: string;
}

export interface ContractSubscribersResponse {
  consumers: ContractSubscriber[];
  totalCount: number;
}

export interface ContractProtocolCount {
  protocol: ContractProtocol;
  count: number;
}

export interface ContractsSummary {
  totalCount: number;
  totalVersions?: number;
  distinctContracts?: number;
  byProtocol: ContractProtocolCount[];
  approvedCount: number;
  lockedCount: number;
  draftCount?: number;
  inReviewCount?: number;
  deprecatedCount?: number;
}

export interface ServiceContractItem {
  contractVersionId: string;
  versionId?: string;
  version: string;
  semVer?: string;
  apiName?: string;
  apiRoutePattern?: string;
  protocol: ContractProtocol;
  lifecycleState: ContractLifecycleState;
  isLocked?: boolean;
}

export interface ServiceContractsResponse {
  items: ServiceContractItem[];
  contracts?: ServiceContractItem[];
  totalCount: number;
}

export interface ContractDraftExample {
  id: string;
  name: string;
  description?: string;
  content: string;
  contentFormat: string;
  exampleType: string;
  createdBy: string;
  createdAt: string;
}

export interface ContractDraft {
  id: string;
  title: string;
  description?: string;
  serviceId?: string;
  contractType: ContractType;
  protocol: ContractProtocol;
  specContent: string;
  format: string;
  proposedVersion: string;
  status: DraftStatus;
  author: string;
  baseContractVersionId?: string;
  isAiGenerated: boolean;
  aiGenerationPrompt?: string;
  lastEditedAt?: string;
  lastEditedBy?: string;
  createdAt: string;
  examples: ContractDraftExample[];
}

export interface ContractReviewEntry {
  id: string;
  draftId: string;
  reviewedBy: string;
  decision: ReviewDecision;
  comment?: string;
  reviewedAt: string;
}

export interface DraftListResponse {
  items: ContractDraft[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Detalhes SOAP/WSDL específicos de uma versão de contrato publicada.
 * Retornados pelo endpoint GET /api/v1/contracts/{id}/soap-detail.
 */
export interface SoapContractDetail {
  soapDetailId: string;
  contractVersionId: string;
  serviceName: string;
  targetNamespace: string;
  soapVersion: '1.1' | '1.2';
  endpointUrl?: string | null;
  wsdlSourceUrl?: string | null;
  portTypeName?: string | null;
  bindingName?: string | null;
  extractedOperationsJson: string;
}

/**
 * Resposta da importação de WSDL.
 * Retornada pelo endpoint POST /api/v1/contracts/wsdl/import.
 */
export interface WsdlImportResponse {
  contractVersionId: string;
  apiAssetId: string;
  semVer: string;
  soapVersion: string;
  serviceName: string;
  targetNamespace: string;
  portTypeName?: string | null;
  bindingName?: string | null;
  endpointUrl?: string | null;
  extractedOperationsJson: string;
  importedAt: string;
}

/**
 * Resposta da criação de draft SOAP.
 * Retornada pelo endpoint POST /api/v1/contracts/drafts/soap.
 */
export interface SoapDraftCreateResponse {
  draftId: string;
  title: string;
  status: string;
  serviceName: string;
  targetNamespace: string;
  soapVersion: string;
  createdAt: string;
}

/**
 * Detalhes AsyncAPI específicos de uma versão de contrato de evento publicada.
 * Retornados pelo endpoint GET /api/v1/contracts/{id}/event-detail.
 */
export interface EventContractDetail {
  eventDetailId: string;
  contractVersionId: string;
  title: string;
  asyncApiVersion: string;
  defaultContentType: string;
  channelsJson: string;
  messagesJson: string;
  serversJson: string;
}

/**
 * Resposta da importação de spec AsyncAPI.
 * Retornada pelo endpoint POST /api/v1/contracts/asyncapi/import.
 */
export interface AsyncApiImportResponse {
  contractVersionId: string;
  apiAssetId: string;
  semVer: string;
  title: string;
  asyncApiVersion: string;
  defaultContentType: string;
  channelsJson: string;
  messagesJson: string;
  serversJson: string;
  importedAt: string;
}

/**
 * Resposta da criação de draft de evento.
 * Retornada pelo endpoint POST /api/v1/contracts/drafts/event.
 */
export interface EventDraftCreateResponse {
  draftId: string;
  title: string;
  status: string;
  asyncApiVersion: string;
  defaultContentType: string;
  createdAt: string;
}

/**
 * Detalhes de Background Service Contract de uma versão publicada.
 * Retornados pelo endpoint GET /api/v1/contracts/{id}/background-service-detail.
 */
export interface BackgroundServiceContractDetail {
  detailId: string;
  contractVersionId: string;
  serviceName: string;
  category: string;
  triggerType: string;
  scheduleExpression?: string | null;
  timeoutExpression?: string | null;
  allowsConcurrency: boolean;
  inputsJson: string;
  outputsJson: string;
  sideEffectsJson: string;
}

/**
 * Resposta do registo de Background Service Contract.
 * Retornada pelo endpoint POST /api/v1/contracts/background-services/register.
 */
export interface BackgroundServiceRegisterResponse {
  contractVersionId: string;
  apiAssetId: string;
  semVer: string;
  serviceName: string;
  category: string;
  triggerType: string;
  scheduleExpression?: string | null;
  timeoutExpression?: string | null;
  allowsConcurrency: boolean;
  registeredAt: string;
}

/**
 * Resposta da criação de draft de Background Service.
 * Retornada pelo endpoint POST /api/v1/contracts/drafts/background-service.
 */
export interface BackgroundServiceDraftCreateResponse {
  draftId: string;
  title: string;
  status: string;
  serviceName: string;
  category: string;
  triggerType: string;
  scheduleExpression?: string | null;
  createdAt: string;
}

/**
 * Entrada do Publication Center — governa a visibilidade de um contrato no Developer Portal.
 */
export interface ContractPublicationEntry {
  publicationEntryId: string;
  contractVersionId: string;
  apiAssetId: string;
  contractTitle: string;
  semVer: string;
  status: ContractPublicationStatus;
  visibility: PublicationVisibility;
  publishedBy: string;
  publishedAt?: string | null;
  withdrawnAt?: string | null;
  withdrawalReason?: string | null;
  releaseNotes?: string | null;
}

/** Estado de publicação de um contrato no Developer Portal. */
export type ContractPublicationStatus =
  | 'PendingPublication'
  | 'Published'
  | 'Withdrawn'
  | 'Deprecated'
  | 'NotPublished';

/** Escopo de visibilidade no Developer Portal. */
export type PublicationVisibility = 'Internal' | 'External' | 'RestrictedToTeams';

/**
 * Resposta da publicação de um contrato no Developer Portal.
 * Retornada pelo endpoint POST /api/v1/publication-center/publish.
 */
export interface PublishContractToPortalResponse {
  publicationEntryId: string;
  contractVersionId: string;
  apiAssetId: string;
  contractTitle: string;
  semVer: string;
  status: ContractPublicationStatus;
  visibility: PublicationVisibility;
  publishedAt: string;
}

/**
 * Resposta da retirada de publicação do Developer Portal.
 * Retornada pelo endpoint POST /api/v1/publication-center/{entryId}/withdraw.
 */
export interface WithdrawContractFromPortalResponse {
  publicationEntryId: string;
  contractVersionId: string;
  status: ContractPublicationStatus;
  withdrawnBy: string;
  withdrawnAt: string;
  withdrawalReason?: string | null;
}

/**
 * Estado de publicação de uma versão de contrato específica.
 * Retornado pelo endpoint GET /api/v1/publication-center/contracts/{id}/status.
 */
export interface ContractPublicationStatusResponse {
  contractVersionId: string;
  isPublished: boolean;
  status: ContractPublicationStatus;
  publicationEntryId?: string | null;
  visibility?: PublicationVisibility | null;
  publishedBy?: string | null;
  publishedAt?: string | null;
  releaseNotes?: string | null;
}

/**
 * Lista paginada de entradas do Publication Center.
 * Retornada pelo endpoint GET /api/v1/publication-center.
 */
export interface PublicationCenterListResponse {
  items: ContractPublicationEntry[];
  totalCount: number;
}

export interface SignatureVerificationResult {
  contractVersionId: string;
  hasSignature: boolean;
  isValid: boolean;
  fingerprint?: string | null;
  algorithm?: string | null;
  verificationMessage: string;
}

// ─── Change Governance / Workflow ─────────────────────────────────────────────

export type ChangeLevel = 0 | 1 | 2 | 3 | 4;
export type DeploymentState = 'Planned' | 'Pending' | 'Running' | 'Succeeded' | 'Failed' | 'RolledBack';

export interface Release {
  id: string;
  apiAssetId: string;
  version: string;
  environment: string;
  status: DeploymentState;
  deploymentState?: DeploymentState;
  changeLevel: ChangeLevel;
  riskScore?: number;
  description?: string;
  changeType?: string;
  commitSha?: string;
  pipelineSource?: string | null;
  workItemReference?: string | null;
  createdAt: string;
}

export interface ChangeDto {
  id: string;
  changeId?: string;
  serviceName: string;
  teamName?: string;
  version: string;
  environment: string;
  changeType?: string;
  description?: string;
  commitSha?: string;
  pipelineSource?: string | null;
  workItemReference?: string | null;
  changeScore: number;
  changeLevel?: number;
  confidenceStatus: string;
  deploymentStatus?: string;
  validationStatus?: string;
  createdAt: string;
}

export interface DecisionHistoryItemDto {
  decisionId: string;
  eventId: string;
  decision: string;
  rationale?: string;
  conditions?: string;
  decidedBy: string;
  decidedAt: string;
  description: string;
  eventType: string;
  source: string;
  occurredAt: string;
}

export interface PromotionGateResult {
  gateName: string;
  passed: boolean;
  message?: string;
}

export interface PromotionRequest {
  id: string;
  releaseId: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Promoted';
  gateResults: PromotionGateResult[];
  createdAt: string;
}

export interface BlastRadiusReport {
  releaseId: string;
  totalAffectedConsumers: number;
  directConsumers: string[];
  transitiveConsumers: string[];
  calculatedAt: string;
}

export interface ChangeScore {
  releaseId: string;
  score: number;
  breakingChangeWeight: number;
  blastRadiusWeight: number;
  environmentWeight: number;
  computedAt: string;
}

export interface WorkflowTemplateStage {
  id: string;
  name: string;
}

export interface WorkflowTemplate {
  id: string;
  name: string;
  changeLevel: ChangeLevel;
  stages: WorkflowTemplateStage[];
}

export interface WorkflowInstance {
  id: string;
  releaseId?: string;
  status: 'Pending' | 'InProgress' | 'Approved' | 'Rejected' | 'Cancelled';
  currentStage?: string | null;
  createdAt: string;
}

export interface AdvisoryFactorDto {
  factorName: string;
  status: 'Pass' | 'Warning' | 'Fail' | 'Unknown';
  explanation: string;
  description?: string;
  weight?: number;
}

export interface ChangeAdvisoryResponse {
  recommendation: 'Approve' | 'ApproveConditionally' | 'Reject' | 'NeedsMoreEvidence' | string;
  confidenceScore: number;
  overallConfidence: number;
  rationale?: string;
  factors: AdvisoryFactorDto[];
}

export interface ChangesListResponse {
  items: ChangeDto[];
  changes?: ChangeDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ChangesSummaryResponse {
  totalChanges: number;
  changesNeedingAttention: number;
  suspectedRegressions: number;
  validatedChanges?: number;
  changesCorrelatedWithIncidents?: number;
}

export interface RecordDecisionRequest {
  decision: 'Approved' | 'Rejected' | 'ApprovedConditionally';
  rationale?: string;
  conditions?: string;
  decidedBy: string;
}

export interface RecordDecisionResponse {
  decisionId: string;
  recordedAt: string;
}

export interface DecisionHistoryResponse {
  items: DecisionHistoryItemDto[];
  decisions?: DecisionHistoryItemDto[];
}

export interface SourceOfTruthReferenceItem {
  referenceId: string;
  title: string;
  description: string;
  assetType: string;
  referenceType: string;
  url?: string;
}

export interface SourceOfTruthSearchResponse {
  services: ServiceDetail[];
  contracts: Array<ContractVersionDetail & { versionId?: string }>;
  references: SourceOfTruthReferenceItem[];
  totalResults?: number;
}

export interface CoverageIndicators {
  hasOwner: boolean;
  hasContracts: boolean;
  hasDocumentation: boolean;
  hasRunbook: boolean;
  hasRecentChangeHistory: boolean;
  hasDependenciesMapped: boolean;
  hasEventTopics: boolean;
}

export interface ServiceCoverageResponse {
  metIndicators: number;
  totalIndicators: number;
  coverageScore?: number;
}

export interface ServiceSourceOfTruth {
  serviceId: string;
  name: string;
  displayName: string;
  description?: string;
  domain: string;
  systemArea?: string;
  serviceType?: string;
  teamName: string;
  criticality?: string;
  lifecycleStatus?: string;
  exposureType?: string;
  technicalOwner?: string;
  businessOwner?: string;
  documentationUrl?: string;
  repositoryUrl?: string;
  totalApis: number;
  totalContracts: number;
  totalReferences: number;
  apis: ServiceApiSummary[];
  contracts: Array<ContractVersionDetail & { versionId?: string }>;
  references: SourceOfTruthReferenceItem[];
  coverage: CoverageIndicators;
}

export type GenerationType = 'ClientSdk' | 'ServerStub' | 'Example';

export interface ContractSourceOfTruth {
  apiAssetId: string;
  semVer: string;
  protocol: ContractProtocol;
  format?: string;
  importedFrom?: string;
  artifactCount: number;
  diffCount: number;
  violationCount: number;
  governance: {
    lifecycleState: ContractLifecycleState;
    isLocked?: boolean;
    isSigned?: boolean;
    deprecationNotice?: string;
    deprecationDate?: string;
    sunsetDate?: string;
  };
  references: SourceOfTruthReferenceItem[];
}

export interface AuditEvent {
  eventId: string;
  id?: string;
  occurredAt: string;
  actor: string;
  actorEmail?: string;
  action: string;
  eventType?: string;
  sourceModule?: string;
  entityType?: string;
  aggregateType?: string;
  entityId?: string;
  correlationId?: string;
  hash?: string;
}

// ── Product Analytics ──────────────────────────────────────────────────────────

export interface ModuleAdoptionDto {
  module: string;
  moduleName: string;
  adoptionPercent: number;
  totalActions: number;
  uniqueUsers: number;
  depthScore: number;
  trend: GovernanceTrendDirection;
  topFeatures: string[];
}

export interface ModuleAdoptionResponse {
  modules: ModuleAdoptionDto[];
  overallAdoptionScore: number;
  mostAdopted: string;
  leastAdopted: string;
  biggestGrowth: string;
  periodLabel: string;
}

export interface ProductAnalyticsModuleUsageDto {
  module: string;
  moduleName: string;
  eventCount: number;
  uniqueUsers: number;
  trend: GovernanceTrendDirection;
}

export interface ProductAnalyticsSummaryResponse {
  totalEvents: number;
  uniqueUsers: number;
  activePersonas: number;
  topModules: ProductAnalyticsModuleUsageDto[];
  adoptionScore: number;
  valueScore: number;
  frictionScore: number;
  avgTimeToFirstValueMinutes: number;
  avgTimeToCoreValueMinutes: number;
  trendDirection: GovernanceTrendDirection;
  periodLabel: string;
}

// ── Benchmarking ───────────────────────────────────────────────────────────────

export interface BenchmarkComparisonDto {
  groupId: string;
  groupName: string;
  serviceCount: number;
  criticality: Criticality | null;
  reliabilityScore: number | null;
  reliabilityTrend: GovernanceTrendDirection | null;
  changeSafetyScore: number | null;
  incidentRecurrenceRate: number | null;
  maturityScore: number | null;
  riskScore: number | null;
  finopsEfficiency: CostEfficiencyType;
  strengths: string[];
  gaps: string[];
  context: string;
}

export interface BenchmarkingResponse {
  dimension: string;
  comparisons: BenchmarkComparisonDto[];
  generatedAt?: string;
}

// ── Evidence Packages ──────────────────────────────────────────────────────────

export type EvidencePackageStatusType = 'Draft' | 'Sealed' | 'Exported';

export type EvidenceTypeValue =
  | 'Approval'
  | 'ChangeHistory'
  | 'ContractPublication'
  | 'ComplianceResult'
  | 'AiUsageRecord'
  | 'MitigationRecord'
  | 'SecurityReview'
  | 'ChangeValidation'
  | 'PolicyDecision'
  | 'ModelRegistrySnapshot'
  | 'TokenUsage'
  | 'AuditReference';

export interface EvidencePackageDto {
  packageId: string;
  name: string;
  description: string;
  scope: string;
  status: EvidencePackageStatusType;
  itemCount: number;
  includedTypes: string[];
  createdBy: string;
  createdAt: string;
  sealedAt: string | null;
}

export interface EvidenceItemDto {
  itemId: string;
  type: string;
  title: string;
  description: string;
  sourceModule: string;
  referenceId: string;
  recordedBy: string;
  recordedAt: string;
}

export interface EvidencePackageListResponse {
  totalPackages: number;
  sealedCount: number;
  exportedCount: number;
  draftCount: number;
  packages: EvidencePackageDto[];
}

// ── Executive Drill-Down ───────────────────────────────────────────────────────

export interface ExecutiveDrillDownIndicatorDto {
  name: string;
  value: string;
  trend: GovernanceTrendDirection;
  explanation: string;
}

export interface ExecutiveDrillDownCriticalServiceDto {
  serviceId: string;
  serviceName: string;
  riskLevel: RiskLevel;
  mainIssue: string;
}

export interface ExecutiveDrillDownGapDto {
  area: string;
  severity: RiskLevel;
  description: string;
  recommendation: string;
}

export interface ExecutiveDrillDownResponse {
  entityType: string;
  entityId: string;
  entityName: string;
  riskLevel: RiskLevel;
  maturityLevel: MaturityLevelType;
  keyIndicators: ExecutiveDrillDownIndicatorDto[];
  criticalServices: ExecutiveDrillDownCriticalServiceDto[];
  topGaps: ExecutiveDrillDownGapDto[];
  recommendedFocus: string[];
  generatedAt: string;
}

// ── Developer Portal ───────────────────────────────────────────────────────────

export type SubscriptionLevel = 'BreakingChangesOnly' | 'AllChanges' | 'DeprecationNotices' | 'SecurityAdvisories';
export type NotificationChannel = 'Email' | 'Webhook' | 'Teams' | 'Slack';

export interface CatalogItem {
  apiAssetId: string;
  name: string;
  apiName?: string;
  displayName?: string;
  description?: string;
  version?: string;
  healthStatus?: string;
  ownerServiceName?: string;
}

export interface Subscription {
  id: string;
  apiAssetId: string;
  apiName: string;
  subscriberEmail: string;
  consumerServiceName: string;
  level: SubscriptionLevel;
  channel: NotificationChannel;
  isActive?: boolean;
}

export interface PlaygroundResult {
  statusCode: number;
  responseStatusCode?: number;
  responseBody: string;
  durationMs: number;
  executedAt?: string;
}

export interface PlaygroundHistoryItem {
  id: string;
  sessionId?: string;
  apiAssetId: string;
  apiName?: string;
  httpMethod?: string;
  requestPath: string;
  executedAt: string;
  statusCode: number;
  responseStatusCode?: number;
  durationMs?: number;
}

export interface PortalAnalytics {
  totalExecutions: number;
  totalSubscriptions: number;
  totalSearches?: number;
  totalApiViews?: number;
  totalPlaygroundExecutions?: number;
  totalCodeGenerations?: number;
  popularApis: Array<{ apiAssetId: string; count: number }>;
  topSearches?: Array<{ term: string; count: number }>;
}

// ── FinOps ──────────────────────────────────────────────────────

export interface FinOpsWasteSignalDto {
  description: string;
  pattern: string;
  type: string;
  estimatedWaste: number;
}

export interface FinOpsReliabilityCorrelationDto {
  reliabilityScore: number;
  recentIncidents: number;
  reliabilityTrend: GovernanceTrendDirection;
}

export interface FinOpsServiceCostDto {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  efficiency: CostEfficiencyType;
  monthlyCost: number;
  trend: GovernanceTrendDirection;
  wasteSignals: FinOpsWasteSignalDto[];
  reliabilityCorrelation: FinOpsReliabilityCorrelationDto | null;
}

export interface FinOpsCostDriverDto {
  serviceId: string;
  serviceName: string;
  monthlyCost: number;
  efficiency: CostEfficiencyType;
}

export interface FinOpsOptimizationDto {
  serviceId: string;
  serviceName: string;
  potentialSavings: number;
  priority: string;
  recommendation: string;
}

export interface FinOpsSummaryResponse {
  totalMonthlyCost: number;
  totalWaste: number;
  overallEfficiency: CostEfficiencyType;
  costTrend: GovernanceTrendDirection;
  services: FinOpsServiceCostDto[];
  topCostDrivers: FinOpsCostDriverDto[];
  topWasteSignals: FinOpsWasteSignalDto[];
  optimizationOpportunities: FinOpsOptimizationDto[];
  generatedAt: string;
}

export interface ServiceFinOpsResponse {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  efficiency: CostEfficiencyType;
  monthlyCost: number;
  trend: GovernanceTrendDirection;
  wasteSignals: FinOpsWasteSignalDto[];
  reliabilityCorrelation: FinOpsReliabilityCorrelationDto | null;
  optimizationOpportunities: FinOpsOptimizationDto[];
  generatedAt: string;
}

export interface TeamFinOpsResponse {
  teamId: string;
  teamName: string;
  totalCost: number;
  efficiency: CostEfficiencyType;
  trend: GovernanceTrendDirection;
  services: FinOpsServiceCostDto[];
  topWasteSignals: FinOpsWasteSignalDto[];
  optimizationOpportunities: FinOpsOptimizationDto[];
  generatedAt: string;
}

export interface DomainFinOpsResponse {
  domainId: string;
  domainName: string;
  totalCost: number;
  efficiency: CostEfficiencyType;
  trend: GovernanceTrendDirection;
  services: FinOpsServiceCostDto[];
  topWasteSignals: FinOpsWasteSignalDto[];
  optimizationOpportunities: FinOpsOptimizationDto[];
  generatedAt: string;
}

export interface FinOpsTrendPointDto {
  period: string;
  totalCost: number;
  waste: number;
  efficiency: CostEfficiencyType;
}

export interface FinOpsTrendsResponse {
  dimension: string;
  filterId: string | null;
  points: FinOpsTrendPointDto[];
  generatedAt: string;
}

// ── Integration Hub ─────────────────────────────────────────────

export interface IntegrationConnectorDto {
  connectorId: string;
  name: string;
  connectorType: string;
  provider: string;
  status: string;
  environment: string;
  lastSyncAt: string | null;
  syncFrequency: string;
  healthScore: number;
  dataDomainsCount: number;
  sourcesCount: number;
  createdAt: string;
}

export interface IntegrationConnectorsListResponse {
  connectors: IntegrationConnectorDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IntegrationFilterOptionsResponse {
  connectorTypes: string[];
  connectorStatuses: string[];
  connectorHealthStatuses: string[];
}

export interface IntegrationConnectorDetailDto {
  connectorId: string;
  name: string;
  connectorType: string;
  provider: string;
  status: string;
  environment: string;
  description: string;
  lastSyncAt: string | null;
  syncFrequency: string;
  healthScore: number;
  configuration: Record<string, string>;
  dataDomains: string[];
  sources: IngestionSourceDto[];
  recentExecutions: IngestionExecutionDto[];
  createdAt: string;
  updatedAt: string;
}

export interface IngestionSourceDto {
  sourceId: string;
  name: string;
  dataDomain: string;
  trustLevel: string;
  status: string;
  lastIngestionAt: string | null;
  recordCount: number;
}

export interface IngestionSourcesListResponse {
  sources: IngestionSourceDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IngestionExecutionDto {
  executionId: string;
  connectorId: string;
  connectorName: string;
  sourceId: string | null;
  sourceName: string | null;
  result: string;
  recordsProcessed: number;
  recordsFailed: number;
  startedAt: string;
  completedAt: string | null;
  durationMs: number;
  errorMessage: string | null;
}

export interface IngestionExecutionsListResponse {
  executions: IngestionExecutionDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IngestionHealthDto {
  connectorId: string | null;
  overallHealth: string;
  activeConnectors: number;
  totalConnectors: number;
  failedExecutions24h: number;
  successRate: number;
  avgProcessingTimeMs: number;
}

export interface FreshnessIndicatorDto {
  dataDomain: string;
  latestIngestionAt: string;
  freshnessStatus: string;
  staleSources: number;
  totalSources: number;
}

export interface IngestionFreshnessResponse {
  indicators: FreshnessIndicatorDto[];
  generatedAt: string;
}

// ── Reliability ─────────────────────────────────────────────────

export interface ServiceReliabilityItem {
  serviceName: string;
  displayName: string;
  serviceType: string;
  domain: string;
  teamName: string;
  criticality: string;
  reliabilityStatus: string;
  operationalSummary: string;
  trend: string;
  activeFlags: number;
  openIncidents: number;
  recentChangeImpact: boolean;
  overallScore: number;
  lastComputedAt: string;
}

export interface ServiceReliabilityListResponse {
  items: ServiceReliabilityItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ServiceReliabilityDetailIdentity {
  serviceId: string;
  displayName: string;
  serviceType: string;
  domain: string;
  teamName: string;
  criticality: string;
}

export interface ServiceReliabilityDetailMetrics {
  availabilityPercent: number;
  latencyP99Ms: number;
  errorRatePercent: number;
  requestsPerSecond: number;
  queueLag: number | null;
  processingDelay: number | null;
}

export interface ServiceReliabilityDetailTrend {
  direction: string;
  timeframe: string;
  summary: string;
}

export interface ServiceReliabilityCoverage {
  hasOperationalSignals: boolean;
  hasRunbook: boolean;
  hasOwner: boolean;
  hasDependenciesMapped: boolean;
  hasRecentChangeContext: boolean;
  hasIncidentLinkage: boolean;
}

export interface ServiceReliabilityDetailIncident {
  incidentId: string;
  reference: string;
  title: string;
  status: string;
  reportedAt: string;
}

export interface ServiceReliabilityDetailChange {
  changeId: string;
  description: string;
  changeType: string;
  confidenceStatus: string;
  deployedAt: string;
}

export interface ServiceReliabilityDetailDependency {
  serviceId: string;
  displayName: string;
  status: string;
}

export interface ServiceReliabilityDetailContract {
  contractVersionId: string;
  name: string;
  version: string;
  protocol: string;
  lifecycleState: string;
}

export interface ServiceReliabilityDetailRunbook {
  title: string;
  url: string | null;
}

export interface ServiceReliabilityDetailResponse {
  identity: ServiceReliabilityDetailIdentity;
  status: string;
  operationalSummary: string;
  trend: ServiceReliabilityDetailTrend;
  metrics: ServiceReliabilityDetailMetrics;
  activeFlags: number;
  recentChanges: ServiceReliabilityDetailChange[];
  linkedIncidents: ServiceReliabilityDetailIncident[];
  dependencies: ServiceReliabilityDetailDependency[];
  linkedContracts: ServiceReliabilityDetailContract[];
  runbooks: ServiceReliabilityDetailRunbook[];
  anomalySummary: string;
  coverage: ServiceReliabilityCoverage;
}

export interface TeamReliabilitySummaryResponse {
  teamId: string;
  totalServices: number;
  healthyServices: number;
  degradedServices: number;
  unavailableServices: number;
  needsAttentionServices: number;
  criticalServicesImpacted: number;
  openIncidents: number;
  overallScore: number;
  trend: string;
}

// ── Platform Operations ─────────────────────────────────────────

export interface PlatformHealthMetric {
  name: string;
  value: string;
  status: string;
  trend: GovernanceTrendDirection;
}

export interface PlatformOperationsResponse {
  health: PlatformHealthMetric[];
  activeAlerts: number;
  resolvedAlerts24h: number;
  systemUptime: string;
  generatedAt: string;
}

// ── Platform Operations: Health ──────────────────────────────────

export type PlatformSubsystemStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface SubsystemHealthDto {
  name: string;
  status: PlatformSubsystemStatus;
  description: string;
  lastCheckedAt: string;
}

export interface PlatformHealthResponse {
  overallStatus: PlatformSubsystemStatus;
  subsystems: SubsystemHealthDto[];
  uptimeSeconds: number;
  version: string;
  checkedAt: string;
}

// ── Platform Operations: Jobs ────────────────────────────────────

export type BackgroundJobStatus = 'Running' | 'Completed' | 'Failed' | 'Scheduled' | 'Disabled';

export interface BackgroundJobSummaryDto {
  jobId: string;
  name: string;
  status: BackgroundJobStatus;
  lastRunAt: string;
  nextRunAt: string | null;
  executionCount: number;
  failureCount: number;
  lastError: string | null;
}

export interface PlatformJobsResponse {
  jobs: BackgroundJobSummaryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Platform Operations: Queues ──────────────────────────────────

export interface QueueSummaryDto {
  queueName: string;
  pendingCount: number;
  processingCount: number;
  failedCount: number;
  deadLetterCount: number;
  averageProcessingMs: number;
  lastActivityAt: string;
}

export interface PlatformQueuesResponse {
  queues: QueueSummaryDto[];
  checkedAt: string;
}

// ── Platform Operations: Events ──────────────────────────────────

export type PlatformEventSeverity = 'Info' | 'Warning' | 'Error' | 'Critical';

export interface PlatformOperationalEventDto {
  eventId: string;
  timestamp: string;
  severity: PlatformEventSeverity;
  subsystem: string;
  message: string;
  correlationId: string | null;
  resolved: boolean;
}

export interface PlatformEventsResponse {
  events: PlatformOperationalEventDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Platform Operations: Config ──────────────────────────────────

export interface FeatureFlagDto {
  name: string;
  enabled: boolean;
  description: string;
}

export interface SubsystemConfigDto {
  name: string;
  enabled: boolean;
  description: string;
}

export interface DatabaseConnectivityDto {
  name: string;
  provider: string;
  connected: boolean;
  statusDescription: string;
}

export interface PlatformConfigResponse {
  environmentName: string;
  deploymentMode: string;
  featureFlags: FeatureFlagDto[];
  subsystems: SubsystemConfigDto[];
  databases: DatabaseConnectivityDto[];
  generatedAt: string;
}

// ── Product Analytics: Persona Usage ────────────────────────────

export interface PersonaModuleUsageDto {
  module: string;
  adoptionPercent: number;
  actionCount: number;
}

export interface PersonaUsageProfileDto {
  persona: string;
  activeUsers: number;
  totalActions: number;
  topModules: PersonaModuleUsageDto[];
  topActions: string[];
  adoptionDepth: number;
  commonFrictionPoints: string[];
  milestonesReached: string[];
}

export interface PersonaUsageResponse {
  profiles: PersonaUsageProfileDto[];
  totalPersonas: number;
  mostActivePersona: string;
  deepestAdoptionPersona: string;
  periodLabel: string;
}

// ── Product Analytics: Journeys ─────────────────────────────────

export interface JourneyStepDto {
  stepId: string;
  stepName: string;
  completionPercent: number;
  order: number;
}

export interface JourneyItemDto {
  journeyId: string;
  journeyName: string;
  steps: JourneyStepDto[];
  completionRate: number;
  avgDurationMinutes: number;
  status: string;
  biggestDropOff: string;
}

export interface JourneysResponse {
  journeys: JourneyItemDto[];
  averageCompletionRate: number;
  mostCompletedJourney: string;
  highestDropOffJourney: string;
  periodLabel: string;
}

// ── Product Analytics: Value Milestones ─────────────────────────

export type MilestoneTrend = 'Improving' | 'Stable' | 'Declining';

export interface MilestoneItemDto {
  milestoneType: string;
  milestoneName: string;
  completionRate: number;
  avgTimeToReachMinutes: number;
  usersReached: number;
  trend: MilestoneTrend;
}

export interface ValueMilestonesResponse {
  milestones: MilestoneItemDto[];
  avgTimeToFirstValueMinutes: number;
  avgTimeToCoreValueMinutes: number;
  overallCompletionRate: number;
  fastestMilestone: string;
  slowestMilestone: string;
  periodLabel: string;
}

// ── Product Analytics: Friction Indicators ──────────────────────

export interface FrictionIndicatorDto {
  indicator: string;
  module: string;
  persona: string;
  occurrences: number;
  impact: string;
  trend: GovernanceTrendDirection;
}

export interface FrictionIndicatorsResponse {
  indicators: FrictionIndicatorDto[];
  generatedAt: string;
}

// ── Product Analytics: Adoption Funnel ──────────────────────────

export interface AdoptionFunnelStepDto {
  stepId: string;
  stepName: string;
  sessionCount: number;
  completionPercent: number;
}

export interface ModuleFunnelDto {
  module: string;
  moduleName: string;
  steps: AdoptionFunnelStepDto[];
  completionRate: number;
  totalSessions: number;
  biggestDropOff: string;
}

export interface AdoptionFunnelResponse {
  funnels: ModuleFunnelDto[];
  periodLabel: string;
}

// ── Product Analytics: Feature Heatmap ──────────────────────────

export interface FeatureUsageDto {
  feature: string;
  count: number;
}

export interface HeatmapCellDto {
  module: string;
  moduleName: string;
  adoptionPercent: number;
  totalActions: number;
  uniqueUsers: number;
  intensity: number;
  topFeatures: FeatureUsageDto[];
}

export interface FeatureHeatmapResponse {
  cells: HeatmapCellDto[];
  modules: string[];
  maxIntensity: number;
  totalUniqueUsers: number;
  periodLabel: string;
}

// ── Knowledge Hub ───────────────────────────────────────────────

export type DocumentCategory =
  | 'General'
  | 'Runbook'
  | 'Troubleshooting'
  | 'Architecture'
  | 'Procedure'
  | 'PostMortem'
  | 'Reference';

export type DocumentStatus = 'Draft' | 'Published' | 'Archived' | 'Deprecated';

export type NoteSeverity = 'Info' | 'Warning' | 'Critical';

export type OperationalNoteType =
  | 'Observation'
  | 'Mitigation'
  | 'Decision'
  | 'Hypothesis'
  | 'FollowUp';

export type KnowledgeRelationType =
  | 'Service'
  | 'Contract'
  | 'Change'
  | 'Incident'
  | 'KnowledgeDocument'
  | 'Runbook'
  | 'Other';

export interface KnowledgeDocumentSummary {
  documentId: string;
  title: string;
  slug: string;
  summary: string | null;
  category: DocumentCategory;
  status: DocumentStatus;
  tags: string[];
  authorId: string;
  version: number;
  createdAt: string;
  updatedAt: string | null;
  publishedAt: string | null;
}

export interface KnowledgeDocumentDetail {
  documentId: string;
  title: string;
  slug: string;
  content: string;
  summary: string | null;
  category: DocumentCategory;
  status: DocumentStatus;
  tags: string[];
  authorId: string;
  lastEditorId: string | null;
  version: number;
  createdAt: string;
  updatedAt: string | null;
  publishedAt: string | null;
}

export interface OperationalNoteDto {
  noteId: string;
  title: string;
  content: string;
  severity: NoteSeverity;
  noteType: OperationalNoteType;
  origin: string;
  authorId: string;
  contextEntityId: string | null;
  contextType: string | null;
  tags: string[];
  isResolved: boolean;
  createdAt: string;
  updatedAt: string | null;
  resolvedAt: string | null;
}

export interface KnowledgeRelationDto {
  relationId: string;
  sourceEntityId: string;
  targetEntityId: string;
  targetEntityType: KnowledgeRelationType;
  relationType: string;
  createdAt: string;
}

export interface KnowledgeDocumentRelationItem {
  relationId: string;
  documentId: string;
  title: string;
  slug: string;
  status: string;
  category: string;
  relationDescription: string | null;
  relationContext: string | null;
  relationCreatedAt: string;
}

export interface OperationalNoteRelationItem {
  relationId: string;
  noteId: string;
  title: string;
  severity: string;
  noteType: string;
  origin: string;
  isResolved: boolean;
  relationDescription: string | null;
  relationContext: string | null;
  relationCreatedAt: string;
}

export interface KnowledgeSearchItem {
  entityId: string;
  entityType: string;
  title: string;
  subtitle: string | null;
  status: string | null;
  route: string;
  relevanceScore: number;
}

export interface KnowledgeSearchResponse {
  items: KnowledgeSearchItem[];
  totalResults: number;
}

export interface KnowledgeDocumentsListResponse {
  items: KnowledgeDocumentSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OperationalNotesListResponse {
  items: OperationalNoteDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateKnowledgeDocumentRequest {
  title: string;
  content: string;
  summary?: string;
  category: DocumentCategory;
  tags?: string[];
}

export interface CreateOperationalNoteRequest {
  title: string;
  content: string;
  severity: NoteSeverity;
  noteType: OperationalNoteType;
  contextEntityId?: string;
  contextType?: string;
  tags?: string[];
}

export interface CreateKnowledgeRelationRequest {
  sourceEntityId: string;
  targetEntityId: string;
  targetEntityType: KnowledgeRelationType;
  relationType: string;
}
