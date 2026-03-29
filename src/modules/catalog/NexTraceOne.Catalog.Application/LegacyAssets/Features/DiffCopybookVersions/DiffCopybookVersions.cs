using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;
using NexTraceOne.Catalog.Domain.LegacyAssets.Services;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.DiffCopybookVersions;

/// <summary>
/// Feature: DiffCopybookVersions — computa diff semântico entre duas versões de copybook.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DiffCopybookVersions
{
    /// <summary>Query de diff semântico entre duas versões de copybook.</summary>
    public sealed record Query(
        Guid CopybookId,
        Guid BaseVersionId,
        Guid TargetVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de diff.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CopybookId).NotEmpty();
            RuleFor(x => x.BaseVersionId).NotEmpty();
            RuleFor(x => x.TargetVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que computa o diff semântico entre duas versões de copybook.</summary>
    public sealed class Handler(
        ICopybookVersionRepository copybookVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var baseVersionId = CopybookVersionId.From(request.BaseVersionId);
            var targetVersionId = CopybookVersionId.From(request.TargetVersionId);

            var baseVersion = await copybookVersionRepository.GetByIdAsync(baseVersionId, cancellationToken);
            if (baseVersion is null)
                return LegacyAssetsErrors.CopybookVersionNotFound(request.BaseVersionId);

            var targetVersion = await copybookVersionRepository.GetByIdAsync(targetVersionId, cancellationToken);
            if (targetVersion is null)
                return LegacyAssetsErrors.CopybookVersionNotFound(request.TargetVersionId);

            var baseLayout = CopybookParser.Parse(baseVersion.RawContent);
            var targetLayout = CopybookParser.Parse(targetVersion.RawContent);
            var diff = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

            return new Response(
                request.CopybookId,
                baseVersion.VersionLabel,
                targetVersion.VersionLabel,
                diff.ChangeLevel,
                diff.BreakingChanges.Count > 0,
                diff.BreakingChanges.ToList(),
                diff.AdditiveChanges.ToList(),
                diff.NonBreakingChanges.ToList());
        }
    }

    /// <summary>Resposta do diff semântico entre duas versões de copybook.</summary>
    public sealed record Response(
        Guid CopybookId,
        string BaseVersion,
        string TargetVersion,
        ChangeLevel ChangeLevel,
        bool HasBreakingChanges,
        List<ChangeEntry> BreakingChanges,
        List<ChangeEntry> AdditiveChanges,
        List<ChangeEntry> NonBreakingChanges);
}
