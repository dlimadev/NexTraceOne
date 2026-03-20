using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Graph.Entities;

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
        IContractVersionRepository repository,
        IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var apiAsset = await apiAssetRepository.GetByIdAsync(ApiAssetId.From(version.ApiAssetId), cancellationToken);
            var service = apiAsset?.OwnerService;

            var violations = version.RuleViolations
                .Select(v => new RuleViolationDto(v.RuleName, v.Severity, v.Message, v.Path, v.SuggestedFix))
                .ToList();

            var artifacts = version.Artifacts
                .Select(a => new ArtifactDto(a.Id.Value, a.ArtifactType, a.Name, a.ContentFormat, a.IsAiGenerated, a.GeneratedAt))
                .ToList();

            var consumers = apiAsset?.ConsumerRelationships
                .Select(c => new ConsumerDto(
                    c.Id.Value,
                    c.ConsumerName,
                    c.SourceType,
                    string.Empty,
                    string.Empty,
                    c.ConfidenceScore,
                    c.LastObservedAt))
                .ToList() ?? [];

            var discoverySources = apiAsset?.DiscoverySources
                .Select(source => new DiscoverySourceDto(
                    source.Id.Value,
                    source.SourceType,
                    source.ExternalReference,
                    source.DiscoveredAt,
                    source.ConfidenceScore))
                .ToList() ?? [];

            var provenance = version.Provenance is null
                ? null
                : new ProvenanceDto(
                    version.Provenance.Origin,
                    version.Provenance.OriginalFormat,
                    version.Provenance.ParserUsed,
                    version.Provenance.StandardVersion,
                    version.Provenance.ImportedBy,
                    version.Provenance.IsAiGenerated,
                    version.Provenance.AiModelVersion);

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
                provenance,
                version.DeprecationNotice,
                version.DeprecationDate,
                version.SunsetDate,
                version.CreatedAt,
                apiAsset?.Name,
                apiAsset?.RoutePattern,
                apiAsset?.Version,
                apiAsset?.Visibility,
                service?.Id.Value,
                service?.Name,
                service?.DisplayName,
                service?.Description,
                service?.ServiceType.ToString(),
                service?.Domain,
                service?.SystemArea,
                service?.TeamName,
                service?.TechnicalOwner,
                service?.BusinessOwner,
                service?.Criticality.ToString(),
                service?.ExposureType.ToString(),
                service?.DocumentationUrl,
                service?.RepositoryUrl,
                consumers,
                discoverySources,
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

    /// <summary>DTO de proveniência.</summary>
    public sealed record ProvenanceDto(
        string Origin,
        string OriginalFormat,
        string ParserUsed,
        string StandardVersion,
        string ImportedBy,
        bool IsAiGenerated,
        string? AiModelVersion);

    /// <summary>DTO de consumidor.</summary>
    public sealed record ConsumerDto(
        Guid Id,
        string Name,
        string Kind,
        string Environment,
        string ExternalReference,
        decimal ConfidenceScore,
        DateTimeOffset LastObservedAt);

    /// <summary>DTO de fonte de descoberta.</summary>
    public sealed record DiscoverySourceDto(
        Guid Id,
        string SourceType,
        string ExternalReference,
        DateTimeOffset DiscoveredAt,
        decimal ConfidenceScore);

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
        string? Algorithm,
        string? SignedBy,
        DateTimeOffset? SignedAt,
        ProvenanceDto? Provenance,
        string? DeprecationNotice,
        DateTimeOffset? DeprecationDate,
        DateTimeOffset? SunsetDate,
        DateTimeOffset CreatedAt,
        string? ApiName,
        string? RoutePattern,
        string? ApiVersion,
        string? Visibility,
        Guid? ServiceAssetId,
        string? ServiceName,
        string? ServiceDisplayName,
        string? ServiceDescription,
        string? ServiceType,
        string? Domain,
        string? SystemArea,
        string? TeamName,
        string? TechnicalOwner,
        string? BusinessOwner,
        string? Criticality,
        string? ExposureType,
        string? DocumentationUrl,
        string? RepositoryUrl,
        IReadOnlyList<ConsumerDto> Consumers,
        IReadOnlyList<DiscoverySourceDto> DiscoverySources,
        IReadOnlyList<RuleViolationDto> RuleViolations,
        IReadOnlyList<ArtifactDto> Artifacts);
}
