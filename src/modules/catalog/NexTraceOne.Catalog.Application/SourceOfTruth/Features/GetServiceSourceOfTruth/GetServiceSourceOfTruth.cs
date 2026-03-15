using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.Contracts.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceSourceOfTruth;

/// <summary>
/// Feature: GetServiceSourceOfTruth — obtém a visão consolidada de Source of Truth
/// para um serviço, combinando identidade, ownership, contratos associados,
/// referências documentais, indicadores de cobertura e metadados operacionais.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceSourceOfTruth
{
    /// <summary>Query de Source of Truth consolidado de um serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que compõe a visão consolidada de Source of Truth de um serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IApiAssetRepository apiRepository,
        IContractVersionRepository contractRepository,
        ILinkedReferenceRepository referenceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return Error.NotFound("SourceOfTruth.ServiceNotFound", "Service '{0}' not found", request.ServiceId);

            // Obter APIs do serviço
            var apis = await apiRepository.ListByServiceIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            // Obter contratos mais recentes por API
            var apiAssetIds = apis.Select(a => a.Id.Value).ToList();
            var contracts = apiAssetIds.Count > 0
                ? await contractRepository.ListByApiAssetIdsAsync(apiAssetIds, cancellationToken)
                : [];

            // Obter referências vinculadas ao serviço
            var references = await referenceRepository.ListByAssetAsync(
                request.ServiceId, LinkedAssetType.Service, cancellationToken);

            // Compor indicadores de cobertura
            var coverage = new CoverageIndicators(
                HasOwner: !string.IsNullOrWhiteSpace(service.TeamName),
                HasContracts: contracts.Count > 0,
                HasDocumentation: references.Any(r => r.ReferenceType == LinkedReferenceType.Documentation) ||
                                  !string.IsNullOrWhiteSpace(service.DocumentationUrl),
                HasRunbook: references.Any(r => r.ReferenceType == LinkedReferenceType.Runbook),
                HasRecentChangeHistory: references.Any(r => r.ReferenceType == LinkedReferenceType.Changelog),
                HasDependenciesMapped: apis.Count > 0,
                HasEventTopics: references.Any(r => r.ReferenceType == LinkedReferenceType.EventTopic));

            // Compor resumo de contratos
            var contractSummaries = contracts.Select(c => new ContractSummaryItem(
                c.Id.Value, c.ApiAssetId, c.SemVer, c.Protocol.ToString(),
                c.LifecycleState.ToString(), c.IsLocked, c.CreatedAt)).ToList();

            // Compor resumo de APIs
            var apiSummaries = apis.Select(a => new ApiSummaryItem(
                a.Id.Value, a.Name, a.RoutePattern, a.Version, a.Visibility,
                a.ConsumerRelationships.Count)).ToList();

            // Compor referências agrupadas
            var referenceSummaries = references.Where(r => r.IsActive).Select(r =>
                new ReferenceSummaryItem(r.Id.Value, r.ReferenceType.ToString(),
                    r.Title, r.Description, r.Url)).ToList();

            return new Response(
                ServiceId: service.Id.Value,
                Name: service.Name,
                DisplayName: service.DisplayName,
                Description: service.Description,
                ServiceType: service.ServiceType.ToString(),
                Domain: service.Domain,
                SystemArea: service.SystemArea,
                TeamName: service.TeamName,
                TechnicalOwner: service.TechnicalOwner,
                BusinessOwner: service.BusinessOwner,
                Criticality: service.Criticality.ToString(),
                LifecycleStatus: service.LifecycleStatus.ToString(),
                ExposureType: service.ExposureType.ToString(),
                DocumentationUrl: service.DocumentationUrl,
                RepositoryUrl: service.RepositoryUrl,
                Apis: apiSummaries,
                Contracts: contractSummaries,
                References: referenceSummaries,
                Coverage: coverage,
                TotalApis: apis.Count,
                TotalContracts: contracts.Count,
                TotalReferences: references.Count(r => r.IsActive));
        }
    }

    /// <summary>Item resumido de API associada ao serviço.</summary>
    public sealed record ApiSummaryItem(
        Guid ApiAssetId, string Name, string RoutePattern, string Version,
        string Visibility, int ConsumerCount);

    /// <summary>Item resumido de contrato associado ao serviço.</summary>
    public sealed record ContractSummaryItem(
        Guid VersionId, Guid ApiAssetId, string SemVer, string Protocol,
        string LifecycleState, bool IsLocked, DateTimeOffset CreatedAt);

    /// <summary>Referência vinculada ao serviço.</summary>
    public sealed record ReferenceSummaryItem(
        Guid ReferenceId, string ReferenceType, string Title,
        string Description, string? Url);

    /// <summary>Indicadores de cobertura/completude do serviço.</summary>
    public sealed record CoverageIndicators(
        bool HasOwner, bool HasContracts, bool HasDocumentation,
        bool HasRunbook, bool HasRecentChangeHistory,
        bool HasDependenciesMapped, bool HasEventTopics);

    /// <summary>Resposta consolidada de Source of Truth do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string SystemArea,
        string TeamName,
        string TechnicalOwner,
        string BusinessOwner,
        string Criticality,
        string LifecycleStatus,
        string ExposureType,
        string? DocumentationUrl,
        string? RepositoryUrl,
        IReadOnlyList<ApiSummaryItem> Apis,
        IReadOnlyList<ContractSummaryItem> Contracts,
        IReadOnlyList<ReferenceSummaryItem> References,
        CoverageIndicators Coverage,
        int TotalApis,
        int TotalContracts,
        int TotalReferences);
}
