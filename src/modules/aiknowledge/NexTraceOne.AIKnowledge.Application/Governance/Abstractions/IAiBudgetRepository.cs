using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de budgets/quotas de tokens de IA.
/// Suporta listagem filtrada por escopo e estado de ativação.
/// </summary>
public interface IAiBudgetRepository
{
    /// <summary>Lista budgets com filtros opcionais de escopo e estado ativo.</summary>
    Task<IReadOnlyList<AIBudget>> ListAsync(
        string? scope,
        bool? isActive,
        CancellationToken ct);

    /// <summary>Obtém um budget pelo identificador fortemente tipado.</summary>
    Task<AIBudget?> GetByIdAsync(AIBudgetId id, CancellationToken ct);

    /// <summary>Atualiza um budget existente.</summary>
    Task UpdateAsync(AIBudget budget, CancellationToken ct);
}
