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
