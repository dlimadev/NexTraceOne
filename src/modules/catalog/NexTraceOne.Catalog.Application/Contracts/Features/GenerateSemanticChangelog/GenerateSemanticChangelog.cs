using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateSemanticChangelog;

/// <summary>
/// Feature: GenerateSemanticChangelog — gera changelog semântico enriquecido para um contrato.
/// Para cada par de versões consecutivas, usa os diffs semânticos já calculados e enriquece-os
/// com anotações semânticas: breaking-change, new-feature, deprecation, fix.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateSemanticChangelog
{
    /// <summary>Query para geração do changelog semântico de um contrato.</summary>
    public sealed record Query(
        Guid ApiAssetId,
        string? FromVersion = null,
        string? ToVersion = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que constrói o changelog semântico do contrato.
    /// Lista todas as versões, ordena por data de criação e para cada par consecutivo
    /// extrai as informações de diff já calculadas e gera anotações semânticas.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versions = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);

            if (versions.Count == 0)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Ordenar versões por data de criação
            var ordered = versions
                .OrderBy(v => v.CreatedAt)
                .ToList();

            // Filtrar por fromVersion/toVersion se fornecidos
            if (!string.IsNullOrEmpty(request.FromVersion))
                ordered = ordered.SkipWhile(v => v.SemVer != request.FromVersion).ToList();

            if (!string.IsNullOrEmpty(request.ToVersion))
            {
                var toIdx = ordered.FindIndex(v => v.SemVer == request.ToVersion);
                if (toIdx >= 0)
                    ordered = ordered.Take(toIdx + 1).ToList();
            }

            var entries = new List<ChangelogEntry>();

            for (var i = 1; i < ordered.Count; i++)
            {
                var from = ordered[i - 1];
                var to = ordered[i];

                var annotations = BuildAnnotations(to);
                var summary = BuildSummary(annotations, from.SemVer, to.SemVer);

                entries.Add(new ChangelogEntry(
                    from.SemVer,
                    to.SemVer,
                    to.CreatedAt,
                    annotations,
                    summary,
                    to.Diffs.Count > 0 ? to.Diffs.Max(d => d.ChangeLevel).ToString() : "Unknown"));
            }

            // Entrada mais recente primeiro
            entries.Reverse();

            return new Response(
                request.ApiAssetId,
                versions[0].ApiAssetId.ToString(),
                entries.AsReadOnly());
        }

        private static IReadOnlyList<string> BuildAnnotations(ContractVersion version)
        {
            var annotations = new List<string>();

            // Determinar nível de mudança a partir dos diffs calculados
            var maxLevel = version.Diffs.Count > 0
                ? version.Diffs.Max(d => d.ChangeLevel)
                : NexTraceOne.BuildingBlocks.Core.Enums.ChangeLevel.NonBreaking;

            if (maxLevel >= NexTraceOne.BuildingBlocks.Core.Enums.ChangeLevel.Breaking)
                annotations.Add("breaking-change");
            else if (maxLevel >= NexTraceOne.BuildingBlocks.Core.Enums.ChangeLevel.Additive)
                annotations.Add("new-feature");
            else
                annotations.Add("fix");

            if (version.LifecycleState == NexTraceOne.Catalog.Domain.Contracts.Enums.ContractLifecycleState.Deprecated
                || !string.IsNullOrWhiteSpace(version.DeprecationNotice))
                annotations.Add("deprecation");

            return annotations.AsReadOnly();
        }

        private static string BuildSummary(IReadOnlyList<string> annotations, string from, string to)
        {
            if (annotations.Contains("breaking-change"))
                return $"Breaking change from {from} to {to} — review consumers before deployment.";
            if (annotations.Contains("deprecation"))
                return $"Version {to} introduces deprecation notice.";
            if (annotations.Contains("new-feature"))
                return $"New features added in {to} — additive change, backward compatible.";
            return $"Non-breaking update from {from} to {to}.";
        }
    }

    /// <summary>Entrada de changelog semântico enriquecido.</summary>
    public sealed record ChangelogEntry(
        string FromVersion,
        string ToVersion,
        DateTimeOffset ChangeDate,
        IReadOnlyList<string> Annotations,
        string Summary,
        string DiffLevel);

    /// <summary>Resposta com o changelog semântico completo do contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string ContractIdentifier,
        IReadOnlyList<ChangelogEntry> ChangelogEntries);
}
