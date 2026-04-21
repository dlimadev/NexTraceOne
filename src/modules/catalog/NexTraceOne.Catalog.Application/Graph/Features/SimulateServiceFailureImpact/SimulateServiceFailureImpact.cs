using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.SimulateServiceFailureImpact;

/// <summary>
/// Feature: SimulateServiceFailureImpact — simula o impacto de falha total de um serviço.
/// Responde a "se este serviço cair, quais outros serviços e APIs ficam impactados transitivamente?".
///
/// Algoritmo:
/// 1. Encontra o ServiceAsset alvo.
/// 2. Lista todas as APIs expostas por esse serviço.
/// 3. Para cada API, propaga o impacto recursivamente pelos consumidores.
/// 4. Agrega por consumidor para eliminar duplicados via conjunto de visitados.
/// 5. O nível de risco de cascata considera: tier do serviço afectado + profundidade + count.
///
/// Componente central do Digital Twin operacional (Wave D.1.b).
/// </summary>
public static class SimulateServiceFailureImpact
{
    public sealed record Query(
        Guid ServiceId,
        int MaxDepth = 3) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 10);
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IApiAssetRepository apiRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceId = ServiceAssetId.From(request.ServiceId);
            var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var allApis = await apiRepository.ListAllAsync(cancellationToken);
            var allServices = await serviceRepository.ListAllAsync(cancellationToken);

            var serviceApis = allApis.Where(a => a.OwnerService?.Id == serviceId).ToList();

            var impactedNodes = new List<FailureImpactNode>();
            var visited = new HashSet<Guid> { request.ServiceId };

            foreach (var api in serviceApis)
            {
                PropagateFailure(api, allApis, allServices, request.MaxDepth, impactedNodes, visited, 1);
            }

            var cascadeRisk = ComputeCascadeRisk(service.Tier, impactedNodes.Count);
            var directCount = impactedNodes.Count(n => n.Depth == 1);
            var transitiveCount = impactedNodes.Count(n => n.Depth > 1);

            return Result<Response>.Success(new Response(
                ServiceId: request.ServiceId,
                ServiceName: service.Name,
                ServiceTier: service.Tier.ToString(),
                ExposedApisCount: serviceApis.Count,
                CascadeRisk: cascadeRisk,
                DirectImpactCount: directCount,
                TransitiveImpactCount: transitiveCount,
                TotalImpacted: impactedNodes.Count,
                ImpactedNodes: impactedNodes));
        }

        private static void PropagateFailure(
            ApiAsset api,
            IReadOnlyList<ApiAsset> allApis,
            IReadOnlyList<ServiceAsset> allServices,
            int maxDepth,
            List<FailureImpactNode> impacted,
            HashSet<Guid> visited,
            int depth)
        {
            if (depth > maxDepth) return;

            foreach (var rel in api.ConsumerRelationships)
            {
                if (!visited.Add(rel.ConsumerAssetId.Value)) continue;

                var consumerService = allServices.FirstOrDefault(s => s.Name == rel.ConsumerName);
                impacted.Add(new FailureImpactNode(
                    ConsumerName: rel.ConsumerName,
                    ConsumerServiceId: consumerService?.Id.Value,
                    ConsumerTeam: consumerService?.TeamName ?? string.Empty,
                    Depth: depth,
                    SourceType: rel.SourceType,
                    ConfidenceScore: rel.ConfidenceScore,
                    CascadeRisk: depth == 1 ? "direct" : "transitive"));

                if (consumerService is not null)
                {
                    var consumerApis = allApis.Where(a => a.OwnerService?.Id == consumerService.Id).ToList();
                    foreach (var consumerApi in consumerApis)
                    {
                        if (!visited.Add(consumerApi.Id.Value)) continue;
                        PropagateFailure(consumerApi, allApis, allServices, maxDepth, impacted, visited, depth + 1);
                    }
                }
            }
        }

        private static string ComputeCascadeRisk(ServiceTierType tier, int impactedCount) =>
            (tier, impactedCount) switch
            {
                (ServiceTierType.Critical, >= 5) => "critical",
                (ServiceTierType.Critical, _) => "high",
                (ServiceTierType.Standard, >= 10) => "high",
                (ServiceTierType.Standard, >= 3) => "medium",
                _ => "low"
            };
    }

    public sealed record FailureImpactNode(
        string ConsumerName,
        Guid? ConsumerServiceId,
        string ConsumerTeam,
        int Depth,
        string SourceType,
        decimal ConfidenceScore,
        string CascadeRisk);

    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string ServiceTier,
        int ExposedApisCount,
        string CascadeRisk,
        int DirectImpactCount,
        int TransitiveImpactCount,
        int TotalImpacted,
        IReadOnlyList<FailureImpactNode> ImpactedNodes);
}
