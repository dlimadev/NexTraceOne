using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de GovernancePacks usando EF Core.
/// </summary>
internal sealed class GovernancePackRepository(GovernanceDbContext context) : IGovernancePackRepository
{
    public async Task<IReadOnlyList<GovernancePack>> ListAsync(
        GovernanceRuleCategory? category,
        GovernancePackStatus? status,
        CancellationToken ct)
    {
        var query = context.Packs.AsQueryable();

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return await query.OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<GovernancePack?> GetByIdAsync(GovernancePackId id, CancellationToken ct)
        => await context.Packs.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<GovernancePack?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Packs.SingleOrDefaultAsync(p => p.Name == name, ct);

    public async Task AddAsync(GovernancePack pack, CancellationToken ct)
        => await context.Packs.AddAsync(pack, ct);

    public Task UpdateAsync(GovernancePack pack, CancellationToken ct)
    {
        context.Packs.Update(pack);
        return Task.CompletedTask;
    }
}
