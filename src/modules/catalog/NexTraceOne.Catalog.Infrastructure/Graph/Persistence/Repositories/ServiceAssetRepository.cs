using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class ServiceAssetRepository(CatalogGraphDbContext context)
    : RepositoryBase<ServiceAsset, ServiceAssetId>(context), IServiceAssetRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<ServiceAsset?> GetByIdAsync(ServiceAssetId id, CancellationToken ct = default)
        => await _context.ServiceAssets.SingleOrDefaultAsync(svc => svc.Id == id, ct);

    public async Task<ServiceAsset?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await _context.ServiceAssets.SingleOrDefaultAsync(svc => svc.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListAllAsync(CancellationToken cancellationToken)
        => await _context.ServiceAssets.ToListAsync(cancellationToken);
}
