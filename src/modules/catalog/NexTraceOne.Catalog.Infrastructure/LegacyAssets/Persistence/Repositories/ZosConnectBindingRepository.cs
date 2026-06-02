using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class ZosConnectBindingRepository(ServiceCatalogDbContext context)
    : RepositoryBase<ZosConnectBinding, ZosConnectBindingId>(context), IZosConnectBindingRepository
{
    private readonly ServiceCatalogDbContext _context = context;

    public override async Task<ZosConnectBinding?> GetByIdAsync(ZosConnectBindingId id, CancellationToken ct = default)
        => await _context.ZosConnectBindings.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<ZosConnectBinding?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.ZosConnectBindings
            .SingleOrDefaultAsync(b => b.Name == name && b.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<ZosConnectBinding>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.ZosConnectBindings
            .Where(b => b.SystemId == systemId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
}
