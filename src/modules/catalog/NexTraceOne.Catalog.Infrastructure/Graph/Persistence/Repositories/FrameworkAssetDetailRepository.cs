using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class FrameworkAssetDetailRepository(CatalogGraphDbContext context)
    : RepositoryBase<FrameworkAssetDetail, FrameworkAssetDetailId>(context), IFrameworkAssetDetailRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public async Task<FrameworkAssetDetail?> GetByServiceAssetIdAsync(
        ServiceAssetId serviceAssetId, CancellationToken cancellationToken)
        => await _context.FrameworkAssetDetails
            .SingleOrDefaultAsync(f => f.ServiceAssetId == serviceAssetId, cancellationToken);
}
