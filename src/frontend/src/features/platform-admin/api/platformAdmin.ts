import client from '../../../api/client';

// ─── Preflight ───────────────────────────────────────────────────────────────

export type PreflightCheckStatus = 'Ok' | 'Warning' | 'Error';

export interface PreflightCheckResult {
  name: string;
  status: PreflightCheckStatus;
  message: string;
  suggestion: string | null;
  isRequired: boolean;
}

export interface PreflightReport {
  overallStatus: PreflightCheckStatus;
  checks: PreflightCheckResult[];
  isReadyToStart: boolean;
  checkedAt: string;
  version: string;
}

// ─── Config Health ────────────────────────────────────────────────────────────

export type ConfigCheckStatus = 'ok' | 'warning' | 'degraded';

export interface ConfigCheckDto {
  key: string;
  status: ConfigCheckStatus;
  message: string;
  suggestion: string | null;
}

export interface ConfigHealthResponse {
  status: ConfigCheckStatus;
  checks: ConfigCheckDto[];
  generatedAt: string;
}

// ─── Pending Migrations ───────────────────────────────────────────────────────

export type MigrationRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export interface MigrationDto {
  migrationId: string;
  context: string;
  riskLevel: MigrationRiskLevel;
  requiresDowntime: boolean;
  isReversible: boolean;
}

export interface PendingMigrationsResponse {
  totalPending: number;
  isSafeToApply: boolean;
  migrations: MigrationDto[];
  checkedAt: string;
}

// ─── Hardware Assessment ──────────────────────────────────────────────────────

export type ModelCompatibilityStatus = 'Compatible' | 'Incompatible';

export interface ModelAdvice {
  name: string;
  displayName: string;
  sizeGb: number;
  requiredRamGb: number;
  estTokPerSec: number;
  acceleratedByGpu: boolean;
  status: ModelCompatibilityStatus;
  warning: string | null;
  description: string;
}

export interface HardwareAssessmentReport {
  cpuModel: string;
  cpuCores: number;
  totalRamGb: number;
  availableRamGb: number;
  diskFreeGb: number;
  hasGpu: boolean;
  gpuModel: string | null;
  gpuVramGb: number;
  osDescription: string;
  models: ModelAdvice[];
  assessedAt: string;
}

// ─── Network Policy ───────────────────────────────────────────────────────────

export type NetworkIsolationMode = 'Off' | 'Restricted' | 'AirGap';

export interface ExternalCallStatus {
  key: string;
  description: string;
  envVar: string;
  configured: boolean;
  blocked: boolean;
}

export interface NetworkPolicyResponse {
  mode: NetworkIsolationMode;
  activeCalls: number;
  blockedCalls: number;
  calls: ExternalCallStatus[];
  auditedAt: string;
}

// ─── Database Health ──────────────────────────────────────────────────────────

export interface DbSchemaSize {
  schema: string;
  sizeGb: number;
  tableCount: number;
}

export interface DbBloatSignal {
  schema: string;
  table: string;
  bloatPct: number;
  severity: 'Low' | 'Medium' | 'High';
}

export interface DbSlowQuery {
  queryPreview: string;
  meanMs: number;
  calls: number;
}

export interface DatabaseHealthReport {
  available: boolean;
  error: string | null;
  version: string | null;
  uptimeMinutes: number;
  activeConnections: number;
  maxConnections: number;
  totalSizeGb: number;
  schemas: DbSchemaSize[];
  bloatSignals: DbBloatSignal[];
  slowQueryCount: number;
  slowQueries: DbSlowQuery[];
  checkedAt: string;
}

// ─── API client ───────────────────────────────────────────────────────────────

export const platformAdminApi = {
  /**
   * GET /preflight — acessível sem autenticação.
   * Retorna o resultado de todos os preflight checks do sistema.
   */
  getPreflight: () =>
    client.get<PreflightReport>('/preflight').then((r) => r.data),

  /**
   * GET /api/v1/platform/config-health — requer platform:admin:read.
   * Valida todas as configurações da plataforma com sugestões de resolução.
   */
  getConfigHealth: () =>
    client.get<ConfigHealthResponse>('/api/v1/platform/config-health').then((r) => r.data),

  /**
   * GET /api/v1/platform/migrations/pending — requer platform:admin:read.
   * Lista todas as migrations pendentes com classificação de risco.
   */
  getPendingMigrations: () =>
    client
      .get<PendingMigrationsResponse>('/api/v1/platform/migrations/pending')
      .then((r) => r.data),

  /**
   * GET /api/v1/admin/ai/hardware-assessment — requer platform:admin:read.
   * Avalia o hardware do servidor e recomenda modelos LLM compatíveis.
   */
  getHardwareAssessment: () =>
    client
      .get<HardwareAssessmentReport>('/api/v1/admin/ai/hardware-assessment')
      .then((r) => r.data),

  /**
   * GET /api/v1/platform/network-policy — requer platform:admin:read.
   * Retorna a política de rede activa e o estado de cada chamada externa.
   */
  getNetworkPolicy: () =>
    client
      .get<NetworkPolicyResponse>('/api/v1/platform/network-policy')
      .then((r) => r.data),

  /**
   * GET /api/v1/platform/database-health — requer platform:admin:read.
   * Retorna métricas de saúde do PostgreSQL.
   */
  getDatabaseHealth: () =>
    client
      .get<DatabaseHealthReport>('/api/v1/platform/database-health')
      .then((r) => r.data),
};

