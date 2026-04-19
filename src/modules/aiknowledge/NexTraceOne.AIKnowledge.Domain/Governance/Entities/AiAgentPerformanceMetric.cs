using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Métricas de performance de um agente, calculadas periodicamente.
/// Usadas no dashboard de performance e para o Agent Lightning.
///
/// As métricas são calculadas num janela de 30 dias (PeriodStart → PeriodEnd)
/// e podem ser actualizadas para reflectir ciclos RL completados.
/// </summary>
public sealed class AiAgentPerformanceMetric : AuditableEntity<AiAgentPerformanceMetricId>
{
    private AiAgentPerformanceMetric() { }

    /// <summary>Agent ao qual estas métricas se referem.</summary>
    public AiAgentId AgentId { get; private set; } = null!;

    /// <summary>Nome do agent no momento do cálculo.</summary>
    public string AgentName { get; private set; } = string.Empty;

    /// <summary>Início do período de 30 dias.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de 30 dias.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Total de execuções no período.</summary>
    public long TotalExecutions { get; private set; }

    /// <summary>Execuções com feedback registado no período.</summary>
    public long ExecutionsWithFeedback { get; private set; }

    /// <summary>Rating médio (1-5) calculado sobre as execuções com feedback.</summary>
    public double AverageRating { get; private set; }

    /// <summary>Percentagem de outcomes correctos (0.0 a 1.0).</summary>
    public double AccuracyRate { get; private set; }

    /// <summary>Número de ciclos de Reinforcement Learning completados para este agent.</summary>
    public int RlCyclesCompleted { get; private set; }

    /// <summary>Total de trajectórias exportadas para o trainer externo.</summary>
    public long TrajectoriesExported { get; private set; }

    /// <summary>Tenant ao qual as métricas pertencem.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Cria um novo registo de métricas de performance para um agent num período.
    /// </summary>
    public static AiAgentPerformanceMetric Create(
        AiAgentId agentId,
        string agentName,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        long totalExecutions,
        long executionsWithFeedback,
        double averageRating,
        double accuracyRate,
        Guid tenantId)
    {
        Guard.Against.Null(agentId);
        Guard.Against.NullOrWhiteSpace(agentName);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        return new AiAgentPerformanceMetric
        {
            Id = AiAgentPerformanceMetricId.New(),
            AgentId = agentId,
            AgentName = agentName,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalExecutions = totalExecutions,
            ExecutionsWithFeedback = executionsWithFeedback,
            AverageRating = averageRating,
            AccuracyRate = accuracyRate,
            RlCyclesCompleted = 0,
            TrajectoriesExported = 0,
            TenantId = tenantId,
        };
    }

    /// <summary>Actualiza o número de ciclos RL completados.</summary>
    public void UpdateRlCycles(int cycles)
    {
        Guard.Against.Negative(cycles, nameof(cycles));
        RlCyclesCompleted = cycles;
    }

    /// <summary>Incrementa o contador de trajectórias exportadas.</summary>
    public void IncrementTrajectoriesExported(long count)
    {
        Guard.Against.Negative(count, nameof(count));
        TrajectoriesExported += count;
    }
}

/// <summary>Identificador fortemente tipado de AiAgentPerformanceMetric.</summary>
public sealed record AiAgentPerformanceMetricId(Guid Value) : TypedIdBase(Value)
{
    public static AiAgentPerformanceMetricId New() => new(Guid.NewGuid());
    public static AiAgentPerformanceMetricId From(Guid id) => new(id);
}
