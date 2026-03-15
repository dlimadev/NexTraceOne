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

/** Nó de serviço no grafo de engenharia. */
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
export type ServiceType = 'RestApi' | 'SoapService' | 'KafkaProducer' | 'KafkaConsumer' | 'BackgroundService' | 'ScheduledProcess' | 'IntegrationComponent' | 'SharedPlatformService';

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
export type ContractProtocol = 'OpenApi' | 'Swagger' | 'Wsdl' | 'AsyncApi' | 'Protobuf' | 'GraphQl';

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
  sunsetDate?: string;
  createdAt: string;
}

/** Resultado da verificação de integridade da assinatura de um contrato. */
export interface SignatureVerificationResult {
  isValid: boolean;
  message: string;
}

/** Informação de proveniência da importação de um contrato. */
export interface ContractProvenance {
  origin: string;
  originalFormat: string;
  parserUsed: string;
  standardVersion: string;
  importedBy: string;
  isAiGenerated: boolean;
}

/** Classificação de mudança de contrato. */
export interface ContractClassification {
  contractVersionId: string;
  changeLevel: 'Breaking' | 'Additive' | 'NonBreaking';
  breakingChangeCount: number;
  nonBreakingChangeCount: number;
  additiveChangeCount: number;
}

/** Violação de regra de governança em contrato. */
export interface ContractRuleViolation {
  id: string;
  ruleName: string;
  severity: string;
  message: string;
  path: string;
}

/** Artefato gerado a partir de contrato. */
export interface ContractArtifact {
  id: string;
  name: string;
  artifactType: string;
  content: string;
  createdAt: string;
}

/** Resultado de busca paginada de contratos. */
export interface ContractSearchResult {
  items: ContractVersion[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Resultado da validação de integridade estrutural de um contrato. */
export interface ContractIntegrityResult {
  isValid: boolean;
  pathCount: number;
  endpointCount: number;
  schemaVersion?: string;
  validationError?: string;
}

/** Item individual de sincronização em lote de contratos externos. */
export interface ContractSyncItem {
  apiAssetId: string;
  semVer: string;
  specContent: string;
  format: 'json' | 'yaml' | 'xml';
  importedFrom: string;
  protocol?: ContractProtocol;
}

/** Resultado do processamento de um item na sincronização em lote. */
export interface ContractSyncItemResult {
  apiAssetId: string;
  semVer: string;
  status: 'Created' | 'Skipped' | 'Failed';
  contractVersionId?: string;
  errorMessage?: string;
}

/** Resposta da sincronização em lote de contratos externos (CI/CD). */
export interface ContractSyncResponse {
  totalProcessed: number;
  created: number;
  skipped: number;
  failed: number;
  correlationId?: string;
  processedAt: string;
  items: ContractSyncItemResult[];
}

// ─── Contract Governance ─────────────────────────────────────────────────────

/** Item de contrato na listagem de governança. */
export interface ContractListItem {
  versionId: string;
  apiAssetId: string;
  semVer: string;
  protocol: string;
  lifecycleState: string;
  isLocked: boolean;
  format: string;
  importedFrom: string;
  createdAt: string;
  deprecationDate: string | null;
  isSigned: boolean;
}

/** Resposta paginada da listagem de contratos. */
export interface ContractListResponse {
  items: ContractListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Contagem por protocolo no resumo de contratos. */
export interface ContractProtocolCount {
  protocol: string;
  count: number;
}

/** Resposta do resumo agregado de contratos. */
export interface ContractsSummary {
  totalVersions: number;
  distinctContracts: number;
  draftCount: number;
  inReviewCount: number;
  approvedCount: number;
  lockedCount: number;
  deprecatedCount: number;
  byProtocol: ContractProtocolCount[];
}

/** Contrato associado a um serviço. */
export interface ServiceContractItem {
  versionId: string;
  apiAssetId: string;
  apiName: string;
  apiRoutePattern: string;
  semVer: string;
  protocol: string;
  lifecycleState: string;
  isLocked: boolean;
  createdAt: string;
}

/** Resposta da listagem de contratos de um serviço. */
export interface ServiceContractsResponse {
  serviceId: string;
  contracts: ServiceContractItem[];
  totalCount: number;
}

// ─── Change Intelligence ─────────────────────────────────────────────────────

export type ChangeLevel = 0 | 1 | 2 | 3 | 4;
export type DeploymentState = 'Pending' | 'Running' | 'Succeeded' | 'Failed' | 'RolledBack';

export interface Release {
  id: string;
  apiAssetId: string;
  apiAssetName: string;
  version: string;
  environment: string;
  changeLevel: ChangeLevel;
  deploymentState: DeploymentState;
  riskScore?: number;
  blastRadius?: BlastRadiusReport;
  createdAt: string;
  deployedAt?: string;
}

export interface BlastRadiusReport {
  releaseId: string;
  directConsumers: number;
  transitiveConsumers: number;
  affectedServices: string[];
  calculatedAt: string;
}

export interface ChangeScore {
  releaseId: string;
  score: number;
  factors: ScoreFactor[];
  computedAt: string;
}

export interface ScoreFactor {
  name: string;
  value: number;
  weight: number;
}

export interface PagedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Workflow ─────────────────────────────────────────────────────────────────

export interface WorkflowTemplate {
  id: string;
  name: string;
  changeLevel: ChangeLevel;
  stages: WorkflowStage[];
  createdAt: string;
}

export interface WorkflowStage {
  id: string;
  name: string;
  order: number;
  approvers: string[];
  requiredApprovals: number;
}

export interface WorkflowInstance {
  id: string;
  releaseId: string;
  templateId: string;
  status: 'Pending' | 'InProgress' | 'Approved' | 'Rejected';
  currentStage?: string;
  createdAt: string;
  completedAt?: string;
}

// ─── Audit ────────────────────────────────────────────────────────────────────

export interface AuditEvent {
  id: string;
  eventType: string;
  aggregateId: string;
  aggregateType: string;
  actorId: string;
  actorEmail: string;
  payload: Record<string, unknown>;
  hash: string;
  previousHash?: string;
  occurredAt: string;
}

// ─── Promotion ───────────────────────────────────────────────────────────────

export interface PromotionRequest {
  id: string;
  releaseId: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Promoted';
  gateResults: GateResult[];
  createdAt: string;
}

export interface GateResult {
  gateName: string;
  passed: boolean;
  message?: string;
}

// ─── Licensing ────────────────────────────────────────────────────────────────

export type LicenseType = 'Trial' | 'Standard' | 'Enterprise';
export type LicenseEdition = 'Community' | 'Professional' | 'Enterprise' | 'Unlimited';
export type EnforcementLevel = 'Soft' | 'Hard' | 'NeverBreak';
export type WarningLevel = 'Normal' | 'Advisory' | 'Warning' | 'Critical' | 'Exceeded';

/** Estado completo da licença — retornado pelo endpoint /licensing/status. */
export interface LicenseStatus {
  licenseId: string;
  licenseKey: string;
  customerName: string;
  type: LicenseType;
  edition: LicenseEdition;
  isActive: boolean;
  isExpired: boolean;
  isInGracePeriod: boolean;
  daysUntilExpiration: number;
  expiresAt: string;
  issuedAt: string;
  trialConverted: boolean;
  capabilities: CapabilityStatus[];
  quotas: UsageQuotaStatus[];
}

/** Estado de uma capability individual da licença. */
export interface CapabilityStatus {
  code: string;
  name: string;
  isEnabled: boolean;
}

/** Estado de uma quota de uso com nível de aviso e enforcement. */
export interface UsageQuotaStatus {
  metricCode: string;
  currentUsage: number;
  limit: number;
  thresholdReached: boolean;
  usagePercentage: number;
  warningLevel: WarningLevel;
  enforcementLevel: EnforcementLevel;
}

/** Resultado do health check da licença — score geral e alertas. */
export interface LicenseHealthResult {
  licenseId: string;
  healthScore: number;
  isExpired: boolean;
  isInGracePeriod: boolean;
  daysUntilExpiration: number;
  quotaWarnings: QuotaWarning[];
}

/** Aviso individual de quota próxima do limite. */
export interface QuotaWarning {
  metricCode: string;
  warningLevel: WarningLevel;
  usagePercentage: number;
  currentUsage: number;
  limit: number;
}

/** Alerta de threshold atingido — retornado pelo endpoint /licensing/thresholds. */
export interface LicenseThresholdAlert {
  metricCode: string;
  currentUsage: number;
  limit: number;
  thresholdPercentage: number;
  warningLevel: WarningLevel;
}

// ─── Developer Portal ─────────────────────────────────────────────────────────

export type SubscriptionLevel = 'BreakingChangesOnly' | 'AllChanges' | 'DeprecationNotices' | 'SecurityAdvisories';
export type NotificationChannel = 'Email' | 'Webhook';
export type GenerationType = 'SdkClient' | 'IntegrationExample' | 'ContractTest' | 'DataModels';

/** Item do catálogo de APIs do Developer Portal. */
export interface CatalogItem {
  apiAssetId: string;
  apiName: string;
  description: string;
  ownerServiceName: string;
  version: string;
  healthStatus: string;
}

/** Detalhe completo de uma API no catálogo. */
export interface ApiDetail {
  apiAssetId: string;
  apiName: string;
  description: string;
  ownerServiceName: string;
  version: string;
  baseUrl: string;
  healthStatus: string;
  consumerCount: number;
}

/** Informação de saúde de uma API. */
export interface ApiHealthInfo {
  apiAssetId: string;
  status: string;
  uptimePercentage: number;
  lastCheckedAt: string;
}

/** Entrada na timeline de eventos de uma API. */
export interface TimelineEntry {
  eventType: string;
  description: string;
  occurredAt: string;
  actorName: string;
}

/** Consumidor de uma API — subscriber ativo. */
export interface ApiConsumer {
  subscriberId: string;
  subscriberEmail: string;
  consumerServiceName: string;
  level: SubscriptionLevel;
  subscribedAt: string;
}

/** Subscription do utilizador a notificações de mudança de uma API. */
export interface Subscription {
  id: string;
  apiAssetId: string;
  apiName: string;
  level: SubscriptionLevel;
  channel: NotificationChannel;
  isActive: boolean;
  createdAt: string;
}

/** Resultado de uma execução no API Playground. */
export interface PlaygroundResult {
  sessionId: string;
  responseStatusCode: number;
  responseBody: string;
  durationMs: number;
  executedAt: string;
}

/** Item do histórico de execuções do playground. */
export interface PlaygroundHistoryItem {
  sessionId: string;
  apiName: string;
  httpMethod: string;
  requestPath: string;
  responseStatusCode: number;
  durationMs: number;
  executedAt: string;
}

/** Resultado da geração de código a partir de um contrato. */
export interface CodeGenerationResult {
  recordId: string;
  generatedCode: string;
  language: string;
  generationType: GenerationType;
  generatedAt: string;
}

/** Métricas de analytics do Developer Portal. */
export interface PortalAnalytics {
  totalSearches: number;
  totalApiViews: number;
  totalPlaygroundExecutions: number;
  totalCodeGenerations: number;
  topSearches: TopSearch[];
}

/** Pesquisa popular no catálogo. */
export interface TopSearch {
  query: string;
  count: number;
}

// ─── Dashboard ───────────────────────────────────────────────────────────────

export interface DashboardStats {
  totalReleases: number;
  pendingApprovals: number;
  activeServices: number;
  totalContracts: number;
  recentReleases: Release[];
}

// ─── Source of Truth ─────────────────────────────────────────────────────────

/** Indicadores de cobertura/completude de um serviço no Source of Truth. */
export interface CoverageIndicators {
  hasOwner: boolean;
  hasContracts: boolean;
  hasDocumentation: boolean;
  hasRunbook: boolean;
  hasRecentChangeHistory: boolean;
  hasDependenciesMapped: boolean;
  hasEventTopics: boolean;
}

/** Resumo de API associada a um serviço no Source of Truth. */
export interface SotApiSummary {
  apiAssetId: string;
  name: string;
  routePattern: string;
  version: string;
  visibility: string;
  consumerCount: number;
}

/** Resumo de contrato associado a um serviço no Source of Truth. */
export interface SotContractSummary {
  versionId: string;
  apiAssetId: string;
  semVer: string;
  protocol: string;
  lifecycleState: string;
  isLocked: boolean;
  createdAt: string;
}

/** Referência vinculada no Source of Truth. */
export interface SotReferenceSummary {
  referenceId: string;
  referenceType: string;
  title: string;
  description: string;
  url: string | null;
}

/** Visão consolidada de Source of Truth de um serviço. */
export interface ServiceSourceOfTruth {
  serviceId: string;
  name: string;
  displayName: string;
  description: string;
  serviceType: string;
  domain: string;
  systemArea: string;
  teamName: string;
  technicalOwner: string;
  businessOwner: string;
  criticality: string;
  lifecycleStatus: string;
  exposureType: string;
  documentationUrl: string | null;
  repositoryUrl: string | null;
  apis: SotApiSummary[];
  contracts: SotContractSummary[];
  references: SotReferenceSummary[];
  coverage: CoverageIndicators;
  totalApis: number;
  totalContracts: number;
  totalReferences: number;
}

/** Resumo de governança de contrato no Source of Truth. */
export interface SotGovernanceSummary {
  lifecycleState: string;
  isLocked: boolean;
  lockedAt: string | null;
  lockedBy: string | null;
  isSigned: boolean;
  deprecationNotice: string | null;
  deprecationDate: string | null;
  sunsetDate: string | null;
}

/** Visão consolidada de Source of Truth de um contrato. */
export interface ContractSourceOfTruth {
  contractVersionId: string;
  apiAssetId: string;
  semVer: string;
  protocol: string;
  format: string;
  importedFrom: string;
  governance: SotGovernanceSummary;
  references: SotReferenceSummary[];
  artifactCount: number;
  diffCount: number;
  violationCount: number;
  hasDocumentation: boolean;
  hasRelatedChanges: boolean;
}

/** Resposta de cobertura de um serviço. */
export interface ServiceCoverageResponse {
  serviceId: string;
  serviceName: string;
  hasOwner: boolean;
  hasContracts: boolean;
  hasDocumentation: boolean;
  hasRunbook: boolean;
  hasRecentChangeHistory: boolean;
  hasDependenciesMapped: boolean;
  hasEventTopics: boolean;
  coverageScore: number;
  totalIndicators: number;
  metIndicators: number;
}

/** Resultado de pesquisa de serviço no Source of Truth. */
export interface SotServiceSearchResult {
  serviceId: string;
  name: string;
  displayName: string;
  domain: string;
  teamName: string;
  criticality: string;
  lifecycleStatus: string;
}

/** Resultado de pesquisa de contrato no Source of Truth. */
export interface SotContractSearchResult {
  versionId: string;
  apiAssetId: string;
  semVer: string;
  protocol: string;
  lifecycleState: string;
}

/** Resultado de pesquisa de referência no Source of Truth. */
export interface SotReferenceSearchResult {
  referenceId: string;
  assetId: string;
  assetType: string;
  referenceType: string;
  title: string;
  description: string;
  url: string | null;
}

/** Resposta de pesquisa unificada do Source of Truth. */
export interface SourceOfTruthSearchResponse {
  services: SotServiceSearchResult[];
  contracts: SotContractSearchResult[];
  references: SotReferenceSearchResult[];
  totalResults: number;
}

// ─── Change Confidence ───────────────────────────────────────────────────────

/** DTO de mudança para o catálogo de Change Confidence. */
export interface ChangeDto {
  changeId: string;
  apiAssetId: string;
  serviceName: string;
  version: string;
  environment: string;
  changeType: string;
  deploymentStatus: string;
  changeLevel: string;
  confidenceStatus: string;
  validationStatus: string;
  changeScore: number;
  teamName: string | null;
  domain: string | null;
  description: string | null;
  workItemReference: string | null;
  commitSha: string;
  createdAt: string;
}

/** Resposta paginada do catálogo de mudanças. */
export interface ChangesListResponse {
  changes: ChangeDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Resposta de resumo agregado de mudanças. */
export interface ChangesSummaryResponse {
  totalChanges: number;
  validatedChanges: number;
  changesNeedingAttention: number;
  suspectedRegressions: number;
  changesCorrelatedWithIncidents: number;
}

// ─── Governance — Reports, Risk, Compliance, FinOps ─────────────────────────

/** Nível de risco operacional. */
export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

/** Dimensão de risco operacional avaliada. */
export type RiskDimensionType = 'Operational' | 'Change' | 'Contract' | 'Dependency' | 'Ownership' | 'Documentation' | 'IncidentRecurrence' | 'AiGovernance';

/** Estado de compliance técnico-operacional. */
export type ComplianceStatusType = 'Compliant' | 'PartiallyCompliant' | 'NonCompliant' | 'NotApplicable';

/** Nível de maturidade operacional. */
export type MaturityLevelType = 'Initial' | 'Developing' | 'Defined' | 'Managed' | 'Optimizing';

/** Classificação de eficiência de custo. */
export type CostEfficiencyType = 'Efficient' | 'Acceptable' | 'Inefficient' | 'Wasteful';

/** Direção de tendência. */
export type GovernanceTrendDirection = 'Improving' | 'Stable' | 'Declining';

/** Dimensão de risco individual com explicação. */
export interface RiskDimensionDto {
  dimension: RiskDimensionType;
  level: RiskLevel;
  explanation: string;
}

/** Indicador de risco por serviço. */
export interface RiskIndicatorDto {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  riskLevel: RiskLevel;
  dimensions: RiskDimensionDto[];
}

/** Resumo de risco operacional. */
export interface RiskSummaryResponse {
  overallRiskLevel: RiskLevel;
  totalServicesAssessed: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  indicators: RiskIndicatorDto[];
  generatedAt: string;
}

/** Indicadores de cobertura de compliance. */
export interface CoverageIndicatorsDto {
  ownerDefined: number;
  contractDefined: number;
  versioningPresent: number;
  documentationAvailable: number;
  runbookAvailable: number;
  dependenciesMapped: number;
  publicationUpToDate: number;
}

/** Gap de compliance identificado. */
export interface ComplianceGapDto {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  status: ComplianceStatusType;
  description: string;
}

/** Resumo de compliance. */
export interface ComplianceSummaryResponse {
  overallScore: number;
  totalServicesAssessed: number;
  compliantCount: number;
  partiallyCompliantCount: number;
  nonCompliantCount: number;
  coverage: CoverageIndicatorsDto;
  gaps: ComplianceGapDto[];
  generatedAt: string;
}

/** Sinal de desperdício operacional. */
export interface WasteSignalDto {
  description: string;
  pattern: string;
  estimatedWaste: number;
}

/** Custo contextual por serviço. */
export interface ServiceCostDto {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  efficiency: CostEfficiencyType;
  monthlyCost: number;
  trend: GovernanceTrendDirection;
  wasteSignals: WasteSignalDto[];
}

/** Resumo de FinOps contextual. */
export interface FinOpsSummaryResponse {
  totalMonthlyCost: number;
  totalWaste: number;
  overallEfficiency: CostEfficiencyType;
  costTrend: GovernanceTrendDirection;
  services: ServiceCostDto[];
  generatedAt: string;
}

/** Resumo executivo de relatórios. */
export interface ReportsSummaryResponse {
  totalServices: number;
  servicesWithOwner: number;
  servicesWithContract: number;
  servicesWithDocumentation: number;
  servicesWithRunbook: number;
  overallRiskLevel: RiskLevel;
  overallMaturity: MaturityLevelType;
  changeConfidenceTrend: GovernanceTrendDirection;
  reliabilityTrend: GovernanceTrendDirection;
  openIncidents: number;
  recentChanges: number;
  complianceScore: number;
  costEfficiency: CostEfficiencyType;
  generatedAt: string;
}
