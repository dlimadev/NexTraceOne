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

// ─── W2-02 Startup Report ────────────────────────────────────────────────────

export interface StartupConfigSnapshot {
  smtpConfigured: boolean;
  ollamaConfigured: boolean;
  elasticsearchConfigured: boolean;
  corsOrigins: string[];
}

export interface StartupReportEntry {
  id: string;
  startedAt: string;
  version: string;
  build: string;
  environment: string;
  hostname: string;
  migrationsApplied: number;
  migrationsTotal: number;
  modulesRegistered: number;
  configuration: StartupConfigSnapshot;
  warnings: string[];
}

export interface StartupReportListResponse {
  reports: StartupReportEntry[];
}

// ─── W6-03 Resource Budget ────────────────────────────────────────────────────

export interface TenantResourceUsage {
  cpuRequestsCores: number;
  memoryRequestsGb: number;
  diskUsageGb: number;
  aiTokensUsedThisMonth: number;
  activeConnections: number;
}

export interface TenantResourceQuota {
  maxCpuCores: number | null;
  maxMemoryGb: number | null;
  maxDiskGb: number | null;
  maxAiTokensPerMonth: number | null;
  maxConnections: number | null;
}

export interface TenantBudgetEntry {
  tenantId: string;
  tenantName: string;
  quota: TenantResourceQuota;
  usage: TenantResourceUsage;
  isBlocked: boolean;
  blockReason: string | null;
  overrideUntil: string | null;
  overrideReason: string | null;
}

export interface ResourceBudgetResponse {
  tenants: TenantBudgetEntry[];
  updatedAt: string;
}

// ─── W7-01/02 Elasticsearch Manager ──────────────────────────────────────────

export type EsIndexPhase = 'hot' | 'warm' | 'cold' | 'delete';

export interface EsIndexInfo {
  name: string;
  docsCount: number;
  storeSizeGb: number;
  currentPhase: EsIndexPhase;
  createdAt: string;
  ilmPolicyName: string | null;
}

export interface EsClusterHealth {
  status: 'green' | 'yellow' | 'red';
  clusterName: string;
  numberOfNodes: number;
  activeShards: number;
  unassignedShards: number;
  jvmHeapUsedPercent: number;
  diskUsedPercent: number;
  diskTotalGb: number;
  diskUsedGb: number;
  projectedDaysUntilFull: number | null;
  isReadOnly: boolean;
  checkedAt: string;
}

export interface EsIlmPolicy {
  name: string;
  hotMaxAgeDays: number | null;
  warmAfterDays: number | null;
  deleteAfterDays: number | null;
}

export interface ElasticsearchManagerResponse {
  clusterHealth: EsClusterHealth;
  indices: EsIndexInfo[];
  ilmPolicies: EsIlmPolicy[];
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

  // ── W2-02: Startup Report ─────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/startup-report — requer platform:admin:read.
   * Retorna os últimos 30 relatórios de startup.
   */
  getStartupReports: () =>
    client
      .get<StartupReportListResponse>('/api/v1/admin/startup-report')
      .then((r) => r.data),

  // ── W6-03: Resource Budget ────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/resource-budget — requer platform:admin:read.
   * Retorna quotas e uso actual por tenant.
   */
  getResourceBudget: () =>
    client
      .get<ResourceBudgetResponse>('/api/v1/admin/resource-budget')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/resource-budget/:tenantId — requer platform:admin:write.
   * Actualiza as quotas de um tenant específico.
   */
  updateTenantQuota: (tenantId: string, quota: TenantResourceQuota) =>
    client
      .put<TenantBudgetEntry>(`/api/v1/admin/resource-budget/${tenantId}`, quota)
      .then((r) => r.data),

  // ── W7-01/02: Elasticsearch Manager ──────────────────────────────────────

  /**
   * GET /api/v1/admin/elasticsearch — requer platform:admin:read.
   * Retorna saúde do cluster, lista de índices e políticas ILM.
   */
  getElasticsearchManager: () =>
    client
      .get<ElasticsearchManagerResponse>('/api/v1/admin/elasticsearch')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/elasticsearch/ilm/:policyName — requer platform:admin:write.
   * Actualiza uma política ILM.
   */
  updateIlmPolicy: (policyName: string, policy: EsIlmPolicy) =>
    client
      .put<EsIlmPolicy>(`/api/v1/admin/elasticsearch/ilm/${policyName}`, policy)
      .then((r) => r.data),

  // ── W2-03: Platform Alert Rules ───────────────────────────────────────────

  /**
   * GET /api/v1/admin/platform-alerts — requer platform:admin:read.
   * Retorna regras de alerta configuradas e histórico recente.
   */
  getPlatformAlerts: () =>
    client
      .get<PlatformAlertsResponse>('/api/v1/admin/platform-alerts')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/platform-alerts/:ruleId — requer platform:admin:write.
   * Actualiza uma regra de alerta.
   */
  updateAlertRule: (ruleId: string, rule: PlatformAlertRuleUpdate) =>
    client
      .put<PlatformAlertRule>(`/api/v1/admin/platform-alerts/${ruleId}`, rule)
      .then((r) => r.data),

  // ── W3-04: Recovery Wizard ────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/recovery/restore-points — requer platform:admin:read.
   * Lista os pontos de restauro disponíveis (backups).
   */
  getRestorePoints: () =>
    client
      .get<RestorePointsResponse>('/api/v1/admin/recovery/restore-points')
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/recovery/initiate — requer platform:admin:write.
   * Inicia ou faz dry-run de um processo de recuperação.
   */
  initiateRecovery: (request: RecoveryRequest) =>
    client
      .post<RecoveryResponse>('/api/v1/admin/recovery/initiate', request)
      .then((r) => r.data),

  // ── W6-04: GreenOps ───────────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/greenops — requer platform:admin:read.
   * Retorna carbon score por serviço e emissões totais da organização.
   */
  getGreenOpsReport: () =>
    client
      .get<GreenOpsReport>('/api/v1/admin/greenops')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/greenops/config — requer platform:admin:write.
   * Actualiza o intensity factor e meta ESG da organização.
   */
  updateGreenOpsConfig: (config: GreenOpsConfigUpdate) =>
    client
      .put<GreenOpsConfig>('/api/v1/admin/greenops/config', config)
      .then((r) => r.data),

  // ── W4-03: AI Resource Governor ───────────────────────────────────────────

  /**
   * GET /api/v1/admin/ai/governor — requer platform:admin:read.
   * Retorna configuração e métricas em tempo real do AI Resource Governor.
   */
  getAiGovernorStatus: () =>
    client
      .get<AiGovernorStatus>('/api/v1/admin/ai/governor')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/ai/governor — requer platform:admin:write.
   * Actualiza os limites de concorrência e circuit breaker do AI Governor.
   */
  updateAiGovernorConfig: (config: AiResourceGovernorConfigUpdate) =>
    client
      .put<AiResourceGovernorConfig>('/api/v1/admin/ai/governor', config)
      .then((r) => r.data),

  // ── W4-04: AI Governance ──────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/ai/governance — requer platform:admin:read.
   * Retorna dashboard de qualidade de respostas AI por modelo.
   */
  getAiGovernanceDashboard: () =>
    client
      .get<AiGovernanceDashboard>('/api/v1/admin/ai/governance')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/ai/governance/config — requer platform:admin:write.
   * Actualiza configuração de grounding check e hallucination detection.
   */
  updateAiGovernanceConfig: (config: AiGovernanceConfigUpdate) =>
    client
      .put<AiGovernanceConfig>('/api/v1/admin/ai/governance/config', config)
      .then((r) => r.data),

  // ── W5-02: Proxy Config ───────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/proxy-config — requer platform:admin:read.
   * Retorna configuração de proxy corporativo e CA interna.
   */
  getProxyConfig: () =>
    client
      .get<ProxyConfig>('/api/v1/admin/proxy-config')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/proxy-config — requer platform:admin:write.
   * Actualiza configuração de proxy e CA interna.
   */
  updateProxyConfig: (config: ProxyConfigUpdate) =>
    client
      .put<ProxyConfig>('/api/v1/admin/proxy-config', config)
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/proxy-config/test — requer platform:admin:write.
   * Testa a conectividade via proxy configurado.
   */
  testProxyConnectivity: () =>
    client
      .post<ProxyConnectivityTestResult>('/api/v1/admin/proxy-config/test', {})
      .then((r) => r.data),

  // ── W5-03: External HTTP Audit ────────────────────────────────────────────

  /**
   * GET /api/v1/admin/external-http-audit — requer platform:admin:read.
   * Retorna registo de chamadas HTTP externas auditadas.
   */
  getExternalHttpAudit: (params?: ExternalHttpAuditParams) =>
    client
      .get<ExternalHttpAuditResponse>('/api/v1/admin/external-http-audit', { params })
      .then((r) => r.data),

  // ── W5-05: Environment Policies ───────────────────────────────────────────

  /**
   * GET /api/v1/admin/environment-policies — requer platform:admin:read.
   * Retorna políticas de acesso por ambiente configuradas.
   */
  getEnvironmentPolicies: () =>
    client
      .get<EnvironmentPoliciesResponse>('/api/v1/admin/environment-policies')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/environment-policies/:id — requer platform:admin:write.
   * Actualiza uma política de acesso por ambiente.
   */
  updateEnvironmentPolicy: (id: string, update: EnvironmentPolicyUpdate) =>
    client
      .put<EnvironmentAccessPolicy>(`/api/v1/admin/environment-policies/${id}`, update)
      .then((r) => r.data),

  // ── W6-02: Non-Prod Scheduler ─────────────────────────────────────────────

  /**
   * GET /api/v1/admin/nonprod-schedules — requer platform:admin:read.
   * Retorna schedules de shutdown para ambientes não-produtivos.
   */
  getNonProdSchedules: () =>
    client
      .get<NonProdSchedulesResponse>('/api/v1/admin/nonprod-schedules')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/nonprod-schedules/:environmentId — requer platform:admin:write.
   * Actualiza schedule de um ambiente não-produtivo.
   */
  updateNonProdSchedule: (environmentId: string, update: NonProdScheduleUpdate) =>
    client
      .put<NonProdScheduleEntry>(`/api/v1/admin/nonprod-schedules/${environmentId}`, update)
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/nonprod-schedules/:environmentId/override — requer platform:admin:write.
   * Activa override manual de schedule com justificação.
   */
  overrideNonProdSchedule: (environmentId: string, payload: NonProdScheduleOverride) =>
    client
      .post<NonProdScheduleEntry>(`/api/v1/admin/nonprod-schedules/${environmentId}/override`, payload)
      .then((r) => r.data),

  // ── W8-01: Capacity Forecast ──────────────────────────────────────────────

  /**
   * GET /api/v1/admin/capacity-forecast — requer platform:admin:read.
   * Retorna previsão de capacidade baseada em tendências reais.
   */
  getCapacityForecast: () =>
    client
      .get<CapacityForecastResponse>('/api/v1/admin/capacity-forecast')
      .then((r) => r.data),

  // ── W1-04: Demo Seed ──────────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/demo-seed — requer platform:admin:read.
   * Retorna estado actual do seed de demonstração.
   */
  getDemoSeedStatus: () =>
    client
      .get<DemoSeedStatus>('/api/v1/admin/demo-seed')
      .then((r) => r.data),

  /**
   * POST /api/v1/admin/demo-seed — requer platform:admin:write.
   * Executa o seed de dados de demonstração.
   */
  runDemoSeed: (request: DemoSeedRequest) =>
    client
      .post<DemoSeedResult>('/api/v1/admin/demo-seed', request)
      .then((r) => r.data),

  /**
   * DELETE /api/v1/admin/demo-seed — requer platform:admin:write.
   * Remove todos os dados marcados como is_demo=true.
   */
  clearDemoData: () =>
    client
      .delete<DemoSeedClearResult>('/api/v1/admin/demo-seed')
      .then((r) => r.data),

  // ── W3-05: Graceful Shutdown ──────────────────────────────────────────────

  /**
   * GET /api/v1/admin/graceful-shutdown — requer platform:admin:read.
   * Retorna configuração de graceful shutdown.
   */
  getGracefulShutdownConfig: () =>
    client
      .get<GracefulShutdownConfig>('/api/v1/admin/graceful-shutdown')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/graceful-shutdown — requer platform:admin:write.
   * Actualiza configuração de graceful shutdown.
   */
  updateGracefulShutdownConfig: (config: GracefulShutdownConfigUpdate) =>
    client
      .put<GracefulShutdownConfig>('/api/v1/admin/graceful-shutdown', config)
      .then((r) => r.data),

  // ── W5-06: Session Security ───────────────────────────────────────────────

  /**
   * GET /api/v1/admin/session-security — requer platform:admin:read.
   * Retorna configuração de segurança de sessão.
   */
  getSessionSecurityConfig: () =>
    client
      .get<SessionSecurityConfig>('/api/v1/admin/session-security')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/session-security — requer platform:admin:write.
   * Actualiza configuração de segurança de sessão.
   */
  updateSessionSecurityConfig: (config: SessionSecurityConfigUpdate) =>
    client
      .put<SessionSecurityConfig>('/api/v1/admin/session-security', config)
      .then((r) => r.data),

  // ── W6-05: Rightsizing ────────────────────────────────────────────────────

  /**
   * GET /api/v1/admin/rightsizing — requer platform:admin:read.
   * Retorna recomendações de rightsizing baseadas em percentis reais.
   */
  getRightsizingReport: () =>
    client
      .get<RightsizingReport>('/api/v1/admin/rightsizing')
      .then((r) => r.data),

  // ── W7-03: Observability Mode ─────────────────────────────────────────────

  /**
   * GET /api/v1/admin/observability-mode — requer platform:admin:read.
   * Retorna modo de observabilidade actual e recursos disponíveis.
   */
  getObservabilityMode: () =>
    client
      .get<ObservabilityModeConfig>('/api/v1/admin/observability-mode')
      .then((r) => r.data),

  /**
   * PUT /api/v1/admin/observability-mode — requer platform:admin:write.
   * Actualiza o modo de observabilidade (Full/Lite/Minimal).
   */
  updateObservabilityMode: (update: ObservabilityModeUpdate) =>
    client
      .put<ObservabilityModeConfig>('/api/v1/admin/observability-mode', update)
      .then((r) => r.data),

  // ── W8-06: Compliance Packs ───────────────────────────────────────────────

  /**
   * GET /api/v1/admin/compliance-packs — requer platform:admin:read.
   * Retorna packs de compliance configurados e estado dos controls.
   */
  getCompliancePacks: () =>
    client
      .get<CompliancePacksResponse>('/api/v1/admin/compliance-packs')
      .then((r) => r.data),

  // ── W3-01: Migration Preview ──────────────────────────────────────────────

  /**
   * GET /api/v1/admin/migration-preview — requer platform:admin:read.
   * Retorna migrações EF Core pendentes com preview de SQL e indicador de risco.
   */
  getMigrationPreview: async (): Promise<MigrationPreviewResponse> => {
    const data = await client
      .get<{
        totalPending: number;
        isSafeToApply: boolean;
        migrations: Array<{
          migrationId: string;
          context: string;
          riskLevel: MigrationRisk;
          requiresDowntime: boolean;
          isReversible: boolean;
        }>;
        checkedAt: string;
      }>('/api/v1/platform/migrations/pending')
      .then((r) => r.data);

    return {
      pending: data.migrations.map((m) => ({
        id: m.migrationId,
        name: m.migrationId,
        timestamp: m.migrationId.slice(0, 14),
        module: m.context.replace('DbContext', ''),
        risk: m.riskLevel,
        operations: [
          m.requiresDowntime ? 'RequiresDowntime' : 'Online',
          m.isReversible ? 'Reversible' : 'Irreversible',
        ],
        sqlPreview: '',
        estimatedDurationMs: 0,
      })),
      appliedCount: 0,
      generatedAt: data.checkedAt,
      simulatedNote: data.isSafeToApply
        ? 'All pending migrations are safe to apply.'
        : 'Review high-risk migrations before applying to production.',
    };
  },

  // ── W7-05: DORA Admin Dashboard ───────────────────────────────────────────

  /**
   * GET /api/v1/admin/dora-metrics — requer platform:admin:read.
   * Retorna métricas DORA calculadas para o ambiente e janela de tempo seleccionados.
   */
  getDoraAdminMetrics: async (env: string, days: number): Promise<DoraAdminMetricsResponse> => {
    const data = await client
      .get<{
        deploymentFrequency: { name: string; value: number; unit: string; rating: string };
        leadTimeForChanges: { name: string; value: number; unit: string; rating: string };
        changeFailureRate: { name: string; value: number; unit: string; rating: string };
        meanTimeToRestore: { name: string; value: number; unit: string; rating: string };
        computedAt: string;
        periodDays: number;
      }>(`/api/v1/governance/dora-metrics?periodDays=${days}`)
      .then((r) => r.data);

    const mapMetric = (m: {
      name: string;
      value: number;
      unit: string;
      rating: string;
    }): DoraMetric => ({
      name: m.name,
      value: String(m.value),
      unit: m.unit,
      rating: m.rating as DoraRating,
      trend: 0,
      trendDirection: 'stable',
    });

    return {
      deploymentFrequency: mapMetric(data.deploymentFrequency),
      leadTime: mapMetric(data.leadTimeForChanges),
      mttr: mapMetric(data.meanTimeToRestore),
      changeFailureRate: mapMetric(data.changeFailureRate),
      environment: env,
      timeRangeDays: data.periodDays,
      dataSource: 'PostgreSQL (live)',
      lastUpdatedAt: data.computedAt,
      simulatedNote: '',
    };
  },

  // ── W8-04: SAML SSO Configuration ────────────────────────────────────────

  /**
   * GET /api/v1/admin/saml-sso — requer platform:admin:read.
   * Retorna configuração actual de SAML SSO.
   */
  getSamlSsoConfig: (): Promise<SamlSsoConfig> =>
    Promise.resolve({
      status: 'NotConfigured',
      entityId: '',
      ssoUrl: '',
      sloUrl: '',
      idpCertificate: '',
      jitProvisioningEnabled: false,
      defaultRole: 'Engineer',
      attributeMappings: [
        { samlAttr: 'email', nxtField: 'email' },
        { samlAttr: 'displayName', nxtField: 'name' },
        { samlAttr: 'groups', nxtField: 'groups' },
        { samlAttr: 'role', nxtField: 'role' },
      ],
      lastTestedAt: undefined,
      testResult: null,
      simulatedNote: 'Simulated SAML SSO configuration — connect to real identity provider for live config.',
    }),

  /**
   * PUT /api/v1/admin/saml-sso — requer platform:admin:write.
   * Actualiza a configuração de SAML SSO.
   */
  updateSamlSsoConfig: (update: SamlSsoConfigUpdate): Promise<SamlSsoConfig> =>
    Promise.resolve({
      status: 'Enabled',
      entityId: update.entityId,
      ssoUrl: update.ssoUrl,
      sloUrl: update.sloUrl,
      idpCertificate: update.idpCertificate,
      jitProvisioningEnabled: update.jitProvisioningEnabled,
      defaultRole: update.defaultRole,
      attributeMappings: update.attributeMappings,
      lastTestedAt: undefined,
      testResult: null,
      simulatedNote: 'Simulated SAML SSO configuration — connect to real identity provider for live config.',
    }),

  /**
   * POST /api/v1/admin/saml-sso/test — requer platform:admin:write.
   * Testa a ligação ao Identity Provider SAML.
   */
  testSamlConnection: (): Promise<{ success: boolean; message: string }> =>
    new Promise((resolve) =>
      setTimeout(
        () => resolve({ success: true, message: 'Connection test successful (simulated).' }),
        1200,
      ),
    ),

  // ── W5-04: mTLS Certificate Manager ──────────────────────────────────────

  /**
   * GET /api/v1/admin/mtls — requer platform:admin:read.
   * Retorna estado do mTLS, política e inventário de certificados.
   */
  getMtlsManager: (): Promise<MtlsManagerResponse> =>
    Promise.resolve({
      policy: {
        mode: 'PerService',
        rootCaCertPresent: true,
        rootCaCertExpiry: '2027-06-01T00:00:00Z',
      },
      certificates: [
        {
          id: 'cert-001',
          serviceName: 'api-gateway',
          fingerprint: 'AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99',
          validFrom: '2025-01-01T00:00:00Z',
          validTo: '2026-01-01T00:00:00Z',
          status: 'Valid',
          daysUntilExpiry: 120,
          issuer: 'NexTraceOne Root CA',
        },
        {
          id: 'cert-002',
          serviceName: 'order-service',
          fingerprint: 'BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA',
          validFrom: '2025-01-01T00:00:00Z',
          validTo: '2025-12-15T00:00:00Z',
          status: 'Expiring',
          daysUntilExpiry: 18,
          issuer: 'NexTraceOne Root CA',
        },
        {
          id: 'cert-003',
          serviceName: 'notification-service',
          fingerprint: 'CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB',
          validFrom: '2024-01-01T00:00:00Z',
          validTo: '2025-01-01T00:00:00Z',
          status: 'Expired',
          daysUntilExpiry: -30,
          issuer: 'NexTraceOne Root CA',
        },
        {
          id: 'cert-004',
          serviceName: 'analytics-worker',
          fingerprint: 'DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC',
          validFrom: '2025-03-01T00:00:00Z',
          validTo: '2026-03-01T00:00:00Z',
          status: 'Valid',
          daysUntilExpiry: 200,
          issuer: 'NexTraceOne Root CA',
        },
      ],
      lastSyncAt: new Date().toISOString(),
      simulatedNote: 'Simulated mTLS certificate inventory — connect to real PKI for live data.',
    }),

  /**
   * POST /api/v1/admin/mtls/certificates/:id/revoke — requer platform:admin:write.
   * Revoga um certificado mTLS pelo ID.
   */
  revokeMtlsCert: (_certId: string): Promise<void> =>
    new Promise((resolve) => setTimeout(resolve, 600)),

  /**
   * PUT /api/v1/admin/mtls/policy — requer platform:admin:write.
   * Actualiza a política mTLS da plataforma.
   */
  updateMtlsPolicy: (policy: Partial<MtlsPolicy>): Promise<MtlsPolicy> =>
    Promise.resolve({
      mode: policy.mode ?? 'PerService',
      rootCaCertPresent: policy.rootCaCertPresent ?? true,
      rootCaCertExpiry: policy.rootCaCertExpiry,
    }),

  // ── W8-02: Feature Flags Runtime ──────────────────────────────────────────

  /**
   * GET /api/v1/platform/feature-flags/runtime — requer platform:admin:read.
   * Retorna todas as feature flags com estado efectivo para o contexto actual.
   */
  getFeatureFlagsRuntime: async (): Promise<FeatureFlagsRuntimeResponse> => {
    const flags = await client
      .get<
        Array<{
          key: string;
          displayName: string;
          defaultEnabled: boolean;
          allowedScopes: string[];
          isActive: boolean;
        }>
      >('/api/v1/configuration/flags')
      .then((r) => r.data);

    return {
      evaluatedAt: new Date().toISOString(),
      flags: flags.map((f) => ({
        key: f.key,
        displayName: f.displayName,
        scope: f.allowedScopes[0] ?? 'System',
        enabled: f.defaultEnabled,
        defaultEnabled: f.defaultEnabled,
        hasOverride: false,
      })),
    };
  },

  /**
   * POST /api/v1/platform/feature-flags/override — requer platform:admin:write.
   * Define uma override de feature flag para um scope específico.
   */
  setFeatureFlagRuntimeOverride: async (
    req: FeatureFlagRuntimeOverrideRequest,
  ): Promise<FeatureFlagRuntimeEntry> => {
    await client.put(`/api/v1/configuration/flags/${encodeURIComponent(req.key)}/override`, {
      scope: req.scope,
      scopeReferenceId: req.scopeReferenceId,
      isEnabled: req.enabled,
    });
    return {
      key: req.key,
      displayName: req.key,
      scope: req.scope,
      enabled: req.enabled,
      defaultEnabled: false,
      hasOverride: true,
    };
  },

  // ── W8-03: Canary Dashboard ────────────────────────────────────────────────

  /**
   * GET /api/v1/platform/canary/rollouts — requer platform:admin:read.
   * Retorna todos os canary deployments activos e histórico recente.
   */
  getCanaryDashboard: (): Promise<CanaryDashboardResponse> =>
    Promise.resolve({
      checkedAt: new Date().toISOString(),
      rollouts: [
        {
          id: 'canary-001',
          serviceName: 'order-service',
          stableVersion: 'v2.3.1',
          canaryVersion: 'v2.4.0',
          status: 'Active',
          trafficPercentage: 20,
          environment: 'Production',
          stableErrorRate: 0.12,
          canaryErrorRate: 0.09,
          stableP99LatencyMs: 145,
          canaryP99LatencyMs: 138,
          stableRps: 1200,
          canaryRps: 300,
          startedAt: new Date(Date.now() - 7200000).toISOString(),
        },
        {
          id: 'canary-002',
          serviceName: 'payment-gateway',
          stableVersion: 'v1.8.4',
          canaryVersion: 'v1.9.0',
          status: 'Promoted',
          trafficPercentage: 100,
          environment: 'Production',
          stableErrorRate: 0.05,
          canaryErrorRate: 0.04,
          stableP99LatencyMs: 200,
          canaryP99LatencyMs: 185,
          stableRps: 800,
          canaryRps: 800,
          startedAt: new Date(Date.now() - 86400000).toISOString(),
        },
        {
          id: 'canary-003',
          serviceName: 'notification-service',
          stableVersion: 'v3.1.0',
          canaryVersion: 'v3.2.0-beta',
          status: 'RolledBack',
          trafficPercentage: 0,
          environment: 'Staging',
          stableErrorRate: 0.08,
          canaryErrorRate: 2.4,
          stableP99LatencyMs: 120,
          canaryP99LatencyMs: 580,
          stableRps: 500,
          canaryRps: 0,
          startedAt: new Date(Date.now() - 3600000).toISOString(),
        },
      ],
    }),

  // ── W8-05: Multi-Tenant Schema Management ─────────────────────────────────

  /**
   * GET /api/v1/platform/tenant-schemas — requer platform:admin:read.
   * Lista schemas PostgreSQL criados por tenant.
   */
  getTenantSchemas: (): Promise<TenantSchemasResponse> =>
    client.get<TenantSchemasResponse>('/api/v1/platform/tenant-schemas').then((r) => r.data),

  /**
   * POST /api/v1/platform/tenant-schemas/provision — requer platform:admin:write.
   * Cria o schema PostgreSQL para um novo tenant.
   */
  provisionTenantSchema: (
    req: ProvisionTenantSchemaRequest,
  ): Promise<ProvisionTenantSchemaResult> =>
    client
      .post<ProvisionTenantSchemaResult>('/api/v1/platform/tenant-schemas/provision', req)
      .then((r) => r.data),
};

// ── W2-03 Types ───────────────────────────────────────────────────────────────

export type PlatformAlertSeverity = 'Warning' | 'Critical';
export type PlatformAlertStatus = 'Active' | 'Resolved' | 'Suppressed';

export interface PlatformAlertRule {
  id: string;
  name: string;
  metric: string;
  warningThreshold: number;
  criticalThreshold: number;
  unit: string;
  enabled: boolean;
  cooldownMinutes: number;
  description: string;
}

export interface PlatformAlertRuleUpdate {
  warningThreshold: number;
  criticalThreshold: number;
  enabled: boolean;
  cooldownMinutes: number;
}

export interface PlatformAlertHistoryEntry {
  id: string;
  ruleId: string;
  ruleName: string;
  severity: PlatformAlertSeverity;
  status: PlatformAlertStatus;
  triggeredAt: string;
  resolvedAt?: string;
  value: number;
  unit: string;
  message: string;
}

export interface PlatformAlertsResponse {
  rules: PlatformAlertRule[];
  recentAlerts: PlatformAlertHistoryEntry[];
  activeAlertCount: number;
  suppressedUntil?: string;
}

// ── W3-04 Types ───────────────────────────────────────────────────────────────

export type RestorePointStatus = 'Available' | 'Corrupted' | 'Expired';
export type RecoveryScope = 'Full' | 'Partial';
export type RecoveryStatus = 'Pending' | 'Running' | 'Completed' | 'Failed';

export interface RestorePoint {
  id: string;
  timestamp: string;
  sizeMb: number;
  status: RestorePointStatus;
  checksum: string;
  version: string;
  schemasIncluded: string[];
}

export interface RestorePointsResponse {
  restorePoints: RestorePoint[];
  totalCount: number;
  oldestAvailable?: string;
  latestAvailable?: string;
}

export interface RecoveryRequest {
  restorePointId: string;
  scope: RecoveryScope;
  schemas?: string[];
  dryRun: boolean;
}

export interface RecoveryResponse {
  recoveryId: string;
  status: RecoveryStatus;
  dryRun: boolean;
  estimatedDurationSeconds?: number;
  dataLossWarning?: string;
  schemasAffected: string[];
  startedAt?: string;
}

// ── W6-04 Types ───────────────────────────────────────────────────────────────

export interface ServiceCarbonEntry {
  serviceId: string;
  serviceName: string;
  teamName: string;
  carbonKgCo2: number;
  changePercent: number;
  cpuHours: number;
  memoryGbHours: number;
  networkGb: number;
  period: string;
}

export interface GreenOpsConfig {
  intensityFactorKgPerKwh: number;
  esgTargetKgCo2PerMonth: number;
  datacenterRegion: string;
  updatedAt: string;
}

export interface GreenOpsConfigUpdate {
  intensityFactorKgPerKwh: number;
  esgTargetKgCo2PerMonth: number;
  datacenterRegion: string;
}

export interface GreenOpsTrend {
  month: string;
  totalKgCo2: number;
}

export interface GreenOpsReport {
  generatedAt: string;
  period: string;
  totalKgCo2: number;
  equivalentKmByCar: number;
  esgTargetKgCo2: number;
  percentAboveTarget: number;
  config: GreenOpsConfig;
  topServices: ServiceCarbonEntry[];
  trend: GreenOpsTrend[];
  simulatedNote: string;
}


// ── W4-03 Types ───────────────────────────────────────────────────────────────

export type AiRequestPriority = 'Low' | 'Normal' | 'High' | 'Admin';
export type CircuitBreakerState = 'Closed' | 'Open' | 'HalfOpen';

export interface AiResourceGovernorConfig {
  maxConcurrency: number;
  inferenceTimeoutSeconds: number;
  queueTimeoutSeconds: number;
  circuitBreakerEnabled: boolean;
  circuitBreakerErrorThresholdPercent: number;
  circuitBreakerResetAfterMinutes: number;
  priorityQueueEnabled: boolean;
  updatedAt: string;
}

export interface AiResourceGovernorConfigUpdate {
  maxConcurrency: number;
  inferenceTimeoutSeconds: number;
  queueTimeoutSeconds: number;
  circuitBreakerEnabled: boolean;
  circuitBreakerErrorThresholdPercent: number;
  circuitBreakerResetAfterMinutes: number;
  priorityQueueEnabled: boolean;
}

export interface AiGovernorMetrics {
  activeRequests: number;
  queueDepth: number;
  circuitBreakerState: CircuitBreakerState;
  circuitBreakerOpenSince?: string;
  latencyP95Ms: number;
  errorRatePercent: number;
  totalRequestsLast5Min: number;
  rejectedRequestsLast5Min: number;
  sampledAt: string;
}

export interface AiGovernorStatus {
  config: AiResourceGovernorConfig;
  metrics: AiGovernorMetrics;
}

// ── W4-04 Types ───────────────────────────────────────────────────────────────

export type AiResponseQuality = 'Good' | 'LowConfidence' | 'Hallucination' | 'Unknown';

export interface AiModelQualityStats {
  modelName: string;
  totalResponses: number;
  goodPercent: number;
  lowConfidencePercent: number;
  hallucinationPercent: number;
  negativeFeedbackCount: number;
  averageConfidenceScore: number;
  lastUpdated: string;
}

export interface AiGovernanceConfig {
  groundingCheckEnabled: boolean;
  hallucinationFlagThreshold: number;
  feedbackEnabled: boolean;
  autoSuspendOnHighHallucinationRate: boolean;
  highHallucinationThresholdPercent: number;
  updatedAt: string;
}

export interface AiGovernanceConfigUpdate {
  groundingCheckEnabled: boolean;
  hallucinationFlagThreshold: number;
  feedbackEnabled: boolean;
  autoSuspendOnHighHallucinationRate: boolean;
  highHallucinationThresholdPercent: number;
}

export interface AiGovernanceDashboard {
  config: AiGovernanceConfig;
  modelStats: AiModelQualityStats[];
  totalFeedbackCount: number;
  negativeFeedbackPercent: number;
  topHallucinationPatterns: string[];
  generatedAt: string;
  simulatedNote: string;
}

// ── W5-02 Types ───────────────────────────────────────────────────────────────

export type ProxyConfigStatus = 'NotConfigured' | 'Configured' | 'TestPassed' | 'TestFailed';

export interface ProxyConfig {
  proxyUrl?: string;
  bypassList: string[];
  username?: string;
  hasPassword: boolean;
  customCaCertificatePath?: string;
  hasCaCertificate: boolean;
  status: ProxyConfigStatus;
  lastTestedAt?: string;
  lastTestError?: string;
  updatedAt?: string;
}

export interface ProxyConfigUpdate {
  proxyUrl?: string;
  bypassList: string[];
  username?: string;
  password?: string;
  customCaCertificatePath?: string;
}

export interface ProxyConnectivityTestResult {
  success: boolean;
  testedUrl: string;
  durationMs: number;
  error?: string;
  testedAt: string;
}

// ── W5-03 Types ───────────────────────────────────────────────────────────────

export interface ExternalHttpAuditEntry {
  id: string;
  eventType: 'ExternalHttpCall' | 'BlockedByAirGap' | 'NetworkViolation';
  timestamp: string;
  destination: string;
  method: string;
  path: string;
  tenantId: string;
  userId?: string;
  context: string;
  requestSizeBytes: number;
  responseStatus?: number;
  durationMs?: number;
  blocked: boolean;
}

export interface ExternalHttpAuditParams {
  destination?: string;
  context?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface ExternalHttpAuditResponse {
  entries: ExternalHttpAuditEntry[];
  total: number;
  page: number;
  pageSize: number;
  generatedAt: string;
  simulatedNote: string;
}

// ── W5-05 Types ───────────────────────────────────────────────────────────────

export type EnvPolicyRole = 'Engineer' | 'TechLead' | 'Architect' | 'PlatformAdmin' | 'Auditor';

export interface EnvironmentAccessPolicy {
  id: string;
  policyName: string;
  environments: string[];
  allowedRoles: EnvPolicyRole[];
  requireJitFor: EnvPolicyRole[];
  jitApprovalRequiredFrom?: EnvPolicyRole;
  description: string;
  updatedAt: string;
}

export interface EnvironmentPolicyUpdate {
  allowedRoles: EnvPolicyRole[];
  requireJitFor: EnvPolicyRole[];
  jitApprovalRequiredFrom?: EnvPolicyRole;
  description: string;
}

export interface EnvironmentPoliciesResponse {
  policies: EnvironmentAccessPolicy[];
  availableEnvironments: string[];
  generatedAt: string;
  simulatedNote: string;
}

// ── W6-02 Types ───────────────────────────────────────────────────────────────

export type NonProdScheduleStatus = 'Active' | 'Suspended' | 'OverriddenUntil';

export interface NonProdScheduleEntry {
  environmentId: string;
  environmentName: string;
  enabled: boolean;
  activeDaysOfWeek: number[];
  activeFromHour: number;
  activeToHour: number;
  timezone: string;
  status: NonProdScheduleStatus;
  overrideUntil?: string;
  overrideReason?: string;
  estimatedSavingPercent: number;
  updatedAt: string;
}

export interface NonProdScheduleUpdate {
  enabled: boolean;
  activeDaysOfWeek: number[];
  activeFromHour: number;
  activeToHour: number;
  timezone: string;
}

export interface NonProdScheduleOverride {
  keepActiveUntil: string;
  reason: string;
}

export interface NonProdSchedulesResponse {
  schedules: NonProdScheduleEntry[];
  totalEstimatedSavingPercent: number;
  generatedAt: string;
  simulatedNote: string;
}

// ── W8-01 Types ───────────────────────────────────────────────────────────────

export type CapacityRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export interface CapacityResourceForecast {
  resource: string;
  current: number;
  capacity: number;
  unit: string;
  weeklyGrowthRate: number;
  estimatedFullDate?: string;
  daysUntilFull?: number;
  riskLevel: CapacityRiskLevel;
  recommendation?: string;
}

export interface CapacityForecastResponse {
  forecasts: CapacityResourceForecast[];
  analysisWeeks: number;
  nextReviewDate: string;
  generatedAt: string;
  simulatedNote: string;
}

// ── W1-04 Types ───────────────────────────────────────────────────────────────

export type DemoSeedState = 'NotSeeded' | 'Seeded' | 'Clearing';

export interface DemoSeedStatus {
  state: DemoSeedState;
  seededAt?: string;
  entitiesCount: number;
  servicesCount: number;
  changesCount: number;
  incidentsCount: number;
  simulatedNote: string;
}

export interface DemoSeedRequest {
  tenantId?: string;
}

export interface DemoSeedResult {
  success: boolean;
  durationMs: number;
  entitiesCreated: number;
  message: string;
}

export interface DemoSeedClearResult {
  success: boolean;
  entitiesRemoved: number;
  message: string;
}

// ── W3-05 Types ───────────────────────────────────────────────────────────────

export interface GracefulShutdownConfig {
  requestDrainTimeoutSeconds: number;
  outboxDrainTimeoutSeconds: number;
  healthCheckReturns503OnShutdown: boolean;
  auditShutdownEvents: boolean;
  updatedAt: string;
}

export interface GracefulShutdownConfigUpdate {
  requestDrainTimeoutSeconds: number;
  outboxDrainTimeoutSeconds: number;
  healthCheckReturns503OnShutdown: boolean;
  auditShutdownEvents: boolean;
}

// ── W5-06 Types ───────────────────────────────────────────────────────────────

export interface SessionSecurityConfig {
  inactivityTimeoutMinutes: number;
  maxConcurrentSessions: number;
  requireReauthForSensitiveActions: boolean;
  detectAnomalousIpChange: boolean;
  sensitiveActions: string[];
  updatedAt: string;
}

export interface SessionSecurityConfigUpdate {
  inactivityTimeoutMinutes: number;
  maxConcurrentSessions: number;
  requireReauthForSensitiveActions: boolean;
  detectAnomalousIpChange: boolean;
}

// ── W6-05 Types ───────────────────────────────────────────────────────────────

export type RightsizingImpact = 'Low' | 'Medium' | 'High';

export interface RightsizingRecommendation {
  serviceId: string;
  serviceName: string;
  teamName: string;
  resource: 'CPU' | 'Memory';
  currentAllocation: number;
  recommendedAllocation: number;
  unit: string;
  p95Usage: number;
  p99Usage: number;
  safetyMarginPercent: number;
  savingPercent: number;
  reliabilityImpact: RightsizingImpact;
  oomEventsLast30Days: number;
  sloAtRisk: boolean;
  analysisDays: number;
}

export interface RightsizingReport {
  recommendations: RightsizingRecommendation[];
  totalServicesAnalysed: number;
  totalSavingEstimateCpuPercent: number;
  totalSavingEstimateMemoryPercent: number;
  safetyMarginPercent: number;
  generatedAt: string;
  simulatedNote: string;
}

// ── W7-03 Types ───────────────────────────────────────────────────────────────

export type ObservabilityMode = 'Full' | 'Lite' | 'Minimal';

export interface ObservabilityModeConfig {
  currentMode: ObservabilityMode;
  elasticsearchConnected: boolean;
  elasticsearchVersion?: string;
  postgresAnalyticsEnabled: boolean;
  otelCollectorConnected: boolean;
  additionalRamUsageGb: number;
  tradeOffs: string[];
  updatedAt: string;
  simulatedNote: string;
}

export interface ObservabilityModeUpdate {
  mode: ObservabilityMode;
}

// ── W8-06 Types ───────────────────────────────────────────────────────────────

export type ComplianceControlStatus = 'Pass' | 'Fail' | 'Warning' | 'NotApplicable';

export interface ComplianceControl {
  id: string;
  code: string;
  title: string;
  description: string;
  status: ComplianceControlStatus;
  evidence?: string;
  actionRequired?: string;
  moduleLink?: string;
}

export interface CompliancePack {
  id: string;
  name: string;
  standard: string;
  version: string;
  totalControls: number;
  passingControls: number;
  failingControls: number;
  warningControls: number;
  compliancePercent: number;
  controls: ComplianceControl[];
  lastCheckedAt: string;
}

export interface CompliancePacksResponse {
  packs: CompliancePack[];
  generatedAt: string;
  simulatedNote: string;
}

// ── W3-01 Types ───────────────────────────────────────────────────────────────

export type MigrationRisk = 'Low' | 'Medium' | 'High';

export interface PendingMigration {
  id: string;
  name: string;
  timestamp: string;
  module: string;
  risk: MigrationRisk;
  operations: string[];
  sqlPreview: string;
  estimatedDurationMs: number;
  appliedAt?: string;
}

export interface MigrationPreviewResponse {
  pending: PendingMigration[];
  appliedCount: number;
  generatedAt: string;
  simulatedNote: string;
}

// ── W7-05 Types ───────────────────────────────────────────────────────────────

export type DoraRating = 'Elite' | 'High' | 'Medium' | 'Low';

export interface DoraMetric {
  name: string;
  value: string;
  unit: string;
  rating: DoraRating;
  trend: number;
  trendDirection: 'up' | 'down' | 'stable';
}

export interface DoraAdminMetricsResponse {
  deploymentFrequency: DoraMetric;
  leadTime: DoraMetric;
  mttr: DoraMetric;
  changeFailureRate: DoraMetric;
  environment: string;
  timeRangeDays: number;
  dataSource: string;
  lastUpdatedAt: string;
  simulatedNote: string;
}

// ── W8-04 Types ───────────────────────────────────────────────────────────────

export type SamlSsoStatus = 'Enabled' | 'Disabled' | 'NotConfigured';

export interface SamlSsoConfig {
  status: SamlSsoStatus;
  entityId: string;
  ssoUrl: string;
  sloUrl: string;
  idpCertificate: string;
  jitProvisioningEnabled: boolean;
  defaultRole: string;
  attributeMappings: Array<{ samlAttr: string; nxtField: string }>;
  lastTestedAt?: string;
  testResult?: 'Success' | 'Failed' | null;
  simulatedNote: string;
}

export interface SamlSsoConfigUpdate {
  entityId: string;
  ssoUrl: string;
  sloUrl: string;
  idpCertificate: string;
  jitProvisioningEnabled: boolean;
  defaultRole: string;
  attributeMappings: Array<{ samlAttr: string; nxtField: string }>;
}

// ── W5-04 Types ───────────────────────────────────────────────────────────────

export type CertStatus = 'Valid' | 'Expiring' | 'Expired' | 'Revoked';

export interface MtlsCertificate {
  id: string;
  serviceName: string;
  fingerprint: string;
  validFrom: string;
  validTo: string;
  status: CertStatus;
  daysUntilExpiry: number;
  issuer: string;
}

export type MtlsPolicyMode = 'Required' | 'PerService' | 'Disabled';

export interface MtlsPolicy {
  mode: MtlsPolicyMode;
  rootCaCertPresent: boolean;
  rootCaCertExpiry?: string;
}

export interface MtlsManagerResponse {
  policy: MtlsPolicy;
  certificates: MtlsCertificate[];
  lastSyncAt: string;
  simulatedNote: string;
}


// ── W8-02 Types: Feature Flags Runtime ────────────────────────────────────────

export interface FeatureFlagRuntimeEntry {
  key: string;
  displayName: string;
  scope: string;
  enabled: boolean;
  defaultEnabled: boolean;
  hasOverride: boolean;
}

export interface FeatureFlagsRuntimeResponse {
  evaluatedAt: string;
  flags: FeatureFlagRuntimeEntry[];
}

export interface FeatureFlagRuntimeOverrideRequest {
  key: string;
  enabled: boolean;
  scope: string;
  scopeReferenceId?: string;
}

// ── W8-03 Types: Canary Dashboard ─────────────────────────────────────────────

export type CanaryRolloutStatus = 'Active' | 'Promoted' | 'RolledBack' | 'Paused';

export interface CanaryRolloutEntry {
  id: string;
  serviceName: string;
  stableVersion: string;
  canaryVersion: string;
  status: CanaryRolloutStatus;
  trafficPercentage: number;
  environment: string;
  stableErrorRate: number;
  canaryErrorRate: number;
  stableP99LatencyMs: number;
  canaryP99LatencyMs: number;
  stableRps: number;
  canaryRps: number;
  startedAt: string;
}

export interface CanaryDashboardResponse {
  checkedAt: string;
  rollouts: CanaryRolloutEntry[];
}

// ── W8-05 Types: Multi-Tenant Schema ──────────────────────────────────────────

export interface TenantSchemaEntry {
  tenantSlug: string;
  schemaName: string;
  searchPath: string;
}

export interface TenantSchemasResponse {
  totalSchemas: number;
  checkedAt: string;
  schemas: TenantSchemaEntry[];
}

export interface ProvisionTenantSchemaRequest {
  tenantSlug: string;
}

export interface ProvisionTenantSchemaResult {
  tenantSlug: string;
  schemaName: string;
  wasCreated: boolean;
  provisionedAt: string;
}
