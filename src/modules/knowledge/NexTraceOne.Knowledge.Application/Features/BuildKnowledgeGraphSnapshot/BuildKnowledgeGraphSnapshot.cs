using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.BuildKnowledgeGraphSnapshot;

/// <summary>
/// Feature: BuildKnowledgeGraphSnapshot — gera snapshot persistido do knowledge graph.
/// Captura métricas de nós, arestas, componentes conexos e cobertura.
/// Marca snapshots anteriores como Stale.
/// </summary>
public static class BuildKnowledgeGraphSnapshot
{
    public sealed record Command(
        int TotalNodes,
        int TotalEdges,
        int ConnectedComponents,
        int IsolatedNodes,
        int CoverageScore,
        string NodeTypeDistribution,
        string EdgeTypeDistribution,
        string? TopConnectedEntities = null,
        string? OrphanEntities = null,
        string? Recommendations = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TotalNodes).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalEdges).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ConnectedComponents).GreaterThanOrEqualTo(0);
            RuleFor(x => x.IsolatedNodes).GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.TotalNodes);
            RuleFor(x => x.CoverageScore).InclusiveBetween(0, 100);
            RuleFor(x => x.NodeTypeDistribution).NotEmpty();
            RuleFor(x => x.EdgeTypeDistribution).NotEmpty();
        }
    }

    public sealed class Handler(
        IKnowledgeGraphSnapshotRepository snapshotRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            // Mark previous snapshot as stale
            var previousSnapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
            if (previousSnapshot is not null && previousSnapshot.Status == KnowledgeGraphSnapshotStatus.Generated)
            {
                previousSnapshot.MarkAsStale();
                snapshotRepository.Update(previousSnapshot);
            }

            var snapshot = KnowledgeGraphSnapshot.Generate(
                request.TotalNodes,
                request.TotalEdges,
                request.ConnectedComponents,
                request.IsolatedNodes,
                request.CoverageScore,
                request.NodeTypeDistribution,
                request.EdgeTypeDistribution,
                request.TopConnectedEntities,
                request.OrphanEntities,
                request.Recommendations,
                currentTenant.Id,
                now);

            snapshotRepository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                snapshot.Id.Value,
                snapshot.TotalNodes,
                snapshot.TotalEdges,
                snapshot.ConnectedComponents,
                snapshot.IsolatedNodes,
                snapshot.CoverageScore,
                snapshot.Status.ToString(),
                snapshot.GeneratedAt));
        }
    }

    public sealed record Response(
        Guid SnapshotId,
        int TotalNodes,
        int TotalEdges,
        int ConnectedComponents,
        int IsolatedNodes,
        int CoverageScore,
        string Status,
        DateTimeOffset GeneratedAt);
}
