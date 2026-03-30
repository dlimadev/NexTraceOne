namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Events;

// ═══════════════════════════════════════════════════════════════════════════════
// PRODUCT ANALYTICS EVENTS (pan_*)
// Módulo: Product Analytics (13)
// Tabela destino: nextraceone_analytics.pan_events
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Registo analítico de evento de uso de produto.
/// Representa um evento gerado pela plataforma (page view, action, search, etc.)
/// para armazenamento no storage analítico (Elasticsearch por padrão — pan_events).
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId → iam_tenants.Id
///   - UserId   → iam_users.Id
/// </summary>
public sealed record ProductAnalyticsRecord(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Persona,
    string Module,
    byte EventType,
    string Feature,
    string EntityType,
    string Outcome,
    string Route,
    Guid? TeamId,
    Guid? DomainId,
    string SessionId,
    string ClientType,
    string MetadataJson,
    DateTimeOffset OccurredAt,
    Guid? EnvironmentId,
    uint? DurationMs,
    Guid? ParentEventId,
    string Source
);

// ═══════════════════════════════════════════════════════════════════════════════
// OPERATIONAL INTELLIGENCE EVENTS (ops_*)
// Módulo: Operational Intelligence (06)
// Tabelas destino: ops_runtime_metrics, ops_cost_entries, ops_incident_trends
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Registo de métrica de runtime de um serviço num ambiente.
/// Projectado a partir de ops_runtime_snapshots do PostgreSQL.
/// Tabela destino: nextraceone_analytics.ops_runtime_metrics
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId     → iam_tenants.Id
///   - ServiceId    → cat_service_assets.Id (opcional — pode ser só ServiceName)
///   - EnvironmentId → env_environments.Id (opcional)
/// </summary>
public sealed record RuntimeMetricRecord(
    Guid Id,
    Guid TenantId,
    string ServiceName,
    Guid? ServiceId,
    string Environment,
    Guid? EnvironmentId,
    string Source,
    decimal AvgLatencyMs,
    decimal P99LatencyMs,
    decimal ErrorRate,
    decimal RequestsPerSecond,
    decimal CpuUsagePercent,
    decimal MemoryUsageMb,
    uint ActiveInstances,
    string HealthStatus,
    DateTimeOffset CapturedAt
);

/// <summary>
/// Registo de entrada de custo operacional por serviço/ambiente/período.
/// Projectado a partir de ops_cost_snapshots e ops_cost_records do PostgreSQL.
/// Tabela destino: nextraceone_analytics.ops_cost_entries
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId     → iam_tenants.Id
///   - ServiceId    → cat_service_assets.Id (opcional)
///   - EnvironmentId → env_environments.Id (opcional)
/// </summary>
public sealed record CostEntryRecord(
    Guid Id,
    Guid TenantId,
    string ServiceName,
    Guid? ServiceId,
    string Environment,
    Guid? EnvironmentId,
    string Currency,
    string Period,
    string Source,
    decimal TotalCost,
    decimal CpuCostShare,
    decimal MemoryCostShare,
    decimal NetworkCostShare,
    decimal StorageCostShare,
    DateTimeOffset CapturedAt
);

/// <summary>
/// Evento de ciclo de vida de incidente para análise de tendências.
/// Nota: o estado activo do incidente permanece no PostgreSQL (ops_incidents).
/// Este registo é uma projecção de eventos do ciclo de vida do incidente.
/// Tabela destino: nextraceone_analytics.ops_incident_trends
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId    → iam_tenants.Id
///   - IncidentId  → ops_incidents.Id
///   - ServiceId   → cat_service_assets.Id (opcional)
/// </summary>
public sealed record IncidentTrendRecord(
    Guid EventId,
    Guid IncidentId,
    Guid TenantId,
    string ServiceName,
    Guid? ServiceId,
    string Environment,
    Guid? EnvironmentId,
    string Severity,
    string IncidentType,
    string LifecycleEvent,
    bool ChangeCorrelated,
    uint? MttrMinutes,
    DateTimeOffset OccurredAt
);

// ═══════════════════════════════════════════════════════════════════════════════
// INTEGRATIONS EVENTS (int_*)
// Módulo: Integrations (12)
// Tabelas destino: int_execution_logs, int_health_history
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Log de execução de conector de integração para storage analítico.
/// Projectado a partir de int_ingestion_executions (execuções completadas).
/// Tabela destino: nextraceone_analytics.int_execution_logs
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId    → iam_tenants.Id
///   - ConnectorId → int_connectors.Id
///   - SourceId    → int_ingestion_sources.Id (opcional)
/// </summary>
public sealed record IntegrationExecutionRecord(
    Guid Id,
    Guid TenantId,
    Guid ConnectorId,
    string ConnectorName,
    string ConnectorType,
    string Provider,
    Guid? SourceId,
    string DataDomain,
    string? CorrelationId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string Result,
    int ItemsProcessed,
    int ItemsSucceeded,
    int ItemsFailed,
    string? ErrorCode,
    int RetryAttempt,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Evento de transição de health de um conector de integração.
/// Registado quando o health status de um conector muda (Healthy → Degraded, etc.).
/// Tabela destino: nextraceone_analytics.int_health_history
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId    → iam_tenants.Id
///   - ConnectorId → int_connectors.Id
/// </summary>
public sealed record ConnectorHealthRecord(
    Guid TenantId,
    Guid ConnectorId,
    string ConnectorName,
    string Health,
    string PreviousHealth,
    int? FreshnessLagMinutes,
    DateTimeOffset ChangedAt
);

// ═══════════════════════════════════════════════════════════════════════════════
// GOVERNANCE ANALYTICS EVENTS (gov_*)
// Módulo: Governance (08)
// Tabelas destino: gov_compliance_trends, gov_finops_aggregates
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Snapshot de score de compliance de um serviço/política num momento.
/// Projectado periodicamente pelo módulo Governance.
/// Tabela destino: nextraceone_analytics.gov_compliance_trends
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId  → iam_tenants.Id
///   - ServiceId → cat_service_assets.Id (opcional)
///   - PolicyId  → gov_compliance_policies.Id (opcional)
/// </summary>
public sealed record ComplianceTrendRecord(
    Guid TenantId,
    Guid? ServiceId,
    string ServiceName,
    Guid? PolicyId,
    string PolicyName,
    string Environment,
    decimal ComplianceScore,
    string Status,
    uint ViolationsCount,
    DateTimeOffset CapturedAt
);

/// <summary>
/// Agregação FinOps contextual por equipa/domínio/serviço/período.
/// Projectada pelo módulo Governance a partir de dados de custo consolidados.
/// Tabela destino: nextraceone_analytics.gov_finops_aggregates
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId  → iam_tenants.Id
///   - TeamId    → iam_teams.Id (opcional)
///   - ServiceId → cat_service_assets.Id (opcional)
/// </summary>
public sealed record FinOpsAggregateRecord(
    Guid TenantId,
    Guid? TeamId,
    string TeamName,
    string DomainName,
    string ServiceName,
    Guid? ServiceId,
    string Environment,
    string Currency,
    string PeriodLabel,
    decimal TotalCost,
    decimal ComputeCost,
    decimal StorageCost,
    decimal NetworkCost,
    bool AnomalyDetected,
    DateTimeOffset CapturedAt
);

// ═══════════════════════════════════════════════════════════════════════════════
// CHANGE INTELLIGENCE EVENTS (chg_*)
// Módulo: Change Governance (10)
// Tabela destino: nextraceone_analytics.chg_trace_release_mapping
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Registo analítico de correlação trace → release.
/// Representa o mapeamento entre um trace distribuído (OTel) e uma Release do
/// módulo Change Governance, para armazenamento no storage analítico (Elasticsearch por padrão — chg_trace_release_mapping).
///
/// Chaves de correlação com PostgreSQL:
///   - TenantId     → iam_tenants.Id
///   - ReleaseId    → chg_releases.Id
///   - ServiceId    → cat_service_assets.Id (opcional)
///   - EnvironmentId → env_environments.Id (opcional)
///
/// Correlação com nextraceone_obs:
///   - TraceId      → traces no storage analítico (Elasticsearch ou ClickHouse — sem FK)
/// </summary>
public sealed record TraceReleaseMappingRecord(
    Guid Id,
    Guid TenantId,
    Guid ReleaseId,
    string TraceId,
    string ServiceName,
    Guid? ServiceId,
    string Environment,
    Guid? EnvironmentId,
    string CorrelationSource,
    DateTimeOffset? TraceStartedAt,
    DateTimeOffset? TraceEndedAt,
    DateTimeOffset CorrelatedAt
);
