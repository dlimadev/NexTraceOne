using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Features.DetectCircularDependencies;

/// <summary>
/// Feature: DetectCircularDependencies — detecta dependências circulares no grafo de serviços.
/// Utiliza DFS (Depth-First Search) com rastreamento de nós em visita para identificar
/// ciclos no grafo de consumidores de APIs. Cada ciclo retorna a cadeia completa de serviços
/// envolvidos, permitindo visualização e resolução imediata.
/// Valor: prevenir blast radius imprevisível causado por ciclos ocultos de dependência.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DetectCircularDependencies
{
    /// <summary>Query de detecção de dependências circulares.</summary>
    /// <param name="ServiceName">Opcional — limita a busca ao subgrafo de um serviço específico.</param>
    public sealed record Query(string? ServiceName = null) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            When(x => x.ServiceName is not null, () =>
                RuleFor(x => x.ServiceName!).NotEmpty().MaximumLength(200));
        }
    }

    /// <summary>
    /// Handler que executa DFS para detectar ciclos no grafo de dependências.
    /// Para cada API e seus consumidores, constrói o grafo de "serviço A consome serviço B"
    /// e identifica caminhos que formam ciclos. Retorna todos os ciclos encontrados
    /// com os participantes ordenados pelo ciclo.
    /// </summary>
    public sealed class Handler(IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);

            // Filtra por serviço se especificado
            if (request.ServiceName is not null)
            {
                allApis = allApis
                    .Where(a => a.OwnerService.Name.Equals(
                        request.ServiceName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Constrói grafo de adjacência: serviceOwner → [consumingServices]
            // Um serviço B "depende" do serviço A quando B consome uma API publicada por A.
            var adjacency = BuildAdjacency(allApis);

            var cycles = DetectCycles(adjacency);

            return new Response(
                TotalServicesAnalyzed: adjacency.Count,
                CircularDependenciesFound: cycles.Count > 0,
                Cycles: cycles);
        }

        /// <summary>
        /// Constrói o mapa de adjacência: nome do serviço → lista de serviços que ele consome.
        /// Se o serviço S1 consome a API publicada por S2, então S1 → S2.
        /// </summary>
        private static Dictionary<string, HashSet<string>> BuildAdjacency(
            IReadOnlyList<ApiAsset> allApis)
        {
            var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var api in allApis)
            {
                var publisher = api.OwnerService.Name;
                if (!adjacency.ContainsKey(publisher))
                    adjacency[publisher] = [];

                foreach (var cr in api.ConsumerRelationships)
                {
                    var consumer = cr.ConsumerName;
                    if (!adjacency.ContainsKey(consumer))
                        adjacency[consumer] = [];

                    // consumer depends-on publisher
                    adjacency[consumer].Add(publisher);
                }
            }

            return adjacency;
        }

        /// <summary>
        /// Executa DFS em todos os nós não visitados e coleta ciclos usando coloração tricolor.
        /// Branco = não visitado, Cinza = em visita (na pilha), Preto = visitado completamente.
        /// Quando encontramos uma aresta para um nó Cinza, detectamos um ciclo.
        /// </summary>
        private static List<CircularDependency> DetectCycles(
            Dictionary<string, HashSet<string>> adjacency)
        {
            var cycles = new List<CircularDependency>();
            var color = new Dictionary<string, NodeColor>(StringComparer.OrdinalIgnoreCase);
            var path = new List<string>();
            var seenCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in adjacency.Keys)
                color[node] = NodeColor.White;

            foreach (var node in adjacency.Keys)
            {
                if (color[node] == NodeColor.White)
                    DfsVisit(node, adjacency, color, path, cycles, seenCycles);
            }

            return cycles;
        }

        private static void DfsVisit(
            string node,
            Dictionary<string, HashSet<string>> adjacency,
            Dictionary<string, NodeColor> color,
            List<string> path,
            List<CircularDependency> cycles,
            HashSet<string> seenCycles)
        {
            color[node] = NodeColor.Gray;
            path.Add(node);

            if (adjacency.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!color.ContainsKey(neighbor))
                        color[neighbor] = NodeColor.White;

                    if (color[neighbor] == NodeColor.Gray)
                    {
                        // Ciclo detectado — extrair a cadeia
                        var cycleStart = path.IndexOf(neighbor);
                        if (cycleStart >= 0)
                        {
                            var cycleNodes = path[cycleStart..];
                            var cycleKey = string.Join("→",
                                cycleNodes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                            if (seenCycles.Add(cycleKey))
                            {
                                cycles.Add(new CircularDependency(
                                    Participants: cycleNodes.ToList(),
                                    CycleLength: cycleNodes.Count,
                                    Description: BuildCycleDescription(cycleNodes, neighbor)));
                            }
                        }
                    }
                    else if (color[neighbor] == NodeColor.White)
                    {
                        DfsVisit(neighbor, adjacency, color, path, cycles, seenCycles);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            color[node] = NodeColor.Black;
        }

        private static string BuildCycleDescription(List<string> cycleNodes, string backEdgeTarget)
        {
            var chain = string.Join(" → ", cycleNodes);
            return $"{chain} → {backEdgeTarget} (circular)";
        }

        private enum NodeColor { White, Gray, Black }
    }

    /// <summary>Resposta da detecção de dependências circulares.</summary>
    public sealed record Response(
        int TotalServicesAnalyzed,
        bool CircularDependenciesFound,
        IReadOnlyList<CircularDependency> Cycles);

    /// <summary>Representa um ciclo de dependência detectado.</summary>
    public sealed record CircularDependency(
        IReadOnlyList<string> Participants,
        int CycleLength,
        string Description);
}
