using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de estados de deployment.
/// </summary>
internal sealed class AssetDeploymentStateRepository(ServiceCatalogDbContext dbContext)
    : IAssetDeploymentStateRepository
{
    public Task<AssetDeploymentState?> GetByServiceAndEnvironmentAsync(
        ServiceAssetId serviceAssetId,
        string environment,
        CancellationToken cancellationToken)
        => dbContext.AssetDeploymentStates
            .FirstOrDefaultAsync(
                x => x.ServiceAssetId == serviceAssetId
                     && x.Environment == environment,
                cancellationToken);

    public async Task<IReadOnlyList<AssetDeploymentState>> ListByServiceAsync(
        ServiceAssetId serviceAssetId,
        CancellationToken cancellationToken)
        => await dbContext.AssetDeploymentStates
            .Where(x => x.ServiceAssetId == serviceAssetId)
            .OrderBy(x => x.Environment)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AssetDeploymentState>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await dbContext.AssetDeploymentStates
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Environment)
            .ThenBy(x => x.ServiceAssetId)
            .ToListAsync(cancellationToken);

    public void Add(AssetDeploymentState state)
        => dbContext.AssetDeploymentStates.Add(state);
}
