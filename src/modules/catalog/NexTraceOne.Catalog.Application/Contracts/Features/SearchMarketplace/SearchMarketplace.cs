using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SearchMarketplace;

/// <summary>
/// Feature: SearchMarketplace — pesquisa listagens no marketplace interno de contratos
/// por categoria e/ou estado, incluindo listagens promovidas.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class SearchMarketplace
{
    /// <summary>Query de pesquisa de listagens no marketplace com filtros opcionais.</summary>
    public sealed record Query(
        string? Category,
        MarketplaceListingStatus? Status) : IQuery<Response>;

    /// <summary>Valida os parâmetros de pesquisa do marketplace.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        }
    }

    /// <summary>
    /// Handler que executa a pesquisa de listagens no marketplace interno.
    /// </summary>
    public sealed class Handler(IContractListingRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = await repository.SearchAsync(request.Category, request.Status, cancellationToken);

            var summaries = items
                .Select(l => new ListingSummary(
                    l.Id.Value,
                    l.ContractId,
                    l.Category,
                    l.Status,
                    l.ConsumerCount,
                    l.Rating,
                    l.TotalReviews,
                    l.IsPromoted,
                    l.PublishedAt))
                .ToList()
                .AsReadOnly();

            return new Response(summaries);
        }
    }

    /// <summary>Resumo de uma listagem do marketplace.</summary>
    public sealed record ListingSummary(
        Guid ListingId,
        string ContractId,
        string Category,
        MarketplaceListingStatus Status,
        int ConsumerCount,
        decimal Rating,
        int TotalReviews,
        bool IsPromoted,
        DateTimeOffset PublishedAt);

    /// <summary>Resposta da pesquisa no marketplace.</summary>
    public sealed record Response(IReadOnlyList<ListingSummary> Items);
}
