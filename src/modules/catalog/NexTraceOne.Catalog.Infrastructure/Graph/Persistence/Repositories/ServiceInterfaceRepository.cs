using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de interfaces de serviço usando CatalogGraphDbContext.
/// </summary>
internal sealed class ServiceInterfaceRepository(CatalogGraphDbContext context)
    : RepositoryBase<ServiceInterface, ServiceInterfaceId>(context), IServiceInterfaceRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<ServiceInterface?> GetByIdAsync(ServiceInterfaceId id, CancellationToken ct = default)
        => await _context.ServiceInterfaces.SingleOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<ServiceInterface>> ListByServiceAsync(Guid serviceAssetId, CancellationToken ct)
        => await _context.ServiceInterfaces
            .AsNoTracking()
            .Where(i => i.ServiceAssetId == ServiceAssetId.From(serviceAssetId) && !i.IsDeleted)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
}
