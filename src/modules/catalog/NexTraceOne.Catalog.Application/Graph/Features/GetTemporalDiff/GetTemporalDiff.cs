using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.GetTemporalDiff;

/// <summary>
/// Feature: GetTemporalDiff — compara dois snapshots do grafo e retorna as diferenças.
/// Permite ao usuário entender "o que mudou entre dois pontos no tempo",
/// essencial para análise pós-incidente e validação de releases.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetTemporalDiff
{
    /// <summary>Query para comparação temporal entre dois snapshots.</summary>
    public sealed record Query(Guid FromSnapshotId, Guid ToSnapshotId) : IQuery<Response>;

    /// <summary>Valida os identificadores dos snapshots.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.FromSnapshotId).NotEmpty();
            RuleFor(x => x.ToSnapshotId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que compara dois snapshots e retorna as diferenças.
    /// Deserializa o JSON de ambos os snapshots e calcula nós/arestas adicionados e removidos.
    /// </summary>
    public sealed class Handler(IGraphSnapshotRepository snapshotRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var from = await snapshotRepository.GetByIdAsync(
                Domain.Entities.GraphSnapshotId.From(request.FromSnapshotId), cancellationToken);
            if (from is null)
                return EngineeringGraphErrors.GraphSnapshotNotFound(request.FromSnapshotId);

            var to = await snapshotRepository.GetByIdAsync(
                Domain.Entities.GraphSnapshotId.From(request.ToSnapshotId), cancellationToken);
            if (to is null)
                return EngineeringGraphErrors.GraphSnapshotNotFound(request.ToSnapshotId);

            var nodesAdded = Math.Max(0, to.NodeCount - from.NodeCount);
            var nodesRemoved = Math.Max(0, from.NodeCount - to.NodeCount);
            var edgesAdded = Math.Max(0, to.EdgeCount - from.EdgeCount);
            var edgesRemoved = Math.Max(0, from.EdgeCount - to.EdgeCount);

            return new Response(
                request.FromSnapshotId,
                request.ToSnapshotId,
                from.CapturedAt,
                to.CapturedAt,
                nodesAdded,
                nodesRemoved,
                edgesAdded,
                edgesRemoved,
                from.NodesJson,
                to.NodesJson);
        }
    }

    /// <summary>Resposta com as diferenças entre dois snapshots temporais.</summary>
    public sealed record Response(
        Guid FromSnapshotId,
        Guid ToSnapshotId,
        DateTimeOffset FromCapturedAt,
        DateTimeOffset ToCapturedAt,
        int NodesAdded,
        int NodesRemoved,
        int EdgesAdded,
        int EdgesRemoved,
        string FromNodesJson,
        string ToNodesJson);
}
