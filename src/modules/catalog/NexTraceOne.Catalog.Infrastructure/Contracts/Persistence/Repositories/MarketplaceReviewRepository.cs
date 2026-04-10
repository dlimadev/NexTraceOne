using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de avaliações do marketplace interno de contratos.
/// Persiste e consulta avaliações com rating e comentário por listagem.
/// </summary>
internal sealed class MarketplaceReviewRepository(ContractsDbContext context)
    : IMarketplaceReviewRepository
{
    /// <inheritdoc />
    public async Task<MarketplaceReview?> GetByIdAsync(MarketplaceReviewId id, CancellationToken cancellationToken)
        => await context.MarketplaceReviews
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarketplaceReview>> ListByListingAsync(ContractListingId listingId, CancellationToken cancellationToken)
        => await context.MarketplaceReviews
            .AsNoTracking()
            .Where(x => x.ListingId == listingId)
            .OrderByDescending(x => x.ReviewedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(MarketplaceReview review, CancellationToken cancellationToken)
        => await context.MarketplaceReviews.AddAsync(review, cancellationToken);
}
