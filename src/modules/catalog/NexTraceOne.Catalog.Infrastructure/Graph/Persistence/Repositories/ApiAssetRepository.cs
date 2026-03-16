using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class ApiAssetRepository(CatalogGraphDbContext context)
    : RepositoryBase<ApiAsset, ApiAssetId>(context), IApiAssetRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<ApiAsset?> GetByIdAsync(ApiAssetId id, CancellationToken ct = default)
        => await IncludeGraph(_context.ApiAssets)
            .SingleOrDefaultAsync(asset => asset.Id == id, ct);

    public async Task<ApiAsset?> GetByNameAndOwnerAsync(string name, ServiceAssetId ownerServiceId, CancellationToken cancellationToken)
        => await IncludeGraph(_context.ApiAssets)
            .SingleOrDefaultAsync(
                asset => asset.Name == name && asset.OwnerService.Id == ownerServiceId,
                cancellationToken);

    public async Task<IReadOnlyList<ApiAsset>> ListAllAsync(CancellationToken cancellationToken)
        => await IncludeGraph(_context.ApiAssets).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApiAsset>> ListByServiceIdAsync(ServiceAssetId serviceId, CancellationToken cancellationToken)
        => await IncludeGraph(_context.ApiAssets)
            .Where(asset => asset.OwnerService.Id == serviceId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApiAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
        => await IncludeGraph(_context.ApiAssets)
            .Where(asset =>
                EF.Functions.ILike(asset.Name, $"%{searchTerm}%") ||
                EF.Functions.ILike(asset.RoutePattern, $"%{searchTerm}%"))
            .ToListAsync(cancellationToken);

    private static IQueryable<ApiAsset> IncludeGraph(IQueryable<ApiAsset> query)
        => query
            .Include(asset => asset.OwnerService)
            .Include(asset => asset.ConsumerRelationships)
            .Include(asset => asset.DiscoverySources);
}
