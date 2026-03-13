using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.GetAssetGraph;

/// <summary>
/// Feature: GetAssetGraph — obtém o grafo completo de ativos e suas relações de consumo.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class GetAssetGraph
{
    /// <summary>Query do grafo completo de ativos de API.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna o grafo de ativos com relacionamentos.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssets = await apiAssetRepository.ListAllAsync(cancellationToken);
            var services = await serviceAssetRepository.ListAllAsync(cancellationToken);

            var serviceNodes = services
                .Select(svc => new ServiceNode(svc.Id.Value, svc.Name, svc.Domain, svc.TeamName))
                .ToList();

            var apiNodes = apiAssets
                .Select(api => new ApiNode(
                    api.Id.Value,
                    api.Name,
                    api.RoutePattern,
                    api.Version,
                    api.Visibility,
                    api.OwnerService.Id.Value,
                    api.ConsumerRelationships.Select(rel => new ConsumerEdge(
                        rel.Id.Value,
                        rel.ConsumerName,
                        rel.SourceType,
                        rel.ConfidenceScore,
                        rel.LastObservedAt)).ToList()))
                .ToList();

            return new Response(serviceNodes, apiNodes);
        }
    }

    /// <summary>Resposta com o grafo completo de engenharia.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceNode> Services,
        IReadOnlyList<ApiNode> Apis);

    /// <summary>Nó representando um serviço no grafo.</summary>
    public sealed record ServiceNode(Guid ServiceAssetId, string Name, string Domain, string TeamName);

    /// <summary>Nó representando uma API com seus consumidores conhecidos.</summary>
    public sealed record ApiNode(
        Guid ApiAssetId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        Guid OwnerServiceAssetId,
        IReadOnlyList<ConsumerEdge> Consumers);

    /// <summary>Aresta representando a relação de consumo entre API e consumidor.</summary>
    public sealed record ConsumerEdge(
        Guid RelationshipId,
        string ConsumerName,
        string SourceType,
        decimal ConfidenceScore,
        DateTimeOffset LastObservedAt);
}
