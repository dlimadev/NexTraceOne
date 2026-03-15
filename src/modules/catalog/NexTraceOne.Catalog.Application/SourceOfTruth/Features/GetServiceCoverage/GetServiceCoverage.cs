using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceCoverage;

/// <summary>
/// Feature: GetServiceCoverage — obtém indicadores de cobertura e maturidade
/// de um serviço no Source of Truth. Usado para governança e visibilidade da
/// completude do cadastro de ativos no NexTraceOne.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceCoverage
{
    /// <summary>Query de indicadores de cobertura de um serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que calcula os indicadores de cobertura de um serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IApiAssetRepository apiRepository,
        ILinkedReferenceRepository referenceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return Error.NotFound("SourceOfTruth.ServiceNotFound", "Service '{0}' not found", request.ServiceId);

            var apis = await apiRepository.ListByServiceIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            var references = await referenceRepository.ListByAssetAsync(
                request.ServiceId, LinkedAssetType.Service, cancellationToken);

            var activeRefs = references.Where(r => r.IsActive).ToList();

            var hasOwner = !string.IsNullOrWhiteSpace(service.TeamName);
            var hasContracts = apis.Count > 0; // Proxying via API existence
            var hasDocumentation = activeRefs.Any(r => r.ReferenceType == LinkedReferenceType.Documentation) ||
                                   !string.IsNullOrWhiteSpace(service.DocumentationUrl);
            var hasRunbook = activeRefs.Any(r => r.ReferenceType == LinkedReferenceType.Runbook);
            var hasChangelog = activeRefs.Any(r => r.ReferenceType == LinkedReferenceType.Changelog);
            var hasDependencies = apis.Count > 0;
            var hasEventTopics = activeRefs.Any(r => r.ReferenceType == LinkedReferenceType.EventTopic);

            // Calcular score de cobertura (0-100)
            var indicators = new[] { hasOwner, hasContracts, hasDocumentation, hasRunbook, hasChangelog, hasDependencies, hasEventTopics };
            var score = indicators.Count(i => i) * 100 / indicators.Length;

            return new Response(
                ServiceId: request.ServiceId,
                ServiceName: service.Name,
                HasOwner: hasOwner,
                HasContracts: hasContracts,
                HasDocumentation: hasDocumentation,
                HasRunbook: hasRunbook,
                HasRecentChangeHistory: hasChangelog,
                HasDependenciesMapped: hasDependencies,
                HasEventTopics: hasEventTopics,
                CoverageScore: score,
                TotalIndicators: indicators.Length,
                MetIndicators: indicators.Count(i => i));
        }
    }

    /// <summary>Resposta com indicadores de cobertura do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        bool HasOwner,
        bool HasContracts,
        bool HasDocumentation,
        bool HasRunbook,
        bool HasRecentChangeHistory,
        bool HasDependenciesMapped,
        bool HasEventTopics,
        int CoverageScore,
        int TotalIndicators,
        int MetIndicators);
}
