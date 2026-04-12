using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractListing;

/// <summary>
/// Feature: GetContractListing — obtém uma listagem do marketplace por identificador.
/// Retorna metadados completos incluindo métricas de adoção e estado de publicação.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractListing
{
    /// <summary>Query para obter uma listagem do marketplace por Id.</summary>
    public sealed record Query(Guid ListingId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ListingId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém a listagem do marketplace por Id.
    /// </summary>
    public sealed class Handler(IContractListingRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var listing = await repository.GetByIdAsync(
                ContractListingId.From(request.ListingId), cancellationToken);

            if (listing is null)
                return ContractsErrors.ContractListingNotFound(request.ListingId.ToString());

            return new Response(
                listing.Id.Value,
                listing.ContractId,
                listing.Category,
                listing.Tags,
                listing.ConsumerCount,
                listing.Rating,
                listing.TotalReviews,
                listing.IsPromoted,
                listing.Description,
                listing.Status,
                listing.PublishedBy,
                listing.PublishedAt,
                listing.TenantId);
        }
    }

    /// <summary>Resposta completa de uma listagem do marketplace.</summary>
    public sealed record Response(
        Guid ListingId,
        string ContractId,
        string Category,
        string? Tags,
        int ConsumerCount,
        decimal Rating,
        int TotalReviews,
        bool IsPromoted,
        string? Description,
        MarketplaceListingStatus Status,
        string? PublishedBy,
        DateTimeOffset PublishedAt,
        string? TenantId);
}
