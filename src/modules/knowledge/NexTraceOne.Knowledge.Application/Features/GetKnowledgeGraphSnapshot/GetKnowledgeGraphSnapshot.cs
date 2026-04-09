using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeGraphSnapshot;

/// <summary>
/// Feature: GetKnowledgeGraphSnapshot — obtém um snapshot persistido do knowledge graph por ID.
/// </summary>
public static class GetKnowledgeGraphSnapshot
{
    public sealed record Query(Guid SnapshotId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SnapshotId).NotEmpty();
        }
    }

    public sealed class Handler(
        IKnowledgeGraphSnapshotRepository snapshotRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshot = await snapshotRepository.GetByIdAsync(
                KnowledgeGraphSnapshotId.From(request.SnapshotId), cancellationToken);

            if (snapshot is null)
                return Error.NotFound("KNOWLEDGE_GRAPH_SNAPSHOT_NOT_FOUND",
                    $"Knowledge graph snapshot '{request.SnapshotId}' not found.");

            return Result<Response>.Success(new Response(
                snapshot.Id.Value,
                snapshot.TotalNodes,
                snapshot.TotalEdges,
                snapshot.ConnectedComponents,
                snapshot.IsolatedNodes,
                snapshot.CoverageScore,
                snapshot.NodeTypeDistribution,
                snapshot.EdgeTypeDistribution,
                snapshot.TopConnectedEntities,
                snapshot.OrphanEntities,
                snapshot.Recommendations,
                snapshot.Status.ToString(),
                snapshot.GeneratedAt,
                snapshot.ReviewedAt,
                snapshot.ReviewComment));
        }
    }

    public sealed record Response(
        Guid SnapshotId,
        int TotalNodes,
        int TotalEdges,
        int ConnectedComponents,
        int IsolatedNodes,
        int CoverageScore,
        string NodeTypeDistribution,
        string EdgeTypeDistribution,
        string? TopConnectedEntities,
        string? OrphanEntities,
        string? Recommendations,
        string Status,
        DateTimeOffset GeneratedAt,
        DateTimeOffset? ReviewedAt,
        string? ReviewComment);
}
