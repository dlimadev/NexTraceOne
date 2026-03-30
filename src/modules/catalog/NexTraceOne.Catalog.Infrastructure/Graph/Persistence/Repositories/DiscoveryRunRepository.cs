using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de execuções de discovery.
/// </summary>
internal sealed class DiscoveryRunRepository(CatalogGraphDbContext dbContext) : IDiscoveryRunRepository
{
    public async Task<DiscoveryRun?> GetByIdAsync(DiscoveryRunId id, CancellationToken cancellationToken)
        => await dbContext.DiscoveryRuns
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiscoveryRun>> ListRecentAsync(int top, CancellationToken cancellationToken)
        => await dbContext.DiscoveryRuns
            .OrderByDescending(x => x.StartedAt)
            .Take(top)
            .ToListAsync(cancellationToken);

    public void Add(DiscoveryRun run)
        => dbContext.DiscoveryRuns.Add(run);
}
