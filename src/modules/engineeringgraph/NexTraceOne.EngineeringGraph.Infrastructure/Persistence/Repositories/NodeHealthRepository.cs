using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Enums;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de registros de saúde/métricas de nós do grafo.
/// Suporta consultas por overlay e por nó individual para alimentar overlays explicáveis.
/// </summary>
internal sealed class NodeHealthRepository(EngineeringGraphDbContext context)
    : RepositoryBase<NodeHealthRecord, NodeHealthRecordId>(context), INodeHealthRepository
{
    private readonly EngineeringGraphDbContext _context = context;

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
