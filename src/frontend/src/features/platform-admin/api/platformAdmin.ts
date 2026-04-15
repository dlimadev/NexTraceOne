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
};
