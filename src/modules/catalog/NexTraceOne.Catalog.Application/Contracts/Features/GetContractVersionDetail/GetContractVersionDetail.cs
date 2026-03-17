using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractVersionDetail;

/// <summary>
/// Feature: GetContractVersionDetail — retorna detalhes completos de uma versão de contrato,
/// incluindo protocolo, lifecycle state, assinatura, proveniência, rule violations e artefatos.
/// Estrutura VSA: Query + Validator + Handler + Response.
/// </summary>
public static class GetContractVersionDetail
{
    /// <summary>Query para obter detalhes completos de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de versão.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que carrega e retorna detalhes completos da versão de contrato.</summary>
    public sealed class Handler(
        IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var violations = version.RuleViolations
                .Select(v => new RuleViolationDto(v.RuleName, v.Severity, v.Message, v.Path, v.SuggestedFix))
                .ToList();

            var artifacts = version.Artifacts
                .Select(a => new ArtifactDto(a.Id.Value, a.ArtifactType, a.Name, a.ContentFormat, a.IsAiGenerated, a.GeneratedAt))
                .ToList();

            return new Response(
                version.Id.Value,
                version.ApiAssetId,
                version.SemVer,
                version.SpecContent,
                version.Protocol,
                version.LifecycleState,
                version.Format,
                version.ImportedFrom,
                version.IsLocked,
                version.LockedAt,
                version.LockedBy,
                version.Signature?.Fingerprint,
                version.Signature?.Algorithm,
                version.Signature?.SignedBy,
                version.Signature?.SignedAt,
                version.Provenance?.Origin,
                version.Provenance?.IsAiGenerated ?? false,
                version.DeprecationNotice,
                version.DeprecationDate,
                version.SunsetDate,
                version.CreatedAt,
                violations,
                artifacts);
        }
    }

    /// <summary>DTO de violação de ruleset.</summary>
    public sealed record RuleViolationDto(
        string RuleName,
        string Severity,
        string Message,
        string Path,
        string? SuggestedFix);

    /// <summary>DTO de artefato gerado.</summary>
    public sealed record ArtifactDto(
        Guid Id,
        ContractArtifactType ArtifactType,
        string Name,
        string ContentFormat,
        bool IsAiGenerated,
        DateTimeOffset GeneratedAt);

    /// <summary>Resposta com detalhes completos de uma versão de contrato.</summary>
    public sealed record Response(
        Guid Id,
        Guid ApiAssetId,
        string SemVer,
        string SpecContent,
        ContractProtocol Protocol,
        ContractLifecycleState LifecycleState,
        string Format,
        string ImportedFrom,
        bool IsLocked,
        DateTimeOffset? LockedAt,
        string? LockedBy,
        string? Fingerprint,
        string? SignatureAlgorithm,
        string? SignedBy,
        DateTimeOffset? SignedAt,
        string? ProvenanceOrigin,
        bool IsAiGenerated,
        string? DeprecationNotice,
        DateTimeOffset? DeprecationDate,
        DateTimeOffset? SunsetDate,
        DateTimeOffset CreatedAt,
        IReadOnlyList<RuleViolationDto> RuleViolations,
        IReadOnlyList<ArtifactDto> Artifacts);
}
