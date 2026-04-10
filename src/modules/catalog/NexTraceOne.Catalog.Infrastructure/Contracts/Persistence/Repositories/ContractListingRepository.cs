using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de listagens do marketplace interno de contratos.
/// Persiste e consulta publicações com metadados de descoberta e classificação.
/// </summary>
internal sealed class ContractListingRepository(ContractsDbContext context)
    : IContractListingRepository
{
    /// <inheritdoc />
    public async Task<ContractListing?> GetByIdAsync(ContractListingId id, CancellationToken cancellationToken)
        => await context.ContractListings
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractListing>> SearchAsync(string? category, MarketplaceListingStatus? status, CancellationToken cancellationToken)
    {
        var query = context.ContractListings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractListing>> ListPromotedAsync(CancellationToken cancellationToken)
        => await context.ContractListings
            .AsNoTracking()
            .Where(x => x.IsPromoted && x.Status == MarketplaceListingStatus.Published)
            .OrderByDescending(x => x.PublishedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ContractListing listing, CancellationToken cancellationToken)
        => await context.ContractListings.AddAsync(listing, cancellationToken);
}
