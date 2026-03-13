using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Application.Features.GetSubgraph;

/// <summary>
/// Feature: GetSubgraph — obtém um subgrafo focado em um nó raiz com profundidade configurável.
/// Usado para mini-grafos contextuais (sidebar de API, release, workflow)
/// e para navegação progressiva (drill-down). Respeita limit de nós para performance.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetSubgraph
{
    /// <summary>Query de subgrafo centrado em um nó raiz com profundidade máxima.</summary>
    public sealed record Query(Guid RootNodeId, int MaxDepth = 2, int MaxNodes = 50) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta do subgrafo.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RootNodeId).NotEmpty();
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 5);
            RuleFor(x => x.MaxNodes).InclusiveBetween(1, 200);
        }
    }

    /// <summary>
    /// Handler que constrói um subgrafo centrado no nó raiz.
    /// Navega as relações de consumo até a profundidade máxima,
    /// respeitando o limite de nós para manter performance previsível.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);
            var allServices = await serviceAssetRepository.ListAllAsync(cancellationToken);

            var rootApi = allApis.FirstOrDefault(a => a.Id.Value == request.RootNodeId);
            var rootService = allServices.FirstOrDefault(s => s.Id.Value == request.RootNodeId);

            if (rootApi is null && rootService is null)
            {
                return Domain.Errors.EngineeringGraphErrors.ImpactRootNodeNotFound(request.RootNodeId);
            }

            var serviceNodes = new List<SubgraphServiceNode>();
            var apiNodes = new List<SubgraphApiNode>();
            var edges = new List<SubgraphEdge>();
            var visited = new HashSet<Guid>();

            if (rootService is not null)
            {
                CollectServiceSubgraph(rootService, allApis, allServices, request.MaxDepth, request.MaxNodes, serviceNodes, apiNodes, edges, visited, 0);
            }
            else if (rootApi is not null)
            {
                CollectApiSubgraph(rootApi, allApis, allServices, request.MaxDepth, request.MaxNodes, serviceNodes, apiNodes, edges, visited, 0);
            }

            return new Response(
                request.RootNodeId,
                serviceNodes,
                apiNodes,
                edges,
                visited.Count >= request.MaxNodes);
        }

        /// <summary>Coleta subgrafo centrado em um serviço, expandindo APIs e consumidores.</summary>
        private static void CollectServiceSubgraph(
            ServiceAsset service,
            IReadOnlyList<ApiAsset> allApis,
            IReadOnlyList<ServiceAsset> allServices,
            int maxDepth,
            int maxNodes,
            List<SubgraphServiceNode> serviceNodes,
            List<SubgraphApiNode> apiNodes,
            List<SubgraphEdge> edges,
            HashSet<Guid> visited,
            int currentDepth)
        {
            if (currentDepth > maxDepth || visited.Count >= maxNodes || !visited.Add(service.Id.Value))
                return;

            serviceNodes.Add(new SubgraphServiceNode(service.Id.Value, service.Name, service.Domain, service.TeamName));

            var ownedApis = allApis.Where(a => a.OwnerService.Id == service.Id).ToList();
            foreach (var api in ownedApis)
            {
                if (visited.Count >= maxNodes) break;
                CollectApiSubgraph(api, allApis, allServices, maxDepth, maxNodes, serviceNodes, apiNodes, edges, visited, currentDepth + 1);
                edges.Add(new SubgraphEdge(service.Id.Value, api.Id.Value, "Exposes"));
            }
        }

        /// <summary>Coleta subgrafo centrado em uma API, expandindo consumidores transitivos.</summary>
        private static void CollectApiSubgraph(
            ApiAsset api,
            IReadOnlyList<ApiAsset> allApis,
            IReadOnlyList<ServiceAsset> allServices,
            int maxDepth,
            int maxNodes,
            List<SubgraphServiceNode> serviceNodes,
            List<SubgraphApiNode> apiNodes,
            List<SubgraphEdge> edges,
            HashSet<Guid> visited,
            int currentDepth)
        {
            if (currentDepth > maxDepth || visited.Count >= maxNodes || !visited.Add(api.Id.Value))
                return;

            apiNodes.Add(new SubgraphApiNode(api.Id.Value, api.Name, api.RoutePattern, api.Version, api.Visibility, api.OwnerService.Id.Value));

            // Adiciona o serviço proprietário se ainda não visitado
            if (visited.Add(api.OwnerService.Id.Value) && visited.Count <= maxNodes)
            {
                serviceNodes.Add(new SubgraphServiceNode(api.OwnerService.Id.Value, api.OwnerService.Name, api.OwnerService.Domain, api.OwnerService.TeamName));
                edges.Add(new SubgraphEdge(api.OwnerService.Id.Value, api.Id.Value, "Exposes"));
            }

            // Expande consumidores
            foreach (var rel in api.ConsumerRelationships)
            {
                if (visited.Count >= maxNodes) break;
                edges.Add(new SubgraphEdge(api.Id.Value, rel.ConsumerAssetId.Value, "DependsOn"));

                var consumerService = allServices.FirstOrDefault(s => s.Name == rel.ConsumerName);
                if (consumerService is not null)
                {
                    CollectServiceSubgraph(consumerService, allApis, allServices, maxDepth, maxNodes, serviceNodes, apiNodes, edges, visited, currentDepth + 1);
                }
            }
        }
    }

    /// <summary>Resposta com o subgrafo centrado no nó raiz.</summary>
    public sealed record Response(
        Guid RootNodeId,
        IReadOnlyList<SubgraphServiceNode> Services,
        IReadOnlyList<SubgraphApiNode> Apis,
        IReadOnlyList<SubgraphEdge> Edges,
        bool IsTruncated);

    /// <summary>Nó de serviço no subgrafo.</summary>
    public sealed record SubgraphServiceNode(Guid Id, string Name, string Domain, string TeamName);

    /// <summary>Nó de API no subgrafo.</summary>
    public sealed record SubgraphApiNode(Guid Id, string Name, string RoutePattern, string Version, string Visibility, Guid OwnerServiceId);

    /// <summary>Aresta do subgrafo com tipo semântico.</summary>
    public sealed record SubgraphEdge(Guid SourceId, Guid TargetId, string EdgeType);
}
