using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ComparePromptVersions;

/// <summary>
/// Feature: ComparePromptVersions — compara duas versões de um PromptAsset lado-a-lado.
/// Retorna conteúdo de ambas as versões com métricas simples de diferença.
/// Estrutura VSA: Query + Validator + Handler + Response.
/// </summary>
public static class ComparePromptVersions
{
    public sealed record Query(
        Guid AssetId,
        int VersionA,
        int VersionB) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AssetId).NotEmpty();
            RuleFor(x => x.VersionA).GreaterThan(0);
            RuleFor(x => x.VersionB).GreaterThan(0);
            RuleFor(x => x)
                .Must(q => q.VersionA != q.VersionB)
                .WithMessage("VersionA and VersionB must differ.");
        }
    }

    public sealed class Handler(
        IPromptAssetRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var assetId = PromptAssetId.From(request.AssetId);
            var asset = await repository.FindByIdAsync(assetId, cancellationToken);

            if (asset is null)
                return Error.Business(
                    "AiGovernance.PromptAsset.NotFound",
                    $"PromptAsset '{request.AssetId}' not found.");

            var vA = asset.Versions.FirstOrDefault(v => v.VersionNumber == request.VersionA);
            var vB = asset.Versions.FirstOrDefault(v => v.VersionNumber == request.VersionB);

            if (vA is null)
                return Error.Business(
                    "AiGovernance.PromptAsset.VersionNotFound",
                    $"Version {request.VersionA} not found for asset '{asset.Slug}'.");

            if (vB is null)
                return Error.Business(
                    "AiGovernance.PromptAsset.VersionNotFound",
                    $"Version {request.VersionB} not found for asset '{asset.Slug}'.");

            var linesA = CountLines(vA.Content);
            var linesB = CountLines(vB.Content);
            var charDelta = vB.Content.Length - vA.Content.Length;

            return new Response(
                AssetId: asset.Id.Value,
                Slug: asset.Slug,
                Name: asset.Name,
                VersionA: MapVersion(vA),
                VersionB: MapVersion(vB),
                LineCountA: linesA,
                LineCountB: linesB,
                CharDelta: charDelta,
                EvalScoreDelta: vB.EvalScore.HasValue && vA.EvalScore.HasValue
                    ? vB.EvalScore.Value - vA.EvalScore.Value
                    : null);
        }

        private static int CountLines(string content) =>
            content.Split('\n', StringSplitOptions.None).Length;

        private static VersionSummary MapVersion(PromptVersion v) =>
            new(v.VersionNumber, v.Content, v.ChangeNotes, v.EvalScore, v.CreatedBy, v.IsActive);
    }

    public sealed record VersionSummary(
        int VersionNumber,
        string Content,
        string ChangeNotes,
        decimal? EvalScore,
        string CreatedBy,
        bool IsActive);

    public sealed record Response(
        Guid AssetId,
        string Slug,
        string Name,
        VersionSummary VersionA,
        VersionSummary VersionB,
        int LineCountA,
        int LineCountB,
        int CharDelta,
        decimal? EvalScoreDelta);
}
