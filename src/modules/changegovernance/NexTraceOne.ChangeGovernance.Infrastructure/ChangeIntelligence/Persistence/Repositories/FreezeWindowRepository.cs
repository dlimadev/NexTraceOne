using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

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

    /// <summary>Lista janelas de freeze que intersectam uma janela temporal, com filtros opcionais.</summary>
    public async Task<IReadOnlyList<FreezeWindow>> ListInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = context.FreezeWindows
            .Where(w => w.StartsAt <= to && w.EndsAt >= from);

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(w =>
                w.Scope == FreezeScope.Global ||
                (w.Scope == FreezeScope.Environment &&
                 w.ScopeValue != null &&
                 w.ScopeValue.ToLower() == environment.ToLower()));

        return await query
            .OrderBy(w => w.StartsAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Conta o total de janelas de freeze com filtros opcionais.</summary>
    public async Task<int> CountAsync(
        string? environment,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = context.FreezeWindows.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(w =>
                w.Scope == FreezeScope.Global ||
                (w.Scope == FreezeScope.Environment &&
                 w.ScopeValue != null &&
                 w.ScopeValue.ToLower() == environment.ToLower()));

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>Adiciona uma janela de freeze.</summary>
    public void Add(FreezeWindow window)
        => context.FreezeWindows.Add(window);

    /// <summary>Remove uma janela de freeze.</summary>
    public void Remove(FreezeWindow window)
        => context.FreezeWindows.Remove(window);
}
