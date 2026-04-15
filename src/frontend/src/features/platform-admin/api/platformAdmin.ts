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
