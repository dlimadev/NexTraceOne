using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.ListKnowledgeGraphSnapshots;

/// <summary>
/// Feature: ListKnowledgeGraphSnapshots — lista snapshots do knowledge graph, opcionalmente filtrados por status.
/// </summary>
public static class ListKnowledgeGraphSnapshots
{
    public sealed record Query(string? Status = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Status)
                .Must(s => s is null || Enum.TryParse<KnowledgeGraphSnapshotStatus>(s, true, out _))
                .WithMessage("Invalid status. Valid values: Generated, Reviewed, Stale.");
        }
    }

    public sealed class Handler(
        IKnowledgeGraphSnapshotRepository snapshotRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            KnowledgeGraphSnapshotStatus? status = null;
            if (request.Status is not null && Enum.TryParse<KnowledgeGraphSnapshotStatus>(request.Status, true, out var parsedStatus))
                status = parsedStatus;

            var snapshots = await snapshotRepository.ListAsync(status, cancellationToken);

            var items = snapshots.Select(s => new SnapshotItem(
                s.Id.Value,
                s.TotalNodes,
                s.TotalEdges,
                s.ConnectedComponents,
                s.IsolatedNodes,
                s.CoverageScore,
                s.Status.ToString(),
                s.GeneratedAt)).ToList();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    public sealed record SnapshotItem(
        Guid SnapshotId,
        int TotalNodes,
        int TotalEdges,
        int ConnectedComponents,
        int IsolatedNodes,
        int CoverageScore,
        string Status,
        DateTimeOffset GeneratedAt);

    public sealed record Response(IReadOnlyList<SnapshotItem> Items, int TotalCount);
}
