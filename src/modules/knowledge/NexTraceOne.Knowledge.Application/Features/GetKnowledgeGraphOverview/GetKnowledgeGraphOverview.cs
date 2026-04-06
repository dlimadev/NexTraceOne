using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeGraphOverview;

/// <summary>
/// Feature: GetKnowledgeGraphOverview — retorna visão em grafo das relações de conhecimento.
/// Expõe nós (documentos, serviços, contratos) e arestas (relações) para visualização.
/// Suporta travessia centrada numa entidade ou overview global.
/// </summary>
public static class GetKnowledgeGraphOverview
{
    public sealed record Query(
        string? CenterEntityType,
        string? CenterEntityId,
        int MaxDepth = 2) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 5);
            RuleFor(x => x.CenterEntityType).MaximumLength(100).When(x => x.CenterEntityType is not null);
            RuleFor(x => x.CenterEntityId).MaximumLength(100).When(x => x.CenterEntityId is not null);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository documentRepository,
        IKnowledgeRelationRepository relationRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var nodes = new Dictionary<string, KnowledgeGraphNode>();
            var edges = new List<KnowledgeGraphEdge>();

            if (!string.IsNullOrWhiteSpace(request.CenterEntityId))
            {
                await TraverseFromEntityAsync(
                    request.CenterEntityId, request.CenterEntityType ?? "Unknown",
                    request.MaxDepth, 0, nodes, edges, new HashSet<string>(), cancellationToken);
            }
            else
            {
                await BuildGlobalOverviewAsync(nodes, edges, cancellationToken);
            }

            var connectedComponents = ComputeConnectedComponents(nodes.Keys.ToList(), edges);

            return Result<Response>.Success(new Response(
                nodes.Values.ToList(),
                edges,
                nodes.Count,
                edges.Count,
                connectedComponents,
                clock.UtcNow));
        }

        private async Task TraverseFromEntityAsync(
            string entityId,
            string entityType,
            int maxDepth,
            int currentDepth,
            Dictionary<string, KnowledgeGraphNode> nodes,
            List<KnowledgeGraphEdge> edges,
            HashSet<string> visited,
            CancellationToken cancellationToken)
        {
            if (currentDepth > maxDepth || visited.Contains(entityId))
                return;

            visited.Add(entityId);
            nodes.TryAdd(entityId, new KnowledgeGraphNode(entityId, entityId, entityType, new Dictionary<string, string>()));

            if (!Guid.TryParse(entityId, out var guidId))
                return;

            var relations = await relationRepository.ListBySourceAsync(guidId, cancellationToken);
            foreach (var relation in relations)
            {
                var targetId = relation.TargetEntityId.ToString();
                var edgeId = $"{entityId}→{targetId}";
                if (!edges.Any(e => e.SourceId == entityId && e.TargetId == targetId))
                {
                    edges.Add(new KnowledgeGraphEdge(entityId, targetId, relation.TargetType.ToString(), 1.0));
                }

                nodes.TryAdd(targetId, new KnowledgeGraphNode(
                    targetId, targetId, relation.TargetType.ToString(), new Dictionary<string, string>()));

                await TraverseFromEntityAsync(
                    targetId, relation.TargetType.ToString(),
                    maxDepth, currentDepth + 1, nodes, edges, visited, cancellationToken);
            }
        }

        private async Task BuildGlobalOverviewAsync(
            Dictionary<string, KnowledgeGraphNode> nodes,
            List<KnowledgeGraphEdge> edges,
            CancellationToken cancellationToken)
        {
            // Limite prático para overview — grafos muito grandes degradam visualização.
            var (documents, _) = await documentRepository.ListAsync(null, null, 1, 500, cancellationToken);

            foreach (var doc in documents)
            {
                var nodeId = doc.Id.Value.ToString();
                nodes[nodeId] = new KnowledgeGraphNode(
                    nodeId,
                    doc.Title,
                    "Document",
                    new Dictionary<string, string>
                    {
                        ["category"] = doc.Category.ToString(),
                        ["status"] = doc.Status.ToString()
                    });

                var relations = await relationRepository.ListBySourceAsync(doc.Id.Value, cancellationToken);
                foreach (var relation in relations)
                {
                    var targetId = relation.TargetEntityId.ToString();
                    nodes.TryAdd(targetId, new KnowledgeGraphNode(
                        targetId, targetId, relation.TargetType.ToString(), new Dictionary<string, string>()));

                    if (!edges.Any(e => e.SourceId == nodeId && e.TargetId == targetId))
                    {
                        edges.Add(new KnowledgeGraphEdge(nodeId, targetId, relation.TargetType.ToString(), 1.0));
                    }
                }
            }
        }

        private static int ComputeConnectedComponents(List<string> nodeIds, List<KnowledgeGraphEdge> edges)
        {
            if (nodeIds.Count == 0) return 0;

            var parent = nodeIds.ToDictionary(id => id, id => id);

            string Find(string x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
            void Union(string a, string b) { parent[Find(a)] = Find(b); }

            foreach (var edge in edges)
            {
                if (parent.ContainsKey(edge.SourceId) && parent.ContainsKey(edge.TargetId))
                    Union(edge.SourceId, edge.TargetId);
            }

            return nodeIds.Select(Find).Distinct().Count();
        }
    }

    public sealed record KnowledgeGraphNode(string Id, string Label, string Type, Dictionary<string, string> Metadata);
    public sealed record KnowledgeGraphEdge(string SourceId, string TargetId, string RelationType, double Weight);

    public sealed record Response(
        IReadOnlyList<KnowledgeGraphNode> Nodes,
        IReadOnlyList<KnowledgeGraphEdge> Edges,
        int TotalNodes,
        int TotalEdges,
        int ConnectedComponents,
        DateTimeOffset ComputedAt);
}
