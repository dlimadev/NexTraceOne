using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de visões salvas do grafo de engenharia.
/// Suporta consultas por Id e listagem por proprietário incluindo visões compartilhadas.
/// </summary>
internal sealed class SavedGraphViewRepository(EngineeringGraphDbContext context)
    : RepositoryBase<SavedGraphView, SavedGraphViewId>(context), ISavedGraphViewRepository
{
    private readonly EngineeringGraphDbContext _context = context;

    public override async Task<SavedGraphView?> GetByIdAsync(SavedGraphViewId id, CancellationToken ct = default)
        => await _context.SavedGraphViews.SingleOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<SavedGraphView>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken)
        => await _context.SavedGraphViews
            .Where(v => v.OwnerId == ownerId || v.IsShared)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
}
