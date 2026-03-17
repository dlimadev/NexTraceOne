using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListBudgets;

/// <summary>
/// Feature: ListBudgets — lista budgets/quotas de tokens de IA com filtros opcionais.
/// Permite visualizar limites e consumo atual por escopo e estado.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListBudgets
{
    /// <summary>Query de listagem filtrada de budgets de IA.</summary>
    public sealed record Query(
        string? Scope,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista budgets com filtros opcionais.</summary>
    public sealed class Handler(
        IAiBudgetRepository budgetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var budgets = await budgetRepository.ListAsync(
                request.Scope,
                request.IsActive,
                cancellationToken);

            var items = budgets
                .Select(b => new BudgetItem(
                    b.Id.Value,
                    b.Name,
                    b.Scope,
                    b.ScopeValue,
                    b.Period.ToString(),
                    b.MaxTokens,
                    b.MaxRequests,
                    b.CurrentTokensUsed,
                    b.CurrentRequestCount,
                    b.IsActive,
                    b.IsQuotaExceeded))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de budgets de IA.</summary>
    public sealed record Response(
        IReadOnlyList<BudgetItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um budget na listagem de governança.</summary>
    public sealed record BudgetItem(
        Guid BudgetId,
        string Name,
        string Scope,
        string ScopeValue,
        string Period,
        long MaxTokens,
        int MaxRequests,
        long CurrentTokensUsed,
        int CurrentRequestCount,
        bool IsActive,
        bool IsQuotaExceeded);
}
