using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de janelas de freeze para restrição de mudanças em períodos críticos.
/// </summary>
internal sealed class FreezeWindowRepository(ChangeIntelligenceDbContext context) : IFreezeWindowRepository
{
    /// <summary>Busca uma janela de freeze pelo identificador.</summary>
    public async Task<FreezeWindow?> GetByIdAsync(FreezeWindowId id, CancellationToken cancellationToken = default)
        => await context.FreezeWindows
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);

    /// <summary>Lista janelas de freeze ativas num determinado momento.</summary>
    public async Task<IReadOnlyList<FreezeWindow>> ListActiveAtAsync(DateTimeOffset at, CancellationToken cancellationToken = default)
        => await context.FreezeWindows
            .Where(w => w.IsActive && w.StartsAt <= at && w.EndsAt >= at)
            .OrderBy(w => w.StartsAt)
            .ToListAsync(cancellationToken);

    /// <summary>Lista todas as janelas de freeze com paginação.</summary>
    public async Task<IReadOnlyList<FreezeWindow>> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.FreezeWindows
            .OrderByDescending(w => w.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona uma janela de freeze.</summary>
    public void Add(FreezeWindow window)
        => context.FreezeWindows.Add(window);
}
