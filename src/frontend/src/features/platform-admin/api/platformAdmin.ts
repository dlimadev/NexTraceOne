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

// ─── W2-04: Support Bundle ────────────────────────────────────────────────────

export interface SupportBundleEntry {
  id: string;
  generatedAt: string;
  generatedBy: string;
  fileSizeKb: number;
  includedFiles: string[];
}

export interface SupportBundleListResponse {
  bundles: SupportBundleEntry[];
}

// ─── W3-03: Backup Coordinator ───────────────────────────────────────────────

export type BackupStatus = 'Success' | 'Failed' | 'InProgress' | 'Scheduled';

export interface BackupRecord {
  id: string;
  startedAt: string;
  completedAt: string | null;
  status: BackupStatus;
  fileSizeGb: number | null;
  durationSeconds: number | null;
  destination: string;
  checksumSha256: string | null;
  errorMessage: string | null;
}

export interface BackupScheduleConfig {
  enabled: boolean;
  cronExpression: string;
  retentionDays: number;
  destination: string;
  compressionEnabled: boolean;
}

export interface BackupCoordinatorResponse {
  schedule: BackupScheduleConfig;
  recentBackups: BackupRecord[];
  lastSuccessfulBackup: BackupRecord | null;
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

  // ── W2-04: Support Bundle ──────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/support-bundles — requer platform:admin:read.
   * Lista os support bundles gerados anteriormente.
   */
  getSupportBundles: () =>
    client
      .get<SupportBundleListResponse>('/api/v1/admin/support-bundles')
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/support-bundles — requer platform:admin:read.
   * Gera um novo support bundle e retorna o id para download.
   */
  generateSupportBundle: () =>
    client
      .post<SupportBundleEntry>('/api/v1/admin/support-bundles')
      .then((r) => r.data),

  /**
   * GET /api/v1/admin/support-bundles/:id/download — requer platform:admin:read.
   * Retorna a URL de download do support bundle especificado.
   */
  getSupportBundleDownloadUrl: (id: string) =>
    `/api/v1/admin/support-bundles/${id}/download`,

  // ── W3-03: Backup Coordinator ─────────────────────────────────────────────

  /**
   * GET /api/v1/admin/backup — requer platform:admin:read.
   * Retorna configuração de backup e histórico de execuções.
   */
  getBackupStatus: () =>
    client
      .get<BackupCoordinatorResponse>('/api/v1/admin/backup')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/backup/schedule — requer platform:admin:write.
   * Actualiza a configuração de agendamento de backup.
   */
  updateBackupSchedule: (config: BackupScheduleConfig) =>
    client
      .put<BackupScheduleConfig>('/api/v1/admin/backup/schedule', config)
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/backup/run — requer platform:admin:write.
   * Inicia um backup manual imediato.
   */
  runBackupNow: () =>
    client.post<BackupRecord>('/api/v1/admin/backup/run').then((r) => r.data),
};

