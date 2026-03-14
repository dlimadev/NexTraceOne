using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Repositório de registros de saúde/métricas de nós do grafo.
/// Suporta consultas por overlay e por nó individual para alimentar overlays explicáveis.
/// </summary>
internal sealed class NodeHealthRepository(CatalogGraphDbContext context)
    : RepositoryBase<NodeHealthRecord, NodeHealthRecordId>(context), INodeHealthRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public async Task<IReadOnlyList<NodeHealthRecord>> GetLatestByOverlayAsync(
        OverlayMode overlayMode, CancellationToken cancellationToken)
    {
        return await _context.NodeHealthRecords
            .Where(r => r.OverlayMode == overlayMode)
            .GroupBy(r => r.NodeId)
            .Select(g => g.OrderByDescending(r => r.CalculatedAt).First())
            .ToListAsync(cancellationToken);
    }

    public async Task<NodeHealthRecord?> GetByNodeAsync(
        Guid nodeId, OverlayMode overlayMode, CancellationToken cancellationToken)
    {
        return await _context.NodeHealthRecords
            .Where(r => r.NodeId == nodeId && r.OverlayMode == overlayMode)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
