using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de CostAttribution usando EF Core.
/// </summary>
internal sealed class CostAttributionRepository(GovernanceDbContext context)
    : ICostAttributionRepository
{
    public async Task<CostAttribution?> GetByIdAsync(
        CostAttributionId id, CancellationToken ct)
        => await context.CostAttributions.SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<CostAttribution>> ListByDimensionAsync(
        CostAttributionDimension dimension,
        DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd,
        CancellationToken ct)
    {
        var query = context.CostAttributions
            .Where(a => a.Dimension == dimension);

        if (periodStart.HasValue)
            query = query.Where(a => a.PeriodStart >= periodStart.Value);

        if (periodEnd.HasValue)
            query = query.Where(a => a.PeriodEnd <= periodEnd.Value);

        return await query
            .OrderByDescending(a => a.TotalCost)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CostAttribution>> GetTopByDimensionAsync(
        CostAttributionDimension dimension,
        int top,
        DateTimeOffset? periodEnd,
        CancellationToken ct)
    {
        var query = context.CostAttributions
            .Where(a => a.Dimension == dimension);

        if (periodEnd.HasValue)
            query = query.Where(a => a.PeriodEnd <= periodEnd.Value);

        return await query
            .OrderByDescending(a => a.TotalCost)
            .Take(top)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(CostAttribution attribution, CancellationToken ct)
        => await context.CostAttributions.AddAsync(attribution, ct);
}
