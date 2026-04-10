using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para operações de persistência de avaliações do marketplace interno de contratos.
/// </summary>
public interface IMarketplaceReviewRepository
{
    /// <summary>Obtém uma avaliação por identificador.</summary>
    Task<MarketplaceReview?> GetByIdAsync(MarketplaceReviewId id, CancellationToken cancellationToken);

    /// <summary>Lista avaliações de uma listagem do marketplace.</summary>
    Task<IReadOnlyList<MarketplaceReview>> ListByListingAsync(ContractListingId listingId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova avaliação ao repositório.</summary>
    Task AddAsync(MarketplaceReview review, CancellationToken cancellationToken);
}
