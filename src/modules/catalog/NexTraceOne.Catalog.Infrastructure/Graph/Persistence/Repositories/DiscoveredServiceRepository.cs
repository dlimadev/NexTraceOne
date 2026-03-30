using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de serviços descobertos.
/// </summary>
internal sealed class DiscoveredServiceRepository(CatalogGraphDbContext dbContext) : IDiscoveredServiceRepository
{
    public async Task<DiscoveredService?> GetByIdAsync(DiscoveredServiceId id, CancellationToken cancellationToken)
        => await dbContext.DiscoveredServices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DiscoveredService?> GetByNameAndEnvironmentAsync(string serviceName, string environment, CancellationToken cancellationToken)
        => await dbContext.DiscoveredServices
            .FirstOrDefaultAsync(x => x.ServiceName == serviceName && x.Environment == environment, cancellationToken);

    public async Task<IReadOnlyList<DiscoveredService>> ListFilteredAsync(
        DiscoveryStatus? status,
        string? environment,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DiscoveredServices.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            query = query.Where(x => x.Environment == environment);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(x =>
                x.ServiceName.ToLower().Contains(term) ||
                x.ServiceNamespace.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(x => x.LastSeenAt)
            .ThenBy(x => x.ServiceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(DiscoveryStatus status, CancellationToken cancellationToken)
        => await dbContext.DiscoveredServices.CountAsync(x => x.Status == status, cancellationToken);

    public async Task<int> CountNewSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
        => await dbContext.DiscoveredServices.CountAsync(
            x => x.Status == DiscoveryStatus.Pending && x.FirstSeenAt >= since, cancellationToken);

    public void Add(DiscoveredService discoveredService)
        => dbContext.DiscoveredServices.Add(discoveredService);
}
