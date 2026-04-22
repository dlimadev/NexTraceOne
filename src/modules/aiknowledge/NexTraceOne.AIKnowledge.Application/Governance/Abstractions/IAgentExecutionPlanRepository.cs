using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de planos de execução agentic com suporte a Human-in-the-Loop.
/// A implementação nula usa ConcurrentDictionary para suportar testes em memória.
/// </summary>
public interface IAgentExecutionPlanRepository
{
    /// <summary>Obtém um plano pelo identificador.</summary>
    Task<AgentExecutionPlan?> GetByIdAsync(AgentExecutionPlanId id, CancellationToken ct);

    /// <summary>Lista planos de um tenant com filtro opcional por status.</summary>
    Task<IReadOnlyList<AgentExecutionPlan>> ListByTenantAsync(
        Guid tenantId,
        PlanStatus? statusFilter,
        int pageSize,
        CancellationToken ct);

    /// <summary>Adiciona um novo plano.</summary>
    Task AddAsync(AgentExecutionPlan plan, CancellationToken ct);

    /// <summary>Actualiza um plano existente.</summary>
    Task UpdateAsync(AgentExecutionPlan plan, CancellationToken ct);
}
