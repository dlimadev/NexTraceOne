using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListRoutingStrategies;

/// <summary>
/// Feature: ListRoutingStrategies — lista estratégias de roteamento de IA configuradas.
/// Permite visibilidade administrativa sobre regras de seleção de modelo e caminho.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListRoutingStrategies
{
    /// <summary>Query de listagem de estratégias de roteamento.</summary>
    public sealed record Query(bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista estratégias de roteamento com filtros opcionais.</summary>
    public sealed class Handler(
        IAiRoutingStrategyRepository strategyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var strategies = await strategyRepository.ListAsync(
                request.IsActive,
                cancellationToken);

            var items = strategies
                .Select(s => new RoutingStrategyItem(
                    s.Id.Value,
                    s.Name,
                    s.Description,
                    s.TargetPersona,
                    s.TargetUseCase,
                    s.TargetClientType,
                    s.PreferredPath.ToString(),
                    s.MaxSensitivityLevel,
                    s.AllowExternalEscalation,
                    s.IsActive,
                    s.Priority,
                    s.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de estratégias de roteamento.</summary>
    public sealed record Response(
        IReadOnlyList<RoutingStrategyItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma estratégia de roteamento.</summary>
    public sealed record RoutingStrategyItem(
        Guid StrategyId,
        string Name,
        string Description,
        string TargetPersona,
        string TargetUseCase,
        string TargetClientType,
        string PreferredPath,
        int MaxSensitivityLevel,
        bool AllowExternalEscalation,
        bool IsActive,
        int Priority,
        DateTimeOffset CreatedAt);
}
