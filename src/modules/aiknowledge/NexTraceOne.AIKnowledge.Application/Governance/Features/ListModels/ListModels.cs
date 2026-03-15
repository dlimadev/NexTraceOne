using Ardalis.GuardClauses;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListModels;

/// <summary>
/// Feature: ListModels — lista modelos do Model Registry com filtros opcionais.
/// Ponto de entrada para consulta do catálogo de modelos de IA registados.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListModels
{
    /// <summary>Query de listagem filtrada de modelos de IA do Model Registry.</summary>
    public sealed record Query(
        string? Provider,
        ModelType? ModelType,
        ModelStatus? Status,
        bool? IsInternal) : IQuery<Response>;

    /// <summary>Handler que lista modelos com filtros opcionais.</summary>
    public sealed class Handler(
        IAiModelRepository modelRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var models = await modelRepository.ListAsync(
                request.Provider,
                request.ModelType,
                request.Status,
                request.IsInternal,
                cancellationToken);

            var items = models
                .Select(m => new ModelItem(
                    m.Id.Value,
                    m.Name,
                    m.DisplayName,
                    m.Provider,
                    m.ModelType.ToString(),
                    m.IsInternal,
                    m.IsExternal,
                    m.Status.ToString(),
                    m.Capabilities,
                    m.SensitivityLevel))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de modelos do Model Registry.</summary>
    public sealed record Response(
        IReadOnlyList<ModelItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um modelo na listagem do Model Registry.</summary>
    public sealed record ModelItem(
        Guid ModelId,
        string Name,
        string DisplayName,
        string Provider,
        string ModelType,
        bool IsInternal,
        bool IsExternal,
        string Status,
        string Capabilities,
        int SensitivityLevel);
}
