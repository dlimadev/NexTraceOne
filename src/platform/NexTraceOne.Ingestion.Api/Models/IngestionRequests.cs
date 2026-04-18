using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.Ingestion.Api.Models;

// ── Existing request models ──────────────────────────────────────────────────

/// <summary>Request para eventos de deployment de pipelines CI/CD.</summary>
public sealed record DeploymentEventRequest(
    string Provider,
    string? Source,
    string? CorrelationId,
    string? ServiceName,
    string? Environment,
    string? Version,
    string? CommitSha);

/// <summary>Request para eventos de promoção entre ambientes.</summary>
public sealed record PromotionEventRequest(
    string? CorrelationId,
    string? ServiceName,
    string? FromEnvironment,
    string? ToEnvironment,
    string? Version);

/// <summary>Request para sinais de runtime de serviços monitorados.</summary>
public sealed record RuntimeSignalRequest(
    string? ServiceName,
    string? SignalType,
    string? Message,
    Dictionary<string, string>? Tags);

/// <summary>Request para sincronização de consumidores e dependências.</summary>
public sealed record ConsumerSyncRequest(
    string? ServiceName,
    List<string>? Consumers,
    List<string>? Dependencies);

/// <summary>Request para sincronização de contratos de fontes externas.</summary>
public sealed record ContractSyncRequest(
    string? Provider,
    List<ContractItem>? Contracts);

/// <summary>Item de contrato individual para sincronização.</summary>
public sealed record ContractItem(
    string Name,
    string Type,
    string? Version,
    string? Content);

// ── Change Intelligence — ingestão de commits ────────────────────────────────

/// <summary>Request para ingestão de um commit de um repositório externo (GitHub, GitLab, Azure DevOps).</summary>
public sealed record IngestCommitRequest(
    string? CorrelationId,
    string ServiceName,
    string CommitSha,
    string Branch,
    string Author,
    string Message,
    DateTimeOffset CommittedAt,
    string? PipelineSource,
    string? ExternalSystem);

// ── Change Intelligence — ingestão de releases externas ─────────────────────

/// <summary>Request para ingestão de uma release criada por sistema externo.</summary>
public sealed record IngestExternalReleaseRequest(
    string? CorrelationId,
    string ExternalReleaseId,
    string ExternalSystem,
    string ServiceName,
    string Version,
    string TargetEnvironment,
    string? Description,
    List<string>? CommitShas,
    List<ExternalWorkItemRefRequest>? WorkItems,
    bool TriggerPromotion,
    Guid? EnvironmentId);

/// <summary>Referência de work item externo associado a uma release.</summary>
public sealed record ExternalWorkItemRefRequest(string Id, string System);

// ── Change Intelligence — feature flags pré-deploy ──────────────────────────

/// <summary>Request para registo do estado de feature flags no momento da release.</summary>
public sealed record RecordFeatureFlagStateRequest(
    string? CorrelationId,
    /// <summary>
    /// Identificador interno da release no NexTraceOne.
    /// Mutuamente exclusivo com ExternalReleaseId + ExternalSystem.
    /// </summary>
    Guid? ReleaseId,
    /// <summary>Identificador da release no sistema de origem externo (ex: "jenkins-build-42").</summary>
    string? ExternalReleaseId,
    /// <summary>Nome do sistema externo de origem (ex: "jenkins", "github", "azuredevops").</summary>
    string? ExternalSystem,
    int ActiveFlagCount,
    int CriticalFlagCount,
    int NewFeatureFlagCount,
    string FlagProvider,
    string? FlagsJson);

// ── Change Intelligence — canary rollout ─────────────────────────────────────

/// <summary>Request para registo do progresso de um canary deployment.</summary>
public sealed record RecordCanaryRolloutRequest(
    string? CorrelationId,
    /// <summary>
    /// Identificador interno da release no NexTraceOne.
    /// Mutuamente exclusivo com ExternalReleaseId + ExternalSystem.
    /// </summary>
    Guid? ReleaseId,
    /// <summary>Identificador da release no sistema de origem externo (ex: "argo-rollout-42").</summary>
    string? ExternalReleaseId,
    /// <summary>Nome do sistema externo de origem (ex: "argorollouts", "flagger").</summary>
    string? ExternalSystem,
    decimal RolloutPercentage,
    int ActiveInstances,
    int TotalInstances,
    string SourceSystem,
    bool IsPromoted,
    bool IsAborted);

// ── Change Intelligence — métricas de observação pós-release ────────────────

/// <summary>Request para ingestão de métricas observadas numa janela pós-release.</summary>
public sealed record RecordObservationMetricsRequest(
    string? CorrelationId,
    /// <summary>
    /// Identificador interno da release no NexTraceOne.
    /// Mutuamente exclusivo com ExternalReleaseId + ExternalSystem.
    /// </summary>
    Guid? ReleaseId,
    /// <summary>Identificador da release no sistema de origem externo.</summary>
    string? ExternalReleaseId,
    /// <summary>Nome do sistema externo de origem (ex: "datadog", "newrelic", "otel-collector").</summary>
    string? ExternalSystem,
    ObservationPhase Phase,
    DateTimeOffset WindowStartsAt,
    DateTimeOffset WindowEndsAt,
    decimal RequestsPerMinute,
    decimal ErrorRate,
    decimal AvgLatencyMs,
    decimal P95LatencyMs,
    decimal P99LatencyMs,
    decimal Throughput);

// ── Change Intelligence — rollback ───────────────────────────────────────────

/// <summary>Request para registo de um rollback de release para a release original.</summary>
public sealed record RegisterRollbackRequest(
    string? CorrelationId,
    /// <summary>
    /// Identificador interno da release que está a ser revertida (a release do rollback).
    /// Mutuamente exclusivo com ExternalReleaseId + ExternalSystem.
    /// </summary>
    Guid? ReleaseId,
    /// <summary>Identificador externo da release que está a ser revertida.</summary>
    string? ExternalReleaseId,
    /// <summary>Nome do sistema externo de origem para a release do rollback.</summary>
    string? ExternalSystem,
    /// <summary>
    /// Identificador interno da release original para a qual se faz rollback.
    /// Mutuamente exclusivo com OriginalExternalReleaseId + OriginalExternalSystem (ou ExternalSystem se omitido).
    /// </summary>
    Guid? OriginalReleaseId,
    /// <summary>Identificador externo da release original para a qual se faz rollback.</summary>
    string? OriginalExternalReleaseId,
    /// <summary>
    /// Nome do sistema externo de origem para a release original.
    /// Quando omitido, usa o mesmo valor de ExternalSystem.
    /// Permite rollback entre sistemas diferentes — ex: reverter de GitHub Actions para Jenkins.
    /// </summary>
    string? OriginalExternalSystem);

// ── Operational Intelligence — runtime snapshot ──────────────────────────────

/// <summary>Request para ingestão de um snapshot de saúde e performance de runtime.</summary>
public sealed record IngestRuntimeSnapshotRequest(
    string? CorrelationId,
    string ServiceName,
    string Environment,
    decimal AvgLatencyMs,
    decimal P99LatencyMs,
    decimal ErrorRate,
    decimal RequestsPerSecond,
    decimal CpuUsagePercent,
    decimal MemoryUsageMb,
    int ActiveInstances,
    DateTimeOffset CapturedAt,
    string Source);

// ── Operational Intelligence — cost snapshot ─────────────────────────────────

/// <summary>Request para ingestão de um snapshot de custo de infraestrutura.</summary>
public sealed record IngestCostSnapshotRequest(
    string? CorrelationId,
    string ServiceName,
    string Environment,
    decimal TotalCost,
    decimal CpuCostShare,
    decimal MemoryCostShare,
    decimal NetworkCostShare,
    decimal StorageCostShare,
    DateTimeOffset CapturedAt,
    string Source,
    string Period,
    string Currency = "USD");

// ── Operational Intelligence — incident ──────────────────────────────────────

/// <summary>Request para criação de incidente originado em sistema externo de alerta.</summary>
public sealed record CreateIncidentRequest(
    string? CorrelationId,
    string Title,
    string Description,
    IncidentType IncidentType,
    IncidentSeverity Severity,
    string ServiceId,
    string ServiceDisplayName,
    string OwnerTeam,
    string? ImpactedDomain,
    string Environment,
    DateTimeOffset? DetectedAtUtc);

/// <summary>
/// Request para resolução de incidente via Ingestion API.
/// Enviado por PagerDuty, OpsGenie, Alertmanager ou pipelines de remediação automática
/// quando o serviço é confirmado como restaurado.
/// </summary>
public sealed record ResolveIncidentRequest(
    string? CorrelationId,
    /// <summary>Data/hora UTC em que o serviço foi confirmado como restaurado. Usa UtcNow quando omitido.</summary>
    DateTimeOffset? ResolvedAtUtc,
    /// <summary>Nota de resolução opcional (ex: causa raiz identificada, acção tomada).</summary>
    string? ResolutionNote);
