using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de Technical Debt Items usando EF Core.
/// </summary>
internal sealed class TechnicalDebtRepository(GovernanceDbContext context) : ITechnicalDebtRepository
{
    public async Task<IReadOnlyList<TechnicalDebtItem>> ListAsync(
        string? serviceName,
        string? debtType,
        int topN,
        CancellationToken ct)
    {
        var query = context.TechnicalDebtItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(d => d.ServiceName == serviceName);

        if (!string.IsNullOrWhiteSpace(debtType))
            query = query.Where(d => d.DebtType == debtType);

        return await query
            .OrderByDescending(d => d.DebtScore)
            .ThenByDescending(d => d.CreatedAt)
            .Take(topN)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<TechnicalDebtItem?> GetByIdAsync(TechnicalDebtItemId id, CancellationToken ct)
        => await context.TechnicalDebtItems.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(TechnicalDebtItem item, CancellationToken ct)
        => await context.TechnicalDebtItems.AddAsync(item, ct);

    public Task UpdateAsync(TechnicalDebtItem item, CancellationToken ct)
    {
        context.TechnicalDebtItems.Update(item);
        return Task.CompletedTask;
    }
}
