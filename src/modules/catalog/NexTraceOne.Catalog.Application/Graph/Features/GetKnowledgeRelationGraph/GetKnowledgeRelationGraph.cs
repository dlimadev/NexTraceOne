using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetKnowledgeRelationGraph;

/// <summary>
/// Feature: GetKnowledgeRelationGraph — grafo semântico de relações entre entidades do NexTraceOne.
///
/// Constrói um grafo de conhecimento navegável a partir de relações estruturais entre entidades:
/// - <c>KnowledgeNode</c> — entidades tipificadas: Service, Contract, Runbook, Incident, Release, Team, OperationalNote
/// - <c>KnowledgeEdge</c> — relações nomeadas: OwnedBy, DependsOn, PublishesContract, ConsumesContract, CorrelatedWith, MitigatedBy
/// - <c>RelationStrength</c> — peso de aresta baseado em recência com decaimento temporal configurável
/// - <c>AnchorEntityId</c> — quando definido, filtra o grafo até MaxDepth saltos a partir da entidade âncora
/// - <c>MaxNodes</c> — limita o tamanho do grafo retornado, priorizando arestas com maior RelationStrength
/// - <c>KnowledgeGraphSummary</c> — contagens de nós/arestas por tipo e densidade do grafo
///
/// Orienta Architect, Tech Lead e Engineer a navegar o contexto operacional de qualquer entidade
/// sem saltar entre módulos.
///
/// Wave AB.1 — GetKnowledgeRelationGraph (Catalog Graph).
/// </summary>
public static class GetKnowledgeRelationGraph
{
    // ── Tipos de nó e aresta ──────────────────────────────────────────────

    /// <summary>Tipo de nó no grafo de conhecimento.</summary>
    public enum KnowledgeNodeType
    {
        /// <summary>Serviço registado no catálogo.</summary>
        Service,
        /// <summary>Contrato de API, evento ou serviço de background.</summary>
        Contract,
        /// <summary>Runbook operacional.</summary>
        Runbook,
        /// <summary>Tipo de incidente correlacionado.</summary>
        Incident,
        /// <summary>Entidade de release.</summary>
        Release,
        /// <summary>Equipa proprietária.</summary>
        Team,
        /// <summary>Nota operacional.</summary>
        OperationalNote
    }

    /// <summary>Tipo de aresta no grafo de conhecimento.</summary>
    public enum KnowledgeEdgeType
    {
        /// <summary>Serviço pertence a equipa.</summary>
        OwnedBy,
        /// <summary>Serviço depende de outro serviço.</summary>
        DependsOn,
        /// <summary>Serviço publica contrato.</summary>
        PublishesContract,
        /// <summary>Serviço consome contrato.</summary>
        ConsumesContract,
        /// <summary>Serviço correlacionado com tipo de incidente.</summary>
        CorrelatedWith,
        /// <summary>Serviço mitigado por runbook.</summary>
        MitigatedBy,
        /// <summary>Entidade documentada em nota operacional.</summary>
        DocumentedIn,
        /// <summary>Serviço implantado como release.</summary>
        DeployedAs
    }

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Nó do grafo de conhecimento — entidade tipificada com metadados.</summary>
    public sealed record KnowledgeNode(
        string Id,
        string Label,
        KnowledgeNodeType NodeType,
        string? Description);

    /// <summary>Aresta do grafo de conhecimento — relação ponderada entre dois nós.</summary>
    public sealed record KnowledgeEdge(
        string SourceId,
        string TargetId,
        KnowledgeEdgeType EdgeType,
        double RelationStrength);

    /// <summary>Resumo estatístico do grafo de conhecimento gerado.</summary>
    public sealed record KnowledgeGraphSummary(
        int TotalNodes,
        int TotalEdges,
        int ServiceCount,
        int ContractCount,
        int RunbookCount,
        int IncidentTypeCount,
        double GraphDensity);

    /// <summary>Grafo de conhecimento completo com nós, arestas e resumo.</summary>
    public sealed record KnowledgeGraph(
        IReadOnlyList<KnowledgeNode> Nodes,
        IReadOnlyList<KnowledgeEdge> Edges,
        KnowledgeGraphSummary Summary);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>AnchorEntityId</c>: quando definido, filtra o grafo até MaxDepth saltos a partir desta entidade.</para>
    /// <para><c>AnchorEntityType</c>: tipo do nó âncora, para resolução correcta do Id.</para>
    /// <para><c>MaxDepth</c>: profundidade máxima de expansão do grafo a partir da âncora (1–3, default 2).</para>
    /// <para><c>MaxNodes</c>: número máximo de nós retornados (1–500, default 200).</para>
    /// <para><c>RelationStrengthDecayDays</c>: dias de decaimento para RelationStrength temporal (1–365, default 90).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string? AnchorEntityId = null,
        KnowledgeNodeType? AnchorEntityType = null,
        int MaxDepth = 2,
        int MaxNodes = 200,
        int RelationStrengthDecayDays = 90) : IQuery<KnowledgeGraph>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxDepth).InclusiveBetween(1, 3);
            RuleFor(q => q.MaxNodes).InclusiveBetween(1, 500);
            RuleFor(q => q.RelationStrengthDecayDays).InclusiveBetween(1, 365);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, KnowledgeGraph>
    {
        private readonly IKnowledgeRelationReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IKnowledgeRelationReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<KnowledgeGraph>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var relations = await _reader.ListServiceRelationsAsync(query.TenantId, cancellationToken);

            if (relations.Count == 0)
                return Result<KnowledgeGraph>.Success(BuildEmptyGraph());

            // Constrói nós e arestas a partir das relações carregadas
            var nodes = new Dictionary<string, KnowledgeNode>(StringComparer.OrdinalIgnoreCase);
            var edges = new List<KnowledgeEdge>();

            foreach (var rel in relations)
            {
                EnsureServiceNode(nodes, rel.ServiceName);

                // Aresta OwnedBy: serviço → equipa
                if (!string.IsNullOrWhiteSpace(rel.TeamName))
                {
                    EnsureTeamNode(nodes, rel.TeamName);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: rel.TeamName,
                        EdgeType: KnowledgeEdgeType.OwnedBy,
                        RelationStrength: 1.0));
                }

                // Arestas DependsOn: serviço → serviços dependência
                foreach (var dep in rel.DependsOnServices)
                {
                    EnsureServiceNode(nodes, dep);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: dep,
                        EdgeType: KnowledgeEdgeType.DependsOn,
                        RelationStrength: 1.0));
                }

                // Arestas PublishesContract: serviço → contrato publicado
                foreach (var contract in rel.PublishedContracts)
                {
                    EnsureContractNode(nodes, contract);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: contract,
                        EdgeType: KnowledgeEdgeType.PublishesContract,
                        RelationStrength: 1.0));
                }

                // Arestas ConsumesContract: serviço → contrato consumido
                foreach (var contract in rel.ConsumedContracts)
                {
                    EnsureContractNode(nodes, contract);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: contract,
                        EdgeType: KnowledgeEdgeType.ConsumesContract,
                        RelationStrength: 1.0));
                }

                // Arestas MitigatedBy: serviço → runbook
                foreach (var runbook in rel.AssociatedRunbooks)
                {
                    EnsureRunbookNode(nodes, runbook);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: runbook,
                        EdgeType: KnowledgeEdgeType.MitigatedBy,
                        RelationStrength: 1.0));
                }

                // Arestas CorrelatedWith: serviço → tipo de incidente (força temporal)
                foreach (var incidentType in rel.AssociatedIncidentTypes)
                {
                    EnsureIncidentNode(nodes, incidentType);
                    double strength = ComputeTemporalStrength(rel.LastIncidentAt, now, query.RelationStrengthDecayDays);
                    edges.Add(new KnowledgeEdge(
                        SourceId: rel.ServiceName,
                        TargetId: incidentType,
                        EdgeType: KnowledgeEdgeType.CorrelatedWith,
                        RelationStrength: strength));
                }
            }

            // Aplica filtro de âncora se definido
            if (!string.IsNullOrWhiteSpace(query.AnchorEntityId))
            {
                (nodes, edges) = FilterByAnchor(
                    nodes, edges, query.AnchorEntityId, query.MaxDepth);
            }

            // Limita número de nós se necessário
            if (nodes.Count > query.MaxNodes)
            {
                (nodes, edges) = TruncateGraph(
                    nodes, edges, query.AnchorEntityId, query.MaxNodes);
            }

            var nodeList = nodes.Values.ToList();
            var edgeList = edges
                .Where(e => nodes.ContainsKey(e.SourceId) && nodes.ContainsKey(e.TargetId))
                .ToList();

            var summary = BuildSummary(nodeList, edgeList);
            return Result<KnowledgeGraph>.Success(new KnowledgeGraph(nodeList, edgeList, summary));
        }

        // ── Helpers de construção de nós ─────────────────────────────────

        private static void EnsureServiceNode(Dictionary<string, KnowledgeNode> nodes, string name)
        {
            if (!nodes.ContainsKey(name))
                nodes[name] = new KnowledgeNode(name, name, KnowledgeNodeType.Service, null);
        }

        private static void EnsureTeamNode(Dictionary<string, KnowledgeNode> nodes, string name)
        {
            if (!nodes.ContainsKey(name))
                nodes[name] = new KnowledgeNode(name, name, KnowledgeNodeType.Team, null);
        }

        private static void EnsureContractNode(Dictionary<string, KnowledgeNode> nodes, string name)
        {
            if (!nodes.ContainsKey(name))
                nodes[name] = new KnowledgeNode(name, name, KnowledgeNodeType.Contract, null);
        }

        private static void EnsureRunbookNode(Dictionary<string, KnowledgeNode> nodes, string name)
        {
            if (!nodes.ContainsKey(name))
                nodes[name] = new KnowledgeNode(name, name, KnowledgeNodeType.Runbook, null);
        }

        private static void EnsureIncidentNode(Dictionary<string, KnowledgeNode> nodes, string name)
        {
            if (!nodes.ContainsKey(name))
                nodes[name] = new KnowledgeNode(name, name, KnowledgeNodeType.Incident, null);
        }

        // ── Cálculo de RelationStrength temporal ─────────────────────────

        private static double ComputeTemporalStrength(
            DateTimeOffset? lastActivityAt,
            DateTimeOffset now,
            int decayDays)
        {
            if (!lastActivityAt.HasValue)
                return 1.0 / (1 + (double)decayDays / decayDays); // equivale a 0.5 quando sem dados
            double daysSince = (now - lastActivityAt.Value).TotalDays;
            return 1.0 / (1.0 + daysSince / decayDays);
        }

        // ── Filtro por âncora ─────────────────────────────────────────────

        private static (Dictionary<string, KnowledgeNode> nodes, List<KnowledgeEdge> edges)
            FilterByAnchor(
                Dictionary<string, KnowledgeNode> allNodes,
                List<KnowledgeEdge> allEdges,
                string anchorId,
                int maxDepth)
        {
            // BFS a partir do anchorId até maxDepth saltos (não-dirigido)
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { anchorId };
            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { anchorId };

            for (int depth = 0; depth < maxDepth && current.Count > 0; depth++)
            {
                var next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var edge in allEdges)
                {
                    if (current.Contains(edge.SourceId) && !visited.Contains(edge.TargetId))
                        next.Add(edge.TargetId);
                    if (current.Contains(edge.TargetId) && !visited.Contains(edge.SourceId))
                        next.Add(edge.SourceId);
                }
                foreach (var n in next) visited.Add(n);
                current = next;
            }

            var filteredNodes = allNodes
                .Where(kv => visited.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            var filteredEdges = allEdges
                .Where(e => filteredNodes.ContainsKey(e.SourceId) && filteredNodes.ContainsKey(e.TargetId))
                .ToList();

            return (filteredNodes, filteredEdges);
        }

        // ── Truncagem de grafo ────────────────────────────────────────────

        private static (Dictionary<string, KnowledgeNode> nodes, List<KnowledgeEdge> edges)
            TruncateGraph(
                Dictionary<string, KnowledgeNode> allNodes,
                List<KnowledgeEdge> allEdges,
                string? anchorId,
                int maxNodes)
        {
            // Prioriza âncora e arestas de maior RelationStrength
            var keepNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(anchorId) && allNodes.ContainsKey(anchorId))
                keepNodes.Add(anchorId);

            var sortedEdges = allEdges.OrderByDescending(e => e.RelationStrength).ToList();

            foreach (var edge in sortedEdges)
            {
                if (keepNodes.Count >= maxNodes) break;
                keepNodes.Add(edge.SourceId);
                if (keepNodes.Count < maxNodes)
                    keepNodes.Add(edge.TargetId);
            }

            var truncNodes = allNodes
                .Where(kv => keepNodes.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            var truncEdges = sortedEdges
                .Where(e => truncNodes.ContainsKey(e.SourceId) && truncNodes.ContainsKey(e.TargetId))
                .ToList();

            return (truncNodes, truncEdges);
        }

        // ── Construção do resumo ──────────────────────────────────────────

        private static KnowledgeGraphSummary BuildSummary(
            IReadOnlyList<KnowledgeNode> nodes,
            IReadOnlyList<KnowledgeEdge> edges)
        {
            int totalNodes = nodes.Count;
            int totalEdges = edges.Count;
            int serviceCount = nodes.Count(n => n.NodeType == KnowledgeNodeType.Service);
            int contractCount = nodes.Count(n => n.NodeType == KnowledgeNodeType.Contract);
            int runbookCount = nodes.Count(n => n.NodeType == KnowledgeNodeType.Runbook);
            int incidentTypeCount = nodes.Count(n => n.NodeType == KnowledgeNodeType.Incident);

            long pairs = (long)totalNodes * (totalNodes - 1);
            double density = pairs > 0 ? (double)totalEdges / pairs : 0.0;

            return new KnowledgeGraphSummary(
                TotalNodes: totalNodes,
                TotalEdges: totalEdges,
                ServiceCount: serviceCount,
                ContractCount: contractCount,
                RunbookCount: runbookCount,
                IncidentTypeCount: incidentTypeCount,
                GraphDensity: Math.Round(density, 4));
        }

        private static KnowledgeGraph BuildEmptyGraph()
        {
            var summary = new KnowledgeGraphSummary(0, 0, 0, 0, 0, 0, 0.0);
            return new KnowledgeGraph([], [], summary);
        }
    }
}
