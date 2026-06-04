using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Repositório de snapshots temporais do grafo de engenharia.
/// Suporta consultas por Id, listagem ordenada e obtenção do mais recente.
/// </summary>
internal sealed class GraphSnapshotRepository(ServiceCatalogDbContext context)
    : RepositoryBase<GraphSnapshot, GraphSnapshotId>(context), IGraphSnapshotRepository
{
    private readonly ServiceCatalogDbContext _context = context;

    public override async Task<GraphSnapshot?> GetByIdAsync(GraphSnapshotId id, CancellationToken ct = default)
        => await _context.GraphSnapshots.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<GraphSnapshot>> ListAsync(int limit, CancellationToken cancellationToken)
        => await _context.GraphSnapshots
            .OrderByDescending(s => s.CapturedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<GraphSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
        => await _context.GraphSnapshots
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
