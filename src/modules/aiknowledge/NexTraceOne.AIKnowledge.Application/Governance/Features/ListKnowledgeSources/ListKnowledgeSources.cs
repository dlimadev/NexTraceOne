using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSources;

/// <summary>
/// Feature: ListKnowledgeSources — lista fontes de conhecimento para grounding da IA.
/// Permite filtrar por tipo de fonte e estado de ativação.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListKnowledgeSources
{
    /// <summary>Query de listagem filtrada de fontes de conhecimento de IA.</summary>
    public sealed record Query(
        KnowledgeSourceType? SourceType,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista fontes de conhecimento com filtros opcionais.</summary>
    public sealed class Handler(
        IAiKnowledgeSourceRepository knowledgeSourceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sources = await knowledgeSourceRepository.ListAsync(
                request.SourceType,
                request.IsActive,
                cancellationToken);

            var items = sources
                .Select(s => new KnowledgeSourceItem(
                    s.Id.Value,
                    s.Name,
                    s.Description,
                    s.SourceType.ToString(),
                    s.EndpointOrPath,
                    s.IsActive,
                    s.Priority,
                    s.RegisteredAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de fontes de conhecimento de IA.</summary>
    public sealed record Response(
        IReadOnlyList<KnowledgeSourceItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma fonte de conhecimento na listagem.</summary>
    public sealed record KnowledgeSourceItem(
        Guid SourceId,
        string Name,
        string Description,
        string SourceType,
        string EndpointOrPath,
        bool IsActive,
        int Priority,
        DateTimeOffset RegisteredAt);
}
