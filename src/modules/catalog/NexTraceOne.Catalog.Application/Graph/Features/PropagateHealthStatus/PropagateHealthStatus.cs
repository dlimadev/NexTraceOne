using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.PropagateHealthStatus;

/// <summary>
/// Feature: PropagateHealthStatus — propaga o status de saúde de um serviço degradado
/// para todos os seus dependentes diretos e transitivos.
/// Se o serviço A está Unhealthy e B depende de A (consome alguma API de A),
/// então B é marcado como "AtRisk". A propagação respeita o MaxDepth para evitar
/// explosão no grafo.
/// Valor: torna visível o blast radius de degradações em tempo real,
/// complementando a análise estática de mudanças.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class PropagateHealthStatus
{
    /// <summary>Query de propagação de saúde a partir de um nó raiz.</summary>
    /// <param name="RootServiceName">Nome do serviço com saúde degradada.</param>
    /// <param name="MaxDepth">Profundidade máxima de propagação (padrão 4).</param>
    public sealed record Query(string RootServiceName, int MaxDepth = 4) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query de propagação de saúde.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RootServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 10);
        }
    }

    /// <summary>
    /// Handler que propaga o status de saúde através do grafo de dependências.
    /// Para cada serviço que consome APIs do nó raiz (ou de serviços já marcados "AtRisk"),
    /// o handler marca esse serviço como "AtRisk" com a profundidade e o caminho de propagação.
    /// Utiliza BFS para garantir que a menor cadeia de propagação é retornada.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        INodeHealthRepository nodeHealthRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);

            // Verifica que o serviço raiz existe no grafo
            var rootExists = allApis.Any(a =>
                a.OwnerService.Name.Equals(request.RootServiceName, StringComparison.OrdinalIgnoreCase));

            if (!rootExists)
                return CatalogGraphErrors.ServiceAssetNotFound(request.RootServiceName);

            // Obtém o status atual de saúde do nó raiz (pode não existir ainda)
            var latestHealthRecords = await nodeHealthRepository
                .GetLatestByOverlayAsync(OverlayMode.Health, cancellationToken);

            var healthByNodeId = latestHealthRecords
                .GroupBy(r => r.NodeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CalculatedAt).First());

            // Constrói mapa de "quem consome quem": publisher → [consumers]
            var publisherToConsumers = BuildPublisherToConsumersMap(allApis);

            // BFS a partir do nó raiz
            var affectedNodes = new List<AffectedNode>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { request.RootServiceName };
            var queue = new Queue<(string ServiceName, int Depth, IReadOnlyList<string> Path)>();
            queue.Enqueue((request.RootServiceName, 0, [request.RootServiceName]));

            while (queue.Count > 0)
            {
                var (current, depth, currentPath) = queue.Dequeue();

                if (!publisherToConsumers.TryGetValue(current, out var directConsumers))
                    continue;

                if (depth >= request.MaxDepth)
                    continue;

                foreach (var consumer in directConsumers)
                {
                    if (visited.Add(consumer))
                    {
                        var propagatedPath = currentPath.Concat([consumer]).ToList();
                        affectedNodes.Add(new AffectedNode(
                            ServiceName: consumer,
                            PropagationDepth: depth + 1,
                            PropagationPath: propagatedPath,
                            AtRiskReason: $"Depends on '{request.RootServiceName}' (depth {depth + 1}) via {string.Join(" → ", propagatedPath)}"));

                        queue.Enqueue((consumer, depth + 1, propagatedPath));
                    }
                }
            }

            // Determina o status real do nó raiz
            var rootStatus = DetermineRootStatus(request.RootServiceName, allApis, healthByNodeId);

            return new Response(
                RootServiceName: request.RootServiceName,
                RootHealthStatus: rootStatus,
                AffectedServicesCount: affectedNodes.Count,
                AffectedNodes: affectedNodes);
        }

        /// <summary>
        /// Constrói mapa inverso: publisher → [lista de serviços que consomem APIs do publisher].
        /// </summary>
        private static Dictionary<string, HashSet<string>> BuildPublisherToConsumersMap(
            IReadOnlyList<ApiAsset> allApis)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var api in allApis)
            {
                var publisher = api.OwnerService.Name;
                if (!map.ContainsKey(publisher))
                    map[publisher] = [];

                foreach (var cr in api.ConsumerRelationships)
                {
                    map[publisher].Add(cr.ConsumerName);
                }
            }

            return map;
        }

        private static string DetermineRootStatus(
            string serviceName,
            IReadOnlyList<ApiAsset> allApis,
            Dictionary<Guid, NodeHealthRecord> healthByNodeId)
        {
            var rootApi = allApis.FirstOrDefault(a =>
                a.OwnerService.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

            if (rootApi is null)
                return "Unknown";

            if (healthByNodeId.TryGetValue(rootApi.OwnerService.Id.Value, out var record))
                return record.Status.ToString();

            return "Unknown";
        }
    }

    /// <summary>Resposta da propagação de saúde.</summary>
    public sealed record Response(
        string RootServiceName,
        string RootHealthStatus,
        int AffectedServicesCount,
        IReadOnlyList<AffectedNode> AffectedNodes);

    /// <summary>Serviço afetado pela propagação de saúde.</summary>
    public sealed record AffectedNode(
        string ServiceName,
        int PropagationDepth,
        IReadOnlyList<string> PropagationPath,
        string AtRiskReason);
}
