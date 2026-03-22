using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Entidade que armazena um snapshot computado de confiabilidade de um serviço num ponto no tempo.
/// Persiste o resultado do cálculo de score para suporte a trending histórico.
///
/// Fórmula: OverallScore = (RuntimeHealthScore × 0.50) + (IncidentImpactScore × 0.30) + (ObservabilityScore × 0.20)
/// </summary>
public sealed class ReliabilitySnapshot : AuditableEntity<ReliabilitySnapshotId>
{
    private ReliabilitySnapshot() { }

    public Guid TenantId { get; private set; }
    public string ServiceId { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public decimal OverallScore { get; private set; }
    public decimal RuntimeHealthScore { get; private set; }
    public decimal IncidentImpactScore { get; private set; }
    public decimal ObservabilityScore { get; private set; }
    public int OpenIncidentCount { get; private set; }
    public string RuntimeHealthStatus { get; private set; } = string.Empty;
    public TrendDirection TrendDirection { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static ReliabilitySnapshot Create(
        Guid tenantId,
        string serviceId,
        string environment,
        decimal overallScore,
        decimal runtimeHealthScore,
        decimal incidentImpactScore,
        decimal observabilityScore,
        int openIncidentCount,
        string runtimeHealthStatus,
        TrendDirection trendDirection,
        DateTimeOffset computedAt)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(runtimeHealthStatus);

        return new ReliabilitySnapshot
        {
            Id = ReliabilitySnapshotId.New(),
            TenantId = tenantId,
            ServiceId = serviceId,
            Environment = environment,
            OverallScore = Math.Clamp(overallScore, 0m, 100m),
            RuntimeHealthScore = Math.Clamp(runtimeHealthScore, 0m, 100m),
            IncidentImpactScore = Math.Clamp(incidentImpactScore, 0m, 100m),
            ObservabilityScore = Math.Clamp(observabilityScore, 0m, 100m),
            OpenIncidentCount = openIncidentCount,
            RuntimeHealthStatus = runtimeHealthStatus,
            TrendDirection = trendDirection,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ReliabilitySnapshot.</summary>
public sealed record ReliabilitySnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static ReliabilitySnapshotId New() => new(Guid.NewGuid());
    public static ReliabilitySnapshotId From(Guid id) => new(id);
}
