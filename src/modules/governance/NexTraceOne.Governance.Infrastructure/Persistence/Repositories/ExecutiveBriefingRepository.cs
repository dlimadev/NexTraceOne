using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ExecutiveBriefing usando EF Core.
/// </summary>
internal sealed class ExecutiveBriefingRepository(GovernanceDbContext context)
    : IExecutiveBriefingRepository
{
    public async Task<ExecutiveBriefing?> GetByIdAsync(
        ExecutiveBriefingId id, CancellationToken ct)
        => await context.ExecutiveBriefings.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<ExecutiveBriefing>> ListAsync(
        BriefingFrequency? frequency, BriefingStatus? status, CancellationToken ct)
    {
        var query = context.ExecutiveBriefings.AsQueryable();

        if (frequency.HasValue)
            query = query.Where(b => b.Frequency == frequency.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .OrderByDescending(b => b.GeneratedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(ExecutiveBriefing briefing, CancellationToken ct)
        => await context.ExecutiveBriefings.AddAsync(briefing, ct);

    public Task UpdateAsync(ExecutiveBriefing briefing, CancellationToken ct)
    {
        context.ExecutiveBriefings.Update(briefing);
        return Task.CompletedTask;
    }
}
