using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de métricas de performance de agents de IA.
/// Suporta listagem por tenant e obtenção de métrica por agent e período.
/// </summary>
public interface IAiAgentPerformanceMetricRepository
{
    /// <summary>Lista todas as métricas de performance para um tenant.</summary>
    Task<IReadOnlyList<AiAgentPerformanceMetric>> ListByTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Obtém a métrica de um agent para um período específico (início do período).</summary>
    Task<AiAgentPerformanceMetric?> GetByAgentAndPeriodAsync(
        AiAgentId agentId,
        DateTimeOffset periodStart,
        CancellationToken ct);

    /// <summary>Adiciona uma nova métrica para persistência.</summary>
    void Add(AiAgentPerformanceMetric metric);
}
