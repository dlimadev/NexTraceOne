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

/**
 * Item de contrato na listagem de governança.
 * Inclui dados enriquecidos do ServiceAsset associado (domain, team, owner, criticality).
 */
export interface ContractListItem {
  versionId: string;
  apiAssetId: string;
  serviceAssetId: string | null;
  name: string;
  semVer: string;
  protocol: string;
  lifecycleState: string;
  isLocked: boolean;
  format: string;
  importedFrom: string;
  createdAt: string;
  updatedAt: string;
  deprecationDate: string | null;
  isSigned: boolean;
  domain: string;
  team: string;
  technicalOwner: string;
  criticality: 'Low' | 'Medium' | 'High' | 'Critical';
  exposure: 'Internal' | 'External' | 'Partner';
  serviceType: 'RestApi' | 'Soap' | 'KafkaProducer' | 'KafkaConsumer' | 'BackgroundService' | 'GraphQl';
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

// ─── Change Confidence — Advisory, Decision, Evidence Readiness ──────────────

/** Factor individual da recomendação de confiança. */
export interface AdvisoryFactorDto {
  factorName: string;
  status: 'Pass' | 'Warning' | 'Fail' | 'Unknown';
  description: string;
  weight: number | null;
}

/** Resposta da advisory/recomendação de confiança. */
export interface ChangeAdvisoryResponse {
  releaseId: string;
  recommendation: 'Approve' | 'Reject' | 'ApproveConditionally' | 'NeedsMoreEvidence';
  rationale: string;
  overallConfidence: number;
  factors: AdvisoryFactorDto[];
  generatedAt: string;
}

/** Comando para registar uma decisão de governança. */
export interface RecordDecisionRequest {
  decision: 'Approved' | 'Rejected' | 'ApprovedConditionally';
  decidedBy: string;
  rationale: string;
  conditions?: string;
}

/** Resposta do registo de decisão. */
export interface RecordDecisionResponse {
  decisionId: string;
  releaseId: string;
  decision: string;
  decidedBy: string;
  decidedAt: string;
}

/** Item do histórico de decisões. */
export interface DecisionHistoryItemDto {
  eventId: string;
  eventType: string;
  description: string;
  source: string;
  occurredAt: string;
}

/** Resposta do histórico de decisões. */
export interface DecisionHistoryResponse {
  releaseId: string;
  decisions: DecisionHistoryItemDto[];
}

// ─── Product Analytics ─────────────────────────────────────────────────────

export type ProductAnalyticsTrendDirection = 'Improving' | 'Stable' | 'Declining';

export interface AnalyticsModuleUsageDto {
  module: string;
  moduleName: string;
  eventCount: number;
  uniqueUsers: number;
  trend: ProductAnalyticsTrendDirection;
}

export interface ProductAnalyticsSummaryResponse {
  totalEvents: number;
  uniqueUsers: number;
  activePersonas: number;
  topModules: AnalyticsModuleUsageDto[];
  adoptionScore: number;
  valueScore: number;
  frictionScore: number;
  avgTimeToFirstValueMinutes: number;
  avgTimeToCoreValueMinutes: number;
  trendDirection: ProductAnalyticsTrendDirection;
  periodLabel: string;
}

export interface ModuleAdoptionDto {
  module: string;
  moduleName: string;
  adoptionPercent: number;
  totalActions: number;
  uniqueUsers: number;
  depthScore: number;
  trend: ProductAnalyticsTrendDirection;
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

// ─── Governance — Reports, Risk, Compliance, FinOps ─────────────────────────

/** Nível de risco operacional. */
export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

/** Dimensão de risco operacional avaliada. */
export type RiskDimensionType =
  | 'Operational'
  | 'Change'
  | 'Contract'
  | 'Dependency'
  | 'Ownership'
  | 'Documentation'
  | 'IncidentRecurrence'
  | 'AiGovernance'
  // Enterprise Governance (Stage 3C)
  | 'Waivers'
  | 'Rollouts'
  | 'Lifecycle';

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

/** Indicador de risco por Governance Pack. */
export interface RiskIndicatorDto {
  packId: string;
  packName: string;
  category: GovernancePackCategory;
  riskLevel: RiskLevel;
  dimensions: RiskDimensionDto[];
}

/** Resumo de risco enterprise (baseado em packs/rollouts/waivers). */
export interface RiskSummaryResponse {
  overallRiskLevel: RiskLevel;
  totalPacksAssessed: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  indicators: RiskIndicatorDto[];
  generatedAt: string;
}

/** Linha de compliance por Governance Pack. */
export interface CompliancePackRowDto {
  packId: string;
  packName: string;
  category: GovernancePackCategory;
  packStatus: GovernancePackStatus;
  status: ComplianceStatusType;
  pendingWaivers: number;
  approvedWaivers: number;
  rolloutCount: number;
  completedRollouts: number;
  failedRollouts: number;
}

/** Resumo de compliance enterprise (baseado em packs/rollouts/waivers). */
export interface ComplianceSummaryResponse {
  overallScore: number;
  totalPacksAssessed: number;
  compliantCount: number;
  partiallyCompliantCount: number;
  nonCompliantCount: number;
  totalRollouts: number;
  pendingRollouts: number;
  completedRollouts: number;
  failedRollouts: number;
  totalWaivers: number;
  pendingWaivers: number;
  approvedWaivers: number;
  packs: CompliancePackRowDto[];
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
/** Resumo executivo de relatórios baseado em dados reais de Governance Packs, Rollouts e Waivers. */
export interface ReportsSummaryResponse {
  totalPacks: number;
  publishedPacks: number;
  packsWithRollout: number;
  packsWithCompletedRollout: number;
  totalWaivers: number;
  pendingWaivers: number;
  approvedWaivers: number;
  totalRollouts: number;
  completedRollouts: number;
  failedRollouts: number;
  pendingRollouts: number;
  overallRiskLevel: RiskLevel;
  overallMaturity: MaturityLevelType;
  changeConfidenceTrend: GovernanceTrendDirection;
  reliabilityTrend: GovernanceTrendDirection;
  complianceScore: number;
  generatedAt: string;
}

// ─── Executive Governance — Overview, Heatmap, Scorecards, Benchmarking, Trends, Drill-down ──

/** Tendência operacional com estabilidade e resolução de incidentes. */
export interface OperationalTrendDto {
  stabilityTrend: GovernanceTrendDirection;
  incidentRateChange: number;
  avgResolutionHours: number;
}

/** Resumo de risco executivo. */
export interface ExecutiveRiskSummaryDto {
  overallRisk: RiskLevel;
  criticalDomains: number;
  highRiskServices: number;
  riskTrend: GovernanceTrendDirection;
}

/** Resumo de maturidade executiva. */
export interface ExecutiveMaturitySummaryDto {
  overallMaturity: MaturityLevelType;
  ownershipCoverage: number;
  contractCoverage: number;
  documentationCoverage: number;
  runbookCoverage: number;
}

/** Área de foco crítico que requer atenção executiva. */
export interface FocusAreaDto {
  areaName: string;
  severity: RiskLevel;
  description: string;
  affectedServices: number;
}

/** Resumo de segurança de mudanças. */
export interface ChangeSafetySummaryDto {
  safeChanges: number;
  riskyChanges: number;
  rollbacks: number;
  confidenceTrend: GovernanceTrendDirection;
}

/** Resumo de tendência de incidentes. */
export interface IncidentTrendSummaryDto {
  openIncidents: number;
  resolvedLast30Days: number;
  avgResolutionHours: number;
  recurrenceRate: number;
  trend: GovernanceTrendDirection;
}

/** Resumo de cobertura de compliance executivo. */
export interface ComplianceCoverageSummaryDto {
  overallScore: number;
  compliantPct: number;
  gapCount: number;
  trend: GovernanceTrendDirection;
}

/** Domínio que requer atenção executiva. */
export interface DomainAttentionDto {
  domainId: string;
  domainName: string;
  riskLevel: RiskLevel;
  reason: string;
}

/** Resposta da visão executiva expandida. */
export interface ExecutiveOverviewResponse {
  operationalTrend: OperationalTrendDto;
  riskSummary: ExecutiveRiskSummaryDto;
  maturitySummary: ExecutiveMaturitySummaryDto;
  criticalFocusAreas: FocusAreaDto[];
  changeSafetySummary: ChangeSafetySummaryDto;
  incidentTrendSummary: IncidentTrendSummaryDto;
  complianceCoverageSummary: ComplianceCoverageSummaryDto;
  topDomainsRequiringAttention: DomainAttentionDto[];
  generatedAt: string;
}

/** Célula do heatmap de risco com indicadores multidimensionais. */
export interface RiskHeatmapCellDto {
  groupId: string;
  groupName: string;
  riskLevel: RiskLevel;
  riskScore: number;
  incidents: number;
  changeFailures: number;
  reliabilityDegradation: boolean;
  contractGaps: number;
  documentationGaps: number;
  runbookGaps: number;
  dependencyFragility: number;
  regressionCount: number;
  explanation: string;
}

/** Resposta do heatmap de risco. */
export interface RiskHeatmapResponse {
  dimension: string;
  cells: RiskHeatmapCellDto[];
  generatedAt: string;
}

/** Score de dimensão de maturidade. */
export interface MaturityDimensionScoreDto {
  dimension: string;
  level: MaturityLevelType;
  score: number;
  maxScore: number;
  explanation: string;
}

/** Scorecard de maturidade por grupo. */
export interface MaturityScorecardDto {
  groupId: string;
  groupName: string;
  overallMaturity: MaturityLevelType;
  dimensions: MaturityDimensionScoreDto[];
}

/** Resposta dos scorecards de maturidade. */
export interface MaturityScorecardsResponse {
  dimension: string;
  scorecards: MaturityScorecardDto[];
  generatedAt: string;
}

/** Comparação de benchmarking por grupo. */
export interface BenchmarkComparisonDto {
  groupId: string;
  groupName: string;
  serviceCount: number;
  criticality: string;
  reliabilityScore: number;
  reliabilityTrend: GovernanceTrendDirection;
  changeSafetyScore: number;
  incidentRecurrenceRate: number;
  maturityScore: number;
  riskScore: number;
  finopsEfficiency: CostEfficiencyType;
  strengths: string[];
  gaps: string[];
  context: string;
}

/** Resposta do benchmarking. */
export interface BenchmarkingResponse {
  dimension: string;
  comparisons: BenchmarkComparisonDto[];
  generatedAt: string;
}

/** Ponto de dados de tendência. */
export interface TrendDataPointDto {
  period: string;
  value: number;
}

/** Série de tendência. */
export interface TrendSeriesDto {
  name: string;
  direction: GovernanceTrendDirection;
  dataPoints: TrendDataPointDto[];
}

/** Insight de tendência com recomendação. */
export interface TrendInsightDto {
  insight: string;
  impact: string;
  recommendation: string;
}

/** Resposta de tendências executivas. */
export interface ExecutiveTrendsResponse {
  category: string;
  series: TrendSeriesDto[];
  insights: TrendInsightDto[];
  generatedAt: string;
}

/** Indicador-chave do drill-down. */
export interface KeyIndicatorDto {
  name: string;
  value: string;
  trend: GovernanceTrendDirection;
  explanation: string;
}

/** Serviço crítico no drill-down. */
export interface CriticalServiceDto {
  serviceId: string;
  serviceName: string;
  riskLevel: RiskLevel;
  mainIssue: string;
}

/** Gap identificado no drill-down. */
export interface ExecutiveGapDto {
  area: string;
  severity: RiskLevel;
  description: string;
  recommendation: string;
}

/** Resposta do drill-down executivo. */
export interface ExecutiveDrillDownResponse {
  entityType: string;
  entityId: string;
  entityName: string;
  riskLevel: RiskLevel;
  maturityLevel: MaturityLevelType;
  keyIndicators: KeyIndicatorDto[];
  criticalServices: CriticalServiceDto[];
  topGaps: ExecutiveGapDto[];
  recommendedFocus: string[];
  generatedAt: string;
}

// ── Contract Studio Types ───────────────────────────────────────

export type ContractType = 'RestApi' | 'Soap' | 'Event' | 'BackgroundService' | 'SharedSchema';
export type DraftStatus = 'Editing' | 'InReview' | 'Approved' | 'Published' | 'Discarded';
export type ReviewDecision = 'Approved' | 'Rejected';

export interface ContractDraft {
  id: string;
  title: string;
  description: string;
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
  examples: ContractDraftExample[];
  createdAt: string;
}

export interface ContractDraftExample {
  id: string;
  name: string;
  description: string;
  content: string;
  contentFormat: string;
  exampleType: string;
  createdAt: string;
  createdBy: string;
}

export interface ContractReviewEntry {
  id: string;
  draftId: string;
  reviewedBy: string;
  decision: ReviewDecision;
  comment: string;
  reviewedAt: string;
}

export interface DraftListResponse {
  items: ContractDraft[];
  totalCount: number;
}

// ─── Governance — Advanced Compliance & Enterprise Controls ─────────────────

/** Categoria da política de governança. */
export type PolicyCategoryType = 'ServiceGovernance' | 'ContractGovernance' | 'ChangeGovernance' | 'OperationalReadiness' | 'AiGovernance' | 'SecurityCompliance' | 'DocumentationStandards';

/** Estado de uma política de governança. */
export type PolicyStatusType = 'Draft' | 'Active' | 'Inactive' | 'Deprecated';

/** Modo de aplicação de uma política. */
export type PolicyEnforcementModeType = 'Advisory' | 'SoftEnforce' | 'HardEnforce';

/** Severidade da política de governança. */
export type PolicySeverityType = 'Low' | 'Medium' | 'High' | 'Critical';

/** Resultado de um check de compliance. */
export type ComplianceCheckStatusType = 'Passed' | 'Failed' | 'Warning' | 'Skipped' | 'Error';

/** Dimensão de controlo enterprise. */
export type ControlDimensionType = 'ContractGovernance' | 'SourceOfTruthCompleteness' | 'ChangeGovernance' | 'IncidentMitigationEvidence' | 'AiGovernance' | 'DocumentationRunbookReadiness' | 'OwnershipCoverage';

/** Tipo de evidência. */
export type EvidenceTypeValue = 'Approval' | 'ChangeHistory' | 'ContractPublication' | 'MitigationRecord' | 'AiUsageRecord' | 'PolicyDecision' | 'ComplianceResult' | 'AuditReference';

/** Estado de um pacote de evidência. */
export type EvidencePackageStatusType = 'Draft' | 'Sealed' | 'Exported';

/** DTO de política de governança. */
export interface PolicyDto {
  policyId: string;
  name: string;
  displayName: string;
  description: string;
  category: PolicyCategoryType;
  scope: string;
  status: PolicyStatusType;
  severity: PolicySeverityType | null;
  enforcementMode: PolicyEnforcementModeType | null;
  effectiveEnvironments: string[];
  affectedAssetsCount: number;
  violationCount: number;
  createdAt: string;
}

/** Resposta da lista de políticas. */
export interface PolicyListResponse {
  totalPolicies: number;
  activeCount: number;
  draftCount: number;
  policies: PolicyDto[];
}

export interface PolicyRuleBindingDto {
  ruleId: string;
  ruleName: string;
  description?: string | null;
  category: GovernancePackCategory;
  defaultEnforcementMode: EnforcementMode;
  isRequired: boolean;
}

export interface PolicyRolloutDto {
  rolloutId: string;
  scopeType: GovernanceScopeType;
  scopeValue: string;
  enforcementMode: 'Advisory' | 'Required' | 'Blocking';
  status: 'Pending' | 'InProgress' | 'Completed' | 'Failed' | 'RolledBack';
  initiatedBy: string;
  initiatedAt: string;
  completedAt?: string | null;
}

export interface PolicyWaiverDto {
  waiverId: string;
  ruleId?: string | null;
  scopeType: GovernanceScopeType;
  scopeValue: string;
  status: WaiverStatus;
  requestedBy: string;
  requestedAt: string;
  reviewedBy?: string | null;
  reviewedAt?: string | null;
  expiresAt?: string | null;
  justification: string;
}

export interface PolicyDetailDto {
  policyId: string;
  name: string;
  displayName: string;
  description: string;
  category: PolicyCategoryType;
  scope: string;
  status: PolicyStatusType;
  severity: PolicySeverityType | null;
  enforcementMode: PolicyEnforcementModeType | null;
  effectiveEnvironments: string[];
  appliesTo: string[];
  affectedAssetsCount: number;
  waiverCount: number;
  currentVersion?: string | null;
  lastRolloutAt?: string | null;
  createdAt: string;
  updatedAt: string;
  rules: PolicyRuleBindingDto[];
  rollouts: PolicyRolloutDto[];
  waivers: PolicyWaiverDto[];
}

export interface PolicyDetailResponse {
  policy: PolicyDetailDto;
}

/** DTO de resultado de check de compliance. */
export interface ComplianceCheckResultDto {
  checkId: string;
  checkName: string;
  serviceId: string;
  serviceName: string;
  team: string;
  domain: string;
  status: ComplianceCheckStatusType;
  policyId: string;
  detail: string;
  evaluatedAt: string;
}

/** Resposta de compliance checks. */
export interface ComplianceChecksResponse {
  totalChecks: number;
  passedCount: number;
  failedCount: number;
  warningCount: number;
  results: ComplianceCheckResultDto[];
  executedAt: string;
}

/** DTO de gap de compliance avançado. */
export interface AdvancedComplianceGapDto {
  gapId: string;
  serviceId: string;
  serviceName: string;
  team: string;
  domain: string;
  description: string;
  severity: PolicySeverityType;
  violatedPolicyIds: string[];
  violationCount: number;
  detectedAt: string;
}

/** Resposta de gaps de compliance. */
export interface ComplianceGapsResponse {
  totalGaps: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  gaps: AdvancedComplianceGapDto[];
  generatedAt: string;
}

/** DTO de pacote de evidência. */
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

/** Resposta da lista de pacotes de evidência. */
export interface EvidencePackageListResponse {
  totalPackages: number;
  sealedCount: number;
  exportedCount: number;
  draftCount: number;
  packages: EvidencePackageDto[];
}

/** DTO de item de evidência. */
export interface EvidenceItemDto {
  itemId: string;
  type: EvidenceTypeValue;
  title: string;
  description: string;
  sourceModule: string;
  referenceId: string;
  recordedBy: string;
  recordedAt: string;
}

/** Detalhe de pacote de evidência. */
export interface EvidencePackageDetailResponse {
  packageId: string;
  name: string;
  description: string;
  scope: string;
  status: EvidencePackageStatusType;
  createdBy: string;
  createdAt: string;
  sealedAt: string | null;
  items: EvidenceItemDto[];
}

/** DTO de dimensão de controle enterprise. */
export interface ControlDimensionDto {
  dimension: ControlDimensionType;
  coveragePercent: number;
  totalAssessed: number;
  gapCount: number;
  maturity: MaturityLevelType;
  trend: GovernanceTrendDirection;
  summary: string;
}

/** Resposta de resumo de controles enterprise. */
export interface ControlsSummaryResponse {
  overallCoverage: number;
  overallMaturity: MaturityLevelType;
  totalDimensions: number;
  criticalGapCount: number;
  dimensions: ControlDimensionDto[];
  generatedAt: string;
}

// ─── FinOps Maturity — Service, Team, Domain, Waste, Efficiency, Trends ──

/** Tipo de sinal de desperdício operacional. */
export type WasteSignalType =
  | 'ExcessiveRetries'
  | 'RepeatedFailures'
  | 'IdleCostlyResource'
  | 'RepeatedReprocessing'
  | 'UnstableConsumer'
  | 'NoisyService'
  | 'DegradedCostAmplification'
  | 'QueueBacklogInefficiency'
  | 'OverProvisioned'
  | 'ChangeRelatedInstability';

/** Categoria de eficiência operacional. */
export type EfficiencyCategory =
  | 'ResourceUtilization'
  | 'RequestEfficiency'
  | 'ErrorRate'
  | 'ThroughputOptimization'
  | 'CostPerTransaction'
  | 'ScalingEfficiency';

/** Dimensão de custo contextual. */
export type CostDimension = 'Service' | 'Team' | 'Domain';

/** Correlação de confiabilidade com custo. */
export interface ReliabilityCorrelationDto {
  reliabilityScore: number;
  recentIncidents: number;
  reliabilityTrend: GovernanceTrendDirection;
}

/** Sinal de desperdício com tipo e timestamp. */
export interface WasteSignalDetailDto {
  signalId: string;
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  type: WasteSignalType;
  description: string;
  pattern: string;
  estimatedWaste: number;
  severity: string;
  detectedAt: string;
  correlatedCause: string | null;
}

/** Driver principal de custo. */
export interface CostDriverDto {
  serviceId: string;
  serviceName: string;
  monthlyCost: number;
  efficiency: CostEfficiencyType;
}

/** Oportunidade de otimização. */
export interface OptimizationOpportunityDto {
  serviceId: string;
  serviceName: string;
  potentialSavings: number;
  priority: string;
  recommendation: string;
}

/** Resumo de FinOps contextual enriquecido. */
export interface FinOpsSummaryEnrichedResponse {
  totalMonthlyCost: number;
  totalWaste: number;
  overallEfficiency: CostEfficiencyType;
  costTrend: GovernanceTrendDirection;
  services: ServiceCostEnrichedDto[];
  topCostDrivers: CostDriverDto[];
  topWasteSignals: WasteSignalTypedDto[];
  optimizationOpportunities: OptimizationOpportunityDto[];
  generatedAt: string;
}

/** Sinal de desperdício com tipo. */
export interface WasteSignalTypedDto {
  description: string;
  pattern: string;
  type: WasteSignalType;
  estimatedWaste: number;
}

/** Custo contextual por serviço enriquecido. */
export interface ServiceCostEnrichedDto {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  efficiency: CostEfficiencyType;
  monthlyCost: number;
  trend: GovernanceTrendDirection;
  wasteSignals: WasteSignalTypedDto[];
  reliabilityCorrelation: ReliabilityCorrelationDto | null;
}

/** Indicador de eficiência. */
export interface EfficiencyMetricDto {
  name: string;
  category: EfficiencyCategory;
  currentValue: number;
  targetValue: number;
  unit: string;
  assessment: string;
}

/** Impacto de mudança no custo. */
export interface ChangeImpactDto {
  changeId: string;
  description: string;
  appliedAt: string;
  costImpact: number;
  explanation: string;
}

/** Otimização sugerida. */
export interface OptimizationDto {
  recommendation: string;
  potentialSavings: number;
  priority: string;
  rationale: string;
}

/** Perfil de FinOps de um serviço. */
export interface ServiceFinOpsResponse {
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  monthlyCost: number;
  previousMonthCost: number;
  costTrend: GovernanceTrendDirection;
  efficiency: CostEfficiencyType;
  wasteSignals: WasteSignalDetailDto[];
  totalWaste: number;
  efficiencyIndicators: EfficiencyMetricDto[];
  reliabilityScore: number;
  recentIncidents: number;
  reliabilityTrend: GovernanceTrendDirection;
  changeImpacts: ChangeImpactDto[];
  optimizations: OptimizationDto[];
  totalPotentialSavings: number;
  generatedAt: string;
}

/** Custo de serviço dentro de equipa. */
export interface TeamServiceCostDto {
  serviceId: string;
  serviceName: string;
  efficiency: CostEfficiencyType;
  monthlyCost: number;
  trend: GovernanceTrendDirection;
  wasteAmount: number;
  reliabilityScore: number;
}

/** Ponto de série temporal de custo. */
export interface TrendPointDto {
  period: string;
  cost: number;
}

/** Perfil de FinOps de uma equipa. */
export interface TeamFinOpsResponse {
  teamId: string;
  teamName: string;
  domain: string;
  totalMonthlyCost: number;
  previousMonthCost: number;
  costTrend: GovernanceTrendDirection;
  overallEfficiency: CostEfficiencyType;
  totalWaste: number;
  serviceCount: number;
  services: TeamServiceCostDto[];
  trendSeries: TrendPointDto[];
  avgReliabilityScore: number;
  totalRecentIncidents: number;
  topOptimizationFocus: string;
  generatedAt: string;
}

/** Custo de equipa dentro de domínio. */
export interface DomainTeamCostDto {
  teamId: string;
  teamName: string;
  serviceCount: number;
  monthlyCost: number;
  wasteAmount: number;
  efficiency: CostEfficiencyType;
  avgReliabilityScore: number;
}

/** Serviço com maior desperdício no domínio. */
export interface WasteServiceDto {
  serviceId: string;
  serviceName: string;
  team: string;
  wasteAmount: number;
  efficiency: CostEfficiencyType;
}

/** Perfil de FinOps de um domínio. */
export interface DomainFinOpsResponse {
  domainId: string;
  domainName: string;
  totalMonthlyCost: number;
  previousMonthCost: number;
  costTrend: GovernanceTrendDirection;
  overallEfficiency: CostEfficiencyType;
  totalWaste: number;
  teamCount: number;
  serviceCount: number;
  teams: DomainTeamCostDto[];
  topWasteServices: WasteServiceDto[];
  trendSeries: TrendPointDto[];
  avgReliabilityScore: number;
  generatedAt: string;
}

/** Desperdício agregado por tipo. */
export interface WasteByTypeDto {
  type: WasteSignalType;
  count: number;
  totalWaste: number;
}

/** Resposta de sinais de desperdício. */
export interface WasteSignalsResponse {
  totalWaste: number;
  signalCount: number;
  signals: WasteSignalDetailDto[];
  byType: WasteByTypeDto[];
  generatedAt: string;
}

/** Eficiência de serviço. */
export interface ServiceEfficiencyDto {
  serviceId: string;
  serviceName: string;
  team: string;
  efficiency: CostEfficiencyType;
  metrics: EfficiencyMetricDto[];
}

/** Resposta de indicadores de eficiência. */
export interface EfficiencyIndicatorsResponse {
  overallEfficiencyScore: number;
  serviceCount: number;
  services: ServiceEfficiencyDto[];
  generatedAt: string;
}

/** Ponto de dados de tendência. */
export interface TrendDataPointDto {
  period: string;
  cost: number;
}

/** Série temporal de custo. */
export interface TrendSeriesDto {
  entityId: string;
  entityName: string;
  dataPoints: TrendDataPointDto[];
  direction: GovernanceTrendDirection;
  changePercent: number;
}

/** Resposta de tendências de custo. */
export interface FinOpsTrendsResponse {
  dimension: CostDimension;
  series: TrendSeriesDto[];
  aggregatedTrend: TrendDataPointDto[];
  overallDirection: GovernanceTrendDirection;
  overallChangePercent: number;
  generatedAt: string;
}

// ─── Multi-Team / Multi-Domain Governance ────────────────────────────────────

export interface TeamSummary {
  teamId: string;
  name: string;
  displayName: string;
  description?: string;
  status: string;
  serviceCount: number;
  contractCount: number;
  memberCount: number;
  maturityLevel: string;
  parentOrganizationUnit?: string;
}

export interface TeamDetail {
  teamId: string;
  name: string;
  displayName: string;
  description?: string;
  status: string;
  parentOrganizationUnit?: string;
  serviceCount: number;
  contractCount: number;
  activeIncidentCount: number;
  recentChangeCount: number;
  maturityLevel: string;
  reliabilityScore: number;
  services: TeamServiceDto[];
  contracts: TeamContractDto[];
  crossTeamDependencies: CrossTeamDependencyDto[];
  createdAt: string;
}

export interface TeamServiceDto {
  serviceId: string;
  name: string;
  domain: string;
  criticality: string;
  ownershipType: string;
}

export interface TeamContractDto {
  contractId: string;
  name: string;
  type: string;
  version: string;
  status: string;
}

export interface CrossTeamDependencyDto {
  dependencyId: string;
  sourceServiceName: string;
  targetServiceName: string;
  targetTeamId: string;
  targetTeamName: string;
  dependencyType: string;
}

export interface DomainSummary {
  domainId: string;
  name: string;
  displayName: string;
  description?: string;
  criticality: string;
  teamCount: number;
  serviceCount: number;
  contractCount: number;
  maturityLevel: string;
  capabilityClassification?: string;
}

export interface DomainDetail {
  domainId: string;
  name: string;
  displayName: string;
  description?: string;
  criticality: string;
  capabilityClassification?: string;
  teamCount: number;
  serviceCount: number;
  activeIncidentCount: number;
  recentChangeCount: number;
  maturityLevel: string;
  reliabilityScore: number;
  teams: DomainTeamDto[];
  services: DomainServiceDto[];
  crossDomainDependencies: CrossDomainDependencyDto[];
  createdAt: string;
}

export interface DomainTeamDto {
  teamId: string;
  name: string;
  displayName: string;
  serviceCount: number;
  ownershipType: string;
}

export interface DomainServiceDto {
  serviceId: string;
  name: string;
  teamName: string;
  criticality: string;
  status: string;
}

export interface CrossDomainDependencyDto {
  dependencyId: string;
  sourceServiceName: string;
  sourceDomainName: string;
  targetServiceName: string;
  targetDomainId: string;
  targetDomainName: string;
  dependencyType: string;
}

export interface GovernanceSummary {
  entityId: string;
  entityName: string;
  overallMaturity: string;
  ownershipCoverage: number;
  contractCoverage: number;
  documentationCoverage: number;
  reliabilityScore: number;
  openRiskCount: number;
  policyViolationCount: number;
  dimensions: GovernanceDimensionDto[];
}

export interface GovernanceDimensionDto {
  dimension: string;
  level: string;
  score: number;
  trend: string;
}

export interface ScopedContext {
  userId: string;
  defaultTeamId: string;
  defaultTeamName: string;
  defaultDomainId?: string;
  defaultDomainName?: string;
  allowedTeams: AllowedScopeDto[];
  allowedDomains: AllowedScopeDto[];
  adminScopes: string[];
  isCentralAdmin: boolean;
  personaHint: string;
}

export interface AllowedScopeDto {
  id: string;
  name: string;
  displayName: string;
  role: string;
}

export interface DelegatedAdminDto {
  delegationId: string;
  granteeUserId: string;
  granteeDisplayName: string;
  scope: string;
  teamId?: string;
  teamName?: string;
  domainId?: string;
  domainName?: string;
  reason: string;
  isActive: boolean;
  grantedAt: string;
  expiresAt?: string;
}

export interface CrossTeamDependencies {
  teamId: string;
  teamName: string;
  outbound: OutboundTeamDependency[];
  inbound: InboundTeamDependency[];
}

export interface OutboundTeamDependency {
  serviceName: string;
  targetServiceName: string;
  targetTeamId: string;
  targetTeamName: string;
  dependencyType: string;
}

export interface InboundTeamDependency {
  serviceName: string;
  sourceServiceName: string;
  sourceTeamId: string;
  sourceTeamName: string;
  dependencyType: string;
}

export interface CrossDomainDependencies {
  domainId: string;
  domainName: string;
  outbound: OutboundDomainDependency[];
  inbound: InboundDomainDependency[];
}

export interface OutboundDomainDependency {
  serviceName: string;
  sourceDomainName: string;
  targetServiceName: string;
  targetDomainId: string;
  targetDomainName: string;
  dependencyType: string;
}

export interface InboundDomainDependency {
  serviceName: string;
  targetDomainName: string;
  sourceServiceName: string;
  sourceDomainId: string;
  sourceDomainName: string;
  dependencyType: string;
}

export interface CreateTeamRequest {
  name: string;
  displayName: string;
  description?: string;
  parentOrganizationUnit?: string;
}

export interface CreateDomainRequest {
  name: string;
  displayName: string;
  description?: string;
  criticality: string;
  capabilityClassification?: string;
}

export interface CreateDelegationRequest {
  granteeUserId: string;
  granteeDisplayName: string;
  scope: string;
  teamId?: string;
  domainId?: string;
  reason: string;
  expiresAt?: string;
}

// ─── Governance Packs & Waivers ─────────────────────────────────────────────

/** Categoria de um Governance Pack. */
export type GovernancePackCategory = 'Contracts' | 'SourceOfTruth' | 'Changes' | 'Incidents' | 'AIGovernance' | 'Reliability' | 'Operations';

/** Estado de um Governance Pack. Alinhado com NexTraceOne.Governance.Domain.Enums.GovernancePackStatus. */
export type GovernancePackStatus = 'Draft' | 'Published' | 'Deprecated' | 'Archived';

/** Modo de enforcement de uma regra de governança. */
export type EnforcementMode = 'Advisory' | 'SoftEnforce' | 'HardEnforce';

/** Tipo de scope de aplicação de uma regra. */
export type GovernanceScopeType = 'Global' | 'Domain' | 'Team' | 'Service';

/** Resumo de um Governance Pack para listagem. */
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
  createdAt: string;
}

/** Resposta da listagem de Governance Packs. */
export interface GovernancePacksListResponse {
  totalPacks: number;
  publishedCount: number;
  draftCount: number;
  packs: GovernancePackSummary[];
}

/** Detalhes completos de um Governance Pack. */
export interface GovernancePackDetail {
  packId: string;
  name: string;
  displayName: string;
  description: string;
  category: GovernancePackCategory;
  status: GovernancePackStatus;
  currentVersion: string;
  scopeCount: number;
  ruleCount: number;
  createdAt: string;
  updatedAt: string;
  rules: GovernanceRuleBindingDto[];
  scopes: GovernanceScopeDto[];
  recentVersions: GovernancePackVersionDto[];
}

/** Regra vinculada a um Governance Pack. */
export interface GovernanceRuleBindingDto {
  ruleId: string;
  ruleName: string;
  description: string;
  category: GovernancePackCategory;
  defaultEnforcementMode: EnforcementMode;
  isRequired: boolean;
}

/** Escopo de aplicação de um Governance Pack. */
export interface GovernanceScopeDto {
  scopeType: GovernanceScopeType;
  scopeValue: string;
  enforcementMode: EnforcementMode;
}

/** Versão de um Governance Pack. */
export interface GovernancePackVersionDto {
  versionId: string;
  version: string;
  createdAt: string;
  createdBy: string;
  ruleCount: number;
}

/** Comando para criar um Governance Pack. */
export interface CreateGovernancePackRequest {
  name: string;
  displayName: string;
  description: string;
  category: GovernancePackCategory;
}

/** Comando para atualizar um Governance Pack. */
export interface UpdateGovernancePackRequest {
  displayName?: string;
  description?: string;
  status?: GovernancePackStatus;
}

/** Estado de um Waiver de governança. */
export type WaiverStatus = 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';

/** Tipo de scope de um Waiver. */
export type WaiverScopeType = 'Service' | 'Team' | 'Domain' | 'EntirePack';

/** DTO de um Waiver de governança para listagem. */
export interface GovernanceWaiverDto {
  waiverId: string;
  packId: string;
  packName: string;
  ruleId: string;
  ruleName: string;
  scope: string;
  scopeType: WaiverScopeType;
  justification: string;
  status: WaiverStatus;
  requestedBy: string;
  requestedAt: string;
  reviewedBy?: string;
  reviewedAt?: string;
  expiresAt?: string;
}

/** Resposta da listagem de Waivers. */
export interface GovernanceWaiversListResponse {
  totalWaivers: number;
  pendingCount: number;
  approvedCount: number;
  waivers: GovernanceWaiverDto[];
}

/** Comando para criar um Waiver. */
export interface CreateGovernanceWaiverRequest {
  packId: string;
  ruleId?: string;
  scope: string;
  scopeType: WaiverScopeType;
  justification: string;
  expiresAt?: string;
}

/** Comando para aprovar um Waiver. */
export interface ApproveWaiverRequest {
  expiresAt?: string;
}

/** Comando para rejeitar um Waiver. */
export interface RejectWaiverRequest {
  reason: string;
}
