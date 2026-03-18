using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiSources;

/// <summary>
/// Feature: ListAiSources — lista todas as fontes de dados de IA ativas e registadas.
/// Utiliza o IAiSourceRegistryService para obter fontes habilitadas.
/// </summary>
public static class ListAiSources
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(
        IAiSourceRegistryService sourceRegistryService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sources = await sourceRegistryService.GetEnabledSourcesAsync(cancellationToken);

            var items = sources.Select(s => new SourceItem(
                s.Id,
                s.Name,
                s.DisplayName,
                s.SourceType,
                s.Description,
                s.IsEnabled,
                s.Classification,
                s.OwnerTeam,
                s.HealthStatus)).ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record Response(
        IReadOnlyList<SourceItem> Items,
        int TotalCount);

    public sealed record SourceItem(
        Guid Id,
        string Name,
        string DisplayName,
        string SourceType,
        string Description,
        bool IsEnabled,
        string Classification,
        string OwnerTeam,
        string HealthStatus);
}
