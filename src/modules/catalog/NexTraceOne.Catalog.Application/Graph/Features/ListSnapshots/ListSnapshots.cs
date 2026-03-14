using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;

namespace NexTraceOne.EngineeringGraph.Application.Features.ListSnapshots;

/// <summary>
/// Feature: ListSnapshots — lista os snapshots temporais do grafo, ordenados do mais recente ao mais antigo.
/// Permite ao frontend exibir um seletor de snapshots para diff temporal e histórico de evolução.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class ListSnapshots
{
    /// <summary>Query para listar snapshots temporais do grafo.</summary>
    public sealed record Query(int Limit = 50) : IQuery<Response>;

    /// <summary>
    /// Handler que lista snapshots do grafo ordenados cronologicamente (mais recente primeiro).
    /// Retorna metadados resumidos sem os payloads JSON completos para performance.
    /// </summary>
    public sealed class Handler(IGraphSnapshotRepository snapshotRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshots = await snapshotRepository.ListAsync(request.Limit, cancellationToken);

            var items = snapshots.Select(s => new SnapshotSummary(
                s.Id.Value,
                s.Label,
                s.CapturedAt,
                s.NodeCount,
                s.EdgeCount,
                s.CreatedBy)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta com a lista de snapshots temporais.</summary>
    public sealed record Response(IReadOnlyList<SnapshotSummary> Items);

    /// <summary>Resumo de um snapshot temporal do grafo.</summary>
    public sealed record SnapshotSummary(
        Guid SnapshotId,
        string Label,
        DateTimeOffset CapturedAt,
        int NodeCount,
        int EdgeCount,
        string CreatedBy);
}
