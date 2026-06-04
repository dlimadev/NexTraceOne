using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Knowledge.Abstractions;
using NexTraceOne.Catalog.Domain.Knowledge.Entities;
using NexTraceOne.Catalog.Domain.Knowledge.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Knowledge.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para snapshots do knowledge graph (KnowledgeGraphSnapshot).
/// </summary>
internal sealed class KnowledgeGraphSnapshotRepository(ServiceCatalogDbContext context)
    : IKnowledgeGraphSnapshotRepository
{
    public async Task<KnowledgeGraphSnapshot?> GetByIdAsync(KnowledgeGraphSnapshotId id, CancellationToken ct)
        => await context.KnowledgeGraphSnapshots
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<KnowledgeGraphSnapshot>> ListAsync(
        KnowledgeGraphSnapshotStatus? status, CancellationToken ct)
    {
        var query = context.KnowledgeGraphSnapshots.AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderByDescending(s => s.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<KnowledgeGraphSnapshot?> GetLatestAsync(CancellationToken ct)
        => await context.KnowledgeGraphSnapshots
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync(ct);

    public void Add(KnowledgeGraphSnapshot snapshot)
        => context.KnowledgeGraphSnapshots.Add(snapshot);

    public void Update(KnowledgeGraphSnapshot snapshot)
        => context.KnowledgeGraphSnapshots.Update(snapshot);
}
