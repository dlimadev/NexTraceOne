using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetLatestTopologySnapshot;

/// <summary>
/// Feature: GetLatestTopologySnapshot — retorna o snapshot mais recente do grafo de topologia,
/// enriquecido com estatísticas de nós e arestas. Componente central do Digital Twin (Wave D.1).
///
/// O snapshot é a materialização do grafo num ponto no tempo — permite ao Digital Twin
/// apresentar uma "fotografia" auditável do sistema e comparar com estados anteriores.
/// Usado para navegação temporal e análise de drift.
/// </summary>
public static class GetLatestTopologySnapshot
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(IGraphSnapshotRepository snapshotRepository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
            if (snapshot is null)
                return CatalogGraphErrors.GraphSnapshotNotFound(Guid.Empty);

            return Result<Response>.Success(new Response(
                SnapshotId: snapshot.Id.Value,
                CapturedAt: snapshot.CapturedAt,
                NodeCount: snapshot.NodeCount,
                EdgeCount: snapshot.EdgeCount,
                NodesJson: snapshot.NodesJson,
                EdgesJson: snapshot.EdgesJson));
        }
    }

    public sealed record Response(
        Guid SnapshotId,
        DateTimeOffset CapturedAt,
        int NodeCount,
        int EdgeCount,
        string NodesJson,
        string EdgesJson);
}
