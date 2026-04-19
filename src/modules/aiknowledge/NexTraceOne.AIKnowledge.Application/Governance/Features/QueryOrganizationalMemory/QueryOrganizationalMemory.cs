using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.QueryOrganizationalMemory;

/// <summary>
/// Feature: QueryOrganizationalMemory — pesquisa nós de memória organizacional por subject.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class QueryOrganizationalMemory
{
    public sealed record Query(string Subject, Guid TenantId, int Limit = 10) : IQuery<Response>;

    public sealed class Handler(IOrganizationalMemoryRepository memoryRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var nodes = await memoryRepository.SearchAsync(request.Subject, request.TenantId, request.Limit, ct);

            var items = nodes
                .OrderByDescending(n => n.RecordedAt)
                .Select(n => new MemoryNodeSummary(
                    n.Id.Value,
                    n.NodeType,
                    n.Title,
                    n.Content,
                    n.Subject,
                    n.RecordedAt,
                    n.RelevanceScore,
                    n.Tags.ToArray()))
                .ToList()
                .AsReadOnly();

            return new Response(items, items.Count);
        }
    }

    public sealed record MemoryNodeSummary(
        Guid NodeId,
        string NodeType,
        string Title,
        string Content,
        string Subject,
        DateTimeOffset RecordedAt,
        double RelevanceScore,
        string[] Tags);

    public sealed record Response(IReadOnlyList<MemoryNodeSummary> Nodes, int TotalFound);
}
