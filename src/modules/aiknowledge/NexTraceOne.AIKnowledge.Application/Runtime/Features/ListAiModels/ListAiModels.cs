using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiModels;

/// <summary>
/// Feature: ListAiModels — lista todos os modelos de IA activos no Model Registry.
/// Endpoint de runtime para consulta de modelos disponíveis para inferência.
/// </summary>
public static class ListAiModels
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(
        IAiModelRepository modelRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var models = await modelRepository.ListAsync(
                provider: null,
                modelType: null,
                status: ModelStatus.Active,
                isInternal: null,
                cancellationToken);

            var items = models.Select(m => new ModelItem(
                m.Id.Value,
                m.Name,
                m.DisplayName,
                m.Provider,
                m.ModelType.ToString(),
                m.Capabilities,
                m.IsInternal,
                m.Status.ToString())).ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record Response(
        IReadOnlyList<ModelItem> Items,
        int TotalCount);

    public sealed record ModelItem(
        Guid Id,
        string Name,
        string DisplayName,
        string Provider,
        string Type,
        string Capabilities,
        bool IsInternal,
        string Status);
}
