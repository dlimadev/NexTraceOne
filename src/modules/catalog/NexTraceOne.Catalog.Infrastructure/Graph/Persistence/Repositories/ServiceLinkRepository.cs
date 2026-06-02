using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class ServiceLinkRepository(ServiceCatalogDbContext context) : IServiceLinkRepository
{
    private readonly ServiceCatalogDbContext _context = context;

    public async Task<ServiceLink?> GetByIdAsync(ServiceLinkId id, CancellationToken cancellationToken)
        => await _context.ServiceLinks.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ServiceLink>> ListByServiceAsync(
        ServiceAssetId serviceAssetId,
        CancellationToken cancellationToken)
        => await _context.ServiceLinks
            .Where(l => l.ServiceAssetId == serviceAssetId)
            .OrderBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .ThenBy(l => l.Title)
            .ToListAsync(cancellationToken);

    public void Add(ServiceLink link)
        => _context.ServiceLinks.Add(link);

    public void Remove(ServiceLink link)
        => _context.ServiceLinks.Remove(link);
}
