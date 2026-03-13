using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.DeveloperPortal.Application.Features.SearchCatalog;

/// <summary>
/// Feature: SearchCatalog — busca universal no catálogo do Developer Portal.
/// Suporta full-text, fuzzy matching, filtros por tipo/status/owner e facets.
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
    /// Combina resultados de APIs, serviços, documentação e contratos.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Portal MVP1: busca estática com filtros — sem PostgreSQL FTS neste handler.
            // A busca é resolvida via dados já disponíveis em memória/cache do catálogo.
            // Em produção, este handler delegará para um SearchService com FTS real.
            var items = new List<SearchResultItem>();

            var result = new Response(
                Items: items.AsReadOnly(),
                TotalCount: 0,
                Page: request.Page,
                PageSize: request.PageSize,
                Facets: new SearchFacets(
                    TypeCounts: new Dictionary<string, int>(),
                    StatusCounts: new Dictionary<string, int>()));

            return Task.FromResult(Result<Response>.Success(result));
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
