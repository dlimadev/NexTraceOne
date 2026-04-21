using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

/// <summary>
/// Snapshot de métricas DORA e maturidade operacional de um tenant num período específico.
/// Pode ser marcado como anonimizado para inclusão em benchmarks cross-tenant.
/// Privacidade: apenas agregados são partilhados — nunca dados individuais de outros tenants.
/// </summary>
public sealed class BenchmarkSnapshotRecord : AuditableEntity<BenchmarkSnapshotRecordId>
{
    private BenchmarkSnapshotRecord() { }

    /// <summary>Identificador do tenant proprietário deste snapshot.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Início do período de medição.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de medição.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Frequência de deployments por semana (DORA).</summary>
    public decimal DeploymentFrequencyPerWeek { get; private set; }

    /// <summary>Lead time médio para mudanças em horas (DORA).</summary>
    public decimal LeadTimeForChangesHours { get; private set; }

    /// <summary>Taxa de falha de mudanças em percentagem (DORA).</summary>
    public decimal ChangeFailureRatePercent { get; private set; }

    /// <summary>Mean time to restore em horas (DORA).</summary>
    public decimal MeanTimeToRestoreHours { get; private set; }

    /// <summary>Score de maturidade global (0-100).</summary>
    public decimal MaturityScore { get; private set; }

    /// <summary>Custo por request em USD (métrica opcional de FinOps).</summary>
    public decimal? CostPerRequestUsd { get; private set; }

    /// <summary>Número de serviços incluídos na medição.</summary>
    public int ServiceCount { get; private set; }

    /// <summary>Indica se este snapshot pode ser incluído em agregados cross-tenant anonimizados.</summary>
    public bool IsAnonymizedForBenchmarks { get; private set; }

    /// <summary>Regista um novo snapshot de benchmarks DORA para um tenant.</summary>
    public static BenchmarkSnapshotRecord Record(
        string tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal deploymentFrequencyPerWeek,
        decimal leadTimeForChangesHours,
        decimal changeFailureRatePercent,
        decimal meanTimeToRestoreHours,
        decimal maturityScore,
        int serviceCount,
        DateTimeOffset now,
        decimal? costPerRequestUsd = null)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.OutOfRange(maturityScore, nameof(maturityScore), 0m, 100m);
        Guard.Against.Negative(serviceCount, nameof(serviceCount));

        return new BenchmarkSnapshotRecord
        {
            Id = BenchmarkSnapshotRecordId.New(),
            TenantId = tenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DeploymentFrequencyPerWeek = deploymentFrequencyPerWeek,
            LeadTimeForChangesHours = leadTimeForChangesHours,
            ChangeFailureRatePercent = changeFailureRatePercent,
            MeanTimeToRestoreHours = meanTimeToRestoreHours,
            MaturityScore = maturityScore,
            ServiceCount = serviceCount,
            CostPerRequestUsd = costPerRequestUsd,
            IsAnonymizedForBenchmarks = false
        };
    }

    /// <summary>Marca o snapshot como elegível para inclusão em benchmarks cross-tenant anonimizados.</summary>
    public void MarkAsAnonymized() => IsAnonymizedForBenchmarks = true;
}

/// <summary>Identificador fortemente tipado de BenchmarkSnapshotRecord.</summary>
public sealed record BenchmarkSnapshotRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BenchmarkSnapshotRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BenchmarkSnapshotRecordId From(Guid id) => new(id);
}
