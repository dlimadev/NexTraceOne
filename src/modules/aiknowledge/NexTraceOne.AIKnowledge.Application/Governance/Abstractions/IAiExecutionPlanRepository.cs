using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de planos de execução de IA.
/// Permite persistir e consultar planos gerados durante o pipeline de execução de agents.
/// </summary>
public interface IAiExecutionPlanRepository
{
    /// <summary>Adiciona um novo plano de execução.</summary>
    Task AddAsync(AIExecutionPlan plan, CancellationToken ct);

    /// <summary>Obtém plano de execução por identificador.</summary>
    Task<AIExecutionPlan?> GetByIdAsync(AIExecutionPlanId id, CancellationToken ct);
}
