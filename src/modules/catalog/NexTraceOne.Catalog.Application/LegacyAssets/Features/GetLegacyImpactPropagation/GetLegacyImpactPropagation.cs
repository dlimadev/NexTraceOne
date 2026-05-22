using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyImpactPropagation;

/// <summary>
/// Feature: GetLegacyImpactPropagation — propagação de impacto no grafo de ativos legacy.
///
/// Para um ativo legacy (COBOL, CICS, IMS, DB2 etc.), percorre o grafo de dependências
/// e calcula quais ativos são afectados quando o ativo origem é alterado ou retirado.
///
/// Algoritmo BFS em largura, com profundidade máxima configurável (default 3).
/// Identifica o conjunto de impacto directo (profundidade 1) e transitivo (≥ 2).
/// </summary>
public static class GetLegacyImpactPropagation
{
    private const int DefaultMaxDepth = 3;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        Guid AssetId,
        MainframeAssetType AssetType,
        int MaxDepth = DefaultMaxDepth) : IQuery<PropagationResult>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AssetId).NotEmpty();
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 5);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record AffectedAsset(
        Guid AssetId,
        MainframeAssetType AssetType,
        string DependencyType,
        int Depth,
        Guid FromAssetId);

    public sealed record PropagationResult(
        Guid RootAssetId,
        MainframeAssetType RootAssetType,
        int MaxDepth,
        int TotalAffected,
        int DirectlyAffected,
        int TransitivelyAffected,
        IReadOnlyList<AffectedAsset> AffectedAssets);

    // ── Handler ────────────────────────────────────────────────────────────
    internal sealed class Handler(
        ILegacyDependencyRepository dependencyRepository) : IQueryHandler<Query, PropagationResult>
    {
        public async Task<Result<PropagationResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Default(request.AssetId);

            var visited = new HashSet<Guid>();
            var affected = new List<AffectedAsset>();

            // BFS: encontra quem depende deste ativo (impacto upstream)
            var queue = new Queue<(Guid id, int depth)>();
            queue.Enqueue((request.AssetId, 0));
            visited.Add(request.AssetId);

            while (queue.Count > 0)
            {
                var (currentId, depth) = queue.Dequeue();
                if (depth >= request.MaxDepth)
                    continue;

                var dependents = await dependencyRepository.ListByTargetAsync(currentId, cancellationToken);

                foreach (var dep in dependents)
                {
                    if (visited.Contains(dep.SourceAssetId))
                        continue;

                    visited.Add(dep.SourceAssetId);
                    affected.Add(new AffectedAsset(
                        dep.SourceAssetId,
                        dep.SourceAssetType,
                        dep.DependencyType,
                        depth + 1,
                        currentId));

                    queue.Enqueue((dep.SourceAssetId, depth + 1));
                }
            }

            var direct = affected.Count(a => a.Depth == 1);
            var transitive = affected.Count - direct;

            return Result<PropagationResult>.Success(new PropagationResult(
                RootAssetId: request.AssetId,
                RootAssetType: request.AssetType,
                MaxDepth: request.MaxDepth,
                TotalAffected: affected.Count,
                DirectlyAffected: direct,
                TransitivelyAffected: transitive,
                AffectedAssets: affected));
        }
    }
}
