using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>Repositório EF Core para ProductivitySnapshot.</summary>
internal sealed class ProductivitySnapshotRepository(ServiceCatalogDbContext context)
    : IProductivitySnapshotRepository
{
    public async Task<IReadOnlyList<ProductivitySnapshot>> ListByTeamAsync(
        string teamId, DateTimeOffset? from, CancellationToken ct)
    {
        var query = context.ProductivitySnapshots.AsNoTracking().Where(s => s.TeamId == teamId);
        if (from.HasValue) query = query.Where(s => s.PeriodStart >= from.Value);
        return await query.OrderByDescending(s => s.PeriodStart).ToListAsync(ct);
    }

    public void Add(ProductivitySnapshot snapshot) => context.ProductivitySnapshots.Add(snapshot);
}
