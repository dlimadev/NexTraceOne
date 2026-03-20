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
