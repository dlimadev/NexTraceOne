using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Application.Features.GetImpactPropagation;

/// <summary>
/// Feature: GetImpactPropagation — calcula e retorna a propagação de impacto
/// a partir de um nó raiz, identificando consumidores diretos e transitivos
/// com contagem por nível de profundidade.
/// Essencial para blast radius e análise de risco antes de releases.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetImpactPropagation
{
    /// <summary>Query de propagação de impacto centrada em um nó.</summary>
    public sealed record Query(Guid RootNodeId, int MaxDepth = 3) : IQuery<Response>;

    /// <summary>Valida os parâmetros de propagação de impacto.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RootNodeId).NotEmpty();
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 10);
        }
    }

    /// <summary>
    /// Handler que calcula a propagação de impacto a partir de um nó raiz.
    /// Constrói o grafo de dependências transitivas, agrupando consumidores
    /// por nível de profundidade e fornecendo contadores para a UI.
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
            if (rootApi is null)
            {
                return Domain.Errors.EngineeringGraphErrors.ImpactRootNodeNotFound(request.RootNodeId);
            }

            var impactedNodes = new List<ImpactedNode>();
            var visited = new HashSet<Guid> { request.RootNodeId };

            PropagateImpact(rootApi, allApis, allServices, request.MaxDepth, impactedNodes, visited, 1);

            var directCount = impactedNodes.Count(n => n.Depth == 1);
            var transitiveCount = impactedNodes.Count(n => n.Depth > 1);

            return new Response(
                request.RootNodeId,
                rootApi.Name,
                impactedNodes,
                directCount,
                transitiveCount,
                impactedNodes.Count);
        }

        /// <summary>
        /// Propaga impacto recursivamente pelos consumidores da API raiz.
        /// Cada nível de profundidade representa um grau de separação.
        /// </summary>
        private static void PropagateImpact(
            ApiAsset api,
            IReadOnlyList<ApiAsset> allApis,
            IReadOnlyList<ServiceAsset> allServices,
            int maxDepth,
            List<ImpactedNode> impactedNodes,
            HashSet<Guid> visited,
            int currentDepth)
        {
            if (currentDepth > maxDepth)
                return;

            foreach (var rel in api.ConsumerRelationships)
            {
                if (!visited.Add(rel.ConsumerAssetId.Value))
                    continue;

                var consumerService = allServices.FirstOrDefault(s => s.Name == rel.ConsumerName);
                impactedNodes.Add(new ImpactedNode(
                    rel.ConsumerAssetId.Value,
                    rel.ConsumerName,
                    currentDepth,
                    rel.SourceType,
                    rel.ConfidenceScore));

                // Propaga para APIs que o serviço consumidor também expõe
                if (consumerService is not null)
                {
                    var consumerApis = allApis.Where(a => a.OwnerService.Id == consumerService.Id).ToList();
                    foreach (var consumerApi in consumerApis)
                    {
                        if (visited.Contains(consumerApi.Id.Value))
                            continue;

                        PropagateImpact(consumerApi, allApis, allServices, maxDepth, impactedNodes, visited, currentDepth + 1);
                    }
                }
            }
        }
    }

    /// <summary>Resposta da propagação de impacto com nós impactados e contadores.</summary>
    public sealed record Response(
        Guid RootNodeId,
        string RootNodeName,
        IReadOnlyList<ImpactedNode> ImpactedNodes,
        int DirectConsumers,
        int TransitiveConsumers,
        int TotalImpacted);

    /// <summary>Nó impactado com profundidade, proveniência e confiança.</summary>
    public sealed record ImpactedNode(
        Guid NodeId,
        string Name,
        int Depth,
        string SourceType,
        decimal ConfidenceScore);
}
