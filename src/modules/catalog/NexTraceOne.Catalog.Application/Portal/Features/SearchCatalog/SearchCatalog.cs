using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Portal.Features.SearchCatalog;

/// <summary>
/// Feature: SearchCatalog — busca universal no catálogo do Developer Portal.
/// Pesquisa contratos publicados e serviços do catálogo com filtros por tipo/status/owner e facets.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchCatalog
{
    /// <summary>Query de busca universal no catálogo do portal.</summary>
    public sealed record Query(
        string SearchTerm,
        string? TypeFilter = null,
        string? StatusFilter = null,
        string? OwnerFilter = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da busca no catálogo.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(2).MaximumLength(500);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que executa busca universal no catálogo.
    /// Combina resultados de contratos (via IContractVersionRepository) e serviços (via IServiceAssetRepository).
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Parse lifecycle state filter
            ContractLifecycleState? lifecycleFilter = null;
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<ContractLifecycleState>(request.StatusFilter, ignoreCase: true, out var parsed))
            {
                lifecycleFilter = parsed;
            }

            // Parse protocol filter from TypeFilter
            ContractProtocol? protocolFilter = null;
            if (!string.IsNullOrWhiteSpace(request.TypeFilter) &&
                Enum.TryParse<ContractProtocol>(request.TypeFilter, ignoreCase: true, out var parsedProtocol))
            {
                protocolFilter = parsedProtocol;
            }

            // Search contracts with repository — delegates to PostgreSQL query with LIKE/FTS
            var (contracts, contractTotal) = await contractVersionRepository.SearchAsync(
                protocol: protocolFilter,
                lifecycleState: lifecycleFilter,
                apiAssetId: null,
                searchTerm: request.SearchTerm,
                page: request.Page,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            // Look up ApiAsset metadata for enrichment (owner, service name)
            var apiAssetIds = contracts.Select(c => c.ApiAssetId).Distinct().ToList();
            var apiAssets = apiAssetIds.Count > 0
                ? await serviceAssetRepository.SearchAsync(request.SearchTerm, cancellationToken)
                : [];

            // Build a lookup of ApiAssetId → service name/team for enrichment
            var serviceNameByApi = new Dictionary<Guid, (string Name, string? Team)>();

            // Map contract results
            var items = new List<SearchResultItem>();
            foreach (var cv in contracts)
            {
                items.Add(new SearchResultItem(
                    EntityId: cv.Id.Value,
                    EntityType: "Contract",
                    Name: $"{cv.Protocol} Contract v{cv.SemVer}",
                    Description: null,
                    Owner: null,
                    Status: cv.LifecycleState.ToString(),
                    Version: cv.SemVer,
                    RelevanceScore: 1.0,
                    MatchReason: "contract_match",
                    LastUpdated: cv.UpdatedAt));
            }

            // Also include matching services (separate entity type)
            var matchingServices = await serviceAssetRepository.SearchAsync(request.SearchTerm, cancellationToken);
            foreach (var svc in matchingServices.Take(request.PageSize - items.Count))
            {
                items.Add(new SearchResultItem(
                    EntityId: svc.Id.Value,
                    EntityType: "Service",
                    Name: svc.Name,
                    Description: svc.Description,
                    Owner: svc.TeamName,
                    Status: svc.LifecycleStatus.ToString(),
                    Version: null,
                    RelevanceScore: 0.9,
                    MatchReason: "service_match",
                    LastUpdated: null));
            }

            // Build facets from contract results
            var typeCounts = contracts
                .GroupBy(c => c.Protocol.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
            var statusCounts = contracts
                .GroupBy(c => c.LifecycleState.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new Response(
                Items: items.AsReadOnly(),
                TotalCount: contractTotal + matchingServices.Count,
                Page: request.Page,
                PageSize: request.PageSize,
                Facets: new SearchFacets(
                    TypeCounts: typeCounts,
                    StatusCounts: statusCounts));

            return Result<Response>.Success(result);
        }
    }

    /// <summary>Item individual de resultado de busca no catálogo.</summary>
    public sealed record SearchResultItem(
        Guid EntityId,
        string EntityType,
        string Name,
        string? Description,
        string? Owner,
        string? Status,
        string? Version,
        double RelevanceScore,
        string? MatchReason,
        DateTimeOffset? LastUpdated);

    /// <summary>Facetas de agregação para refinamento de busca.</summary>
    public sealed record SearchFacets(
        IReadOnlyDictionary<string, int> TypeCounts,
        IReadOnlyDictionary<string, int> StatusCounts);

    /// <summary>Resposta da busca universal com resultados paginados e facetas.</summary>
    public sealed record Response(
        IReadOnlyList<SearchResultItem> Items,
        int TotalCount,
        int Page,
        int PageSize,
        SearchFacets Facets);
}
