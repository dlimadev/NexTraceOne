using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetMemoryNodeDetails;

/// <summary>
/// Feature: GetMemoryNodeDetails — obtém detalhes completos de um nó de memória com linked nodes.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetMemoryNodeDetails
{
    public sealed record Query(Guid NodeId) : IQuery<Response>;

    public sealed class Handler(IOrganizationalMemoryRepository memoryRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var node = await memoryRepository.GetByIdAsync(
                OrganizationalMemoryNodeId.From(request.NodeId), ct);

            if (node is null)
                return AiGovernanceErrors.MemoryNodeNotFound(request.NodeId.ToString());

            var linkedNodes = await memoryRepository.GetLinkedNodesAsync(node.Id, ct);

            var linkedRefs = linkedNodes.Select(ln => new LinkedNodeRef(
                ln.Id.Value, ln.NodeType, ln.Title, ln.Subject, ln.RecordedAt
            )).ToList().AsReadOnly();

            return new Response(
                node.Id.Value,
                node.NodeType,
                node.Subject,
                node.Title,
                node.Content,
                node.Context,
                node.ActorId,
                node.Tags.ToArray(),
                node.SourceType,
                node.SourceId,
                linkedRefs,
                node.RecordedAt);
        }
    }

    public sealed record LinkedNodeRef(Guid NodeId, string NodeType, string Title, string Subject, DateTimeOffset RecordedAt);

    public sealed record Response(
        Guid NodeId,
        string NodeType,
        string Subject,
        string Title,
        string Content,
        string Context,
        string ActorId,
        string[] Tags,
        string SourceType,
        string? SourceId,
        IReadOnlyList<LinkedNodeRef> LinkedNodes,
        DateTimeOffset RecordedAt);
}
