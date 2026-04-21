using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório EF Core de ReleaseCalendarEntry.
/// Suporta listagem por tenant, tipo, estado e intervalo temporal.
/// Wave F.1 — Release Calendar.
/// </summary>
internal sealed class ReleaseCalendarRepository(ChangeIntelligenceDbContext context)
    : IReleaseCalendarRepository
{
    public Task<ReleaseCalendarEntry?> GetByIdAsync(ReleaseCalendarEntryId id, CancellationToken ct = default)
        => context.ReleaseCalendarEntries
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

    public async Task<IReadOnlyList<ReleaseCalendarEntry>> ListAsync(
        string tenantId,
        ReleaseWindowStatus? status = null,
        ReleaseWindowType? windowType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = context.ReleaseCalendarEntries
            .Where(e => e.TenantId == tenantId && !e.IsDeleted);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (windowType.HasValue)
            query = query.Where(e => e.WindowType == windowType.Value);

        if (from.HasValue)
            query = query.Where(e => e.EndsAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartsAt <= to.Value);

        return await query
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReleaseCalendarEntry>> ListActiveAtAsync(
        string tenantId,
        DateTimeOffset moment,
        string? environment = null,
        CancellationToken ct = default)
    {
        var query = context.ReleaseCalendarEntries
            .Where(e => e.TenantId == tenantId
                     && !e.IsDeleted
                     && e.Status == ReleaseWindowStatus.Active
                     && e.StartsAt <= moment
                     && e.EndsAt >= moment);

        if (environment is not null)
        {
            var env = environment.Trim().ToLowerInvariant();
            query = query.Where(e => e.EnvironmentFilter == null || e.EnvironmentFilter == env);
        }

        return await query.ToListAsync(ct);
    }

    public void Add(ReleaseCalendarEntry entry)
        => context.ReleaseCalendarEntries.Add(entry);

    public void Update(ReleaseCalendarEntry entry)
        => context.ReleaseCalendarEntries.Update(entry);
}
