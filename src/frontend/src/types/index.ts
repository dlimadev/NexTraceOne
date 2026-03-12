// ─── Auth & Identity ─────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
  tenantId: string;
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

// ─── Engineering Graph ───────────────────────────────────────────────────────

export interface ApiAsset {
  id: string;
  name: string;
  baseUrl: string;
  description?: string;
  ownerServiceId: string;
  createdAt: string;
}

export interface ServiceAsset {
  id: string;
  name: string;
  team: string;
  description?: string;
  createdAt: string;
}

export interface ConsumerRelationship {
  apiAssetId: string;
  consumerServiceId: string;
  trustLevel: 'Inferred' | 'Low' | 'Medium' | 'High' | 'Confirmed';
}

export interface AssetGraph {
  services: ServiceAsset[];
  apis: ApiAsset[];
  relationships: ConsumerRelationship[];
}

// ─── Contracts ───────────────────────────────────────────────────────────────

export interface ContractVersion {
  id: string;
  apiAssetId: string;
  version: string;
  content: string;
  isLocked: boolean;
  createdAt: string;
}

export interface SemanticDiff {
  fromVersion: string;
  toVersion: string;
  changes: ChangeEntry[];
  isBreaking: boolean;
  suggestedVersion: string;
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

// ─── Dashboard ───────────────────────────────────────────────────────────────

export interface DashboardStats {
  totalReleases: number;
  pendingApprovals: number;
  activeServices: number;
  totalContracts: number;
  recentReleases: Release[];
}
