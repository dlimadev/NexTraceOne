using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class FrameworkAssetDetailRepository(ServiceCatalogDbContext context)
    : RepositoryBase<FrameworkAssetDetail, FrameworkAssetDetailId>(context), IFrameworkAssetDetailRepository
{
    private readonly ServiceCatalogDbContext _context = context;

    public async Task<FrameworkAssetDetail?> GetByServiceAssetIdAsync(
        ServiceAssetId serviceAssetId, CancellationToken cancellationToken)
        => await _context.FrameworkAssetDetails
            .SingleOrDefaultAsync(f => f.ServiceAssetId == serviceAssetId, cancellationToken);
}
