using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de GovernanceWaivers usando EF Core.
/// </summary>
internal sealed class GovernanceWaiverRepository(GovernanceDbContext context) : IGovernanceWaiverRepository
{
    public async Task<IReadOnlyList<GovernanceWaiver>> ListAsync(
        GovernancePackId? packId,
        WaiverStatus? status,
        CancellationToken ct)
    {
        var query = context.Waivers.AsQueryable();

        if (packId is not null)
            query = query.Where(w => w.PackId == packId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query.OrderByDescending(w => w.RequestedAt).ToListAsync(ct);
    }

    public async Task<GovernanceWaiver?> GetByIdAsync(GovernanceWaiverId id, CancellationToken ct)
        => await context.Waivers.SingleOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddAsync(GovernanceWaiver waiver, CancellationToken ct)
        => await context.Waivers.AddAsync(waiver, ct);

    public Task UpdateAsync(GovernanceWaiver waiver, CancellationToken ct)
    {
        context.Waivers.Update(waiver);
        return Task.CompletedTask;
    }
}
