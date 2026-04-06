using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpactCascade;

/// <summary>
/// Feature: GetCanonicalEntityImpactCascade — análise em cascata multi-nível do impacto
/// de uma entidade canónica em contratos e entidades relacionadas.
/// Determina quantos contratos são afectados directa e indirectamente quando a entidade muda,
/// construindo uma árvore de nós com profundidade configurável (máx. 3).
/// Pilar: Contract Governance + Change Intelligence.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static partial class GetCanonicalEntityImpactCascade
{
    /// <summary>Expressão regular para extrair nomes de schemas $ref do conteúdo OpenAPI.</summary>
    [GeneratedRegex(@"\$ref[""']?\s*:\s*[""']#/components/schemas/([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SchemaRefRegex();

    /// <summary>Nó da árvore de cascata de impacto.</summary>
    public sealed record CascadeNode(
        string EntityName,
        int Depth,
        IReadOnlyList<Guid> AffectedContractIds,
        IReadOnlyList<CascadeNode> Children);

    /// <summary>Query para análise em cascata de impacto de uma entidade canónica.</summary>
    public sealed record Query(Guid CanonicalEntityId, int MaxDepth = 2) : IQuery<Response>;

    /// <summary>Valida a entrada da query de cascata de impacto.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CanonicalEntityId).NotEmpty();
            RuleFor(x => x.MaxDepth).InclusiveBetween(1, 3);
        }
    }

    /// <summary>
    /// Handler que constrói a árvore de cascata de impacto para uma entidade canónica.
    /// Para cada nível, identifica contratos afectados e entidades canónicas relacionadas
    /// referenciadas nesses contratos.
    /// </summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository,
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var root = await entityRepository.GetByIdAsync(
                CanonicalEntityId.From(request.CanonicalEntityId), cancellationToken);
            if (root is null)
                return ContractsErrors.CanonicalEntityNotFound(request.CanonicalEntityId.ToString());

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.Name };
            var allContractIds = new HashSet<Guid>();

            var rootNode = await BuildNodeAsync(root.Name, 0, request.MaxDepth, visited, allContractIds, cancellationToken);

            var riskLevel = allContractIds.Count switch
            {
                0 => "None",
                <= 3 => "Low",
                <= 10 => "Medium",
                <= 25 => "High",
                _ => "Critical"
            };

            return new Response(
                RootEntityId: request.CanonicalEntityId,
                RootEntityName: root.Name,
                TotalContractsAffected: allContractIds.Count,
                TotalUniqueEntitiesInCascade: visited.Count,
                CascadeNodes: rootNode.Children.Count > 0 ? rootNode.Children : [rootNode],
                MaxDepthReached: GetMaxDepthReached(rootNode),
                RiskLevel: riskLevel);
        }

        private async Task<CascadeNode> BuildNodeAsync(
            string entityName, int depth, int maxDepth,
            HashSet<string> visited, HashSet<Guid> allContractIds,
            CancellationToken ct)
        {
            // Pesquisa contratos que referenciam esta entidade
            var (contracts, _) = await contractVersionRepository.SearchAsync(
                null, null, null, entityName, 1, 200, ct);

            var affected = contracts
                .Where(c => c.SpecContent.Contains(entityName, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id.Value)
                .ToList();

            foreach (var id in affected) allContractIds.Add(id);

            if (depth >= maxDepth)
                return new CascadeNode(entityName, depth, affected.AsReadOnly(), []);

            // Extrair entidades relacionadas referenciadas nos contratos afectados
            var relatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var contract in contracts.Where(c => c.SpecContent.Contains(entityName, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (Match match in SchemaRefRegex().Matches(contract.SpecContent))
                {
                    var name = match.Groups[1].Value;
                    if (!visited.Contains(name))
                        relatedNames.Add(name);
                }
            }

            var children = new List<CascadeNode>();
            foreach (var relatedName in relatedNames.Take(10)) // limitar fan-out
            {
                if (visited.Add(relatedName))
                {
                    var child = await BuildNodeAsync(relatedName, depth + 1, maxDepth, visited, allContractIds, ct);
                    children.Add(child);
                }
            }

            return new CascadeNode(entityName, depth, affected.AsReadOnly(), children.AsReadOnly());
        }

        private static int GetMaxDepthReached(CascadeNode node)
        {
            if (node.Children.Count == 0) return node.Depth;
            return node.Children.Max(c => GetMaxDepthReached(c));
        }
    }

    /// <summary>Resposta da análise em cascata de impacto de entidade canónica.</summary>
    public sealed record Response(
        Guid RootEntityId,
        string RootEntityName,
        int TotalContractsAffected,
        int TotalUniqueEntitiesInCascade,
        IReadOnlyList<CascadeNode> CascadeNodes,
        int MaxDepthReached,
        string RiskLevel);
}
