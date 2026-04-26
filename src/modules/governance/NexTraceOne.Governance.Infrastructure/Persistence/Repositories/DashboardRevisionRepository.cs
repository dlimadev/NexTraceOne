using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de DashboardRevision (V3.1).
/// </summary>
internal sealed class DashboardRevisionRepository(GovernanceDbContext context) : IDashboardRevisionRepository
{
    public async Task<IReadOnlyList<DashboardRevision>> ListByDashboardIdAsync(
        CustomDashboardId dashboardId,
        int maxResults,
        CancellationToken ct)
        => await context.DashboardRevisions
            .Where(r => r.DashboardId == dashboardId)
            .OrderByDescending(r => r.RevisionNumber)
            .Take(maxResults)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<DashboardRevision?> GetByRevisionNumberAsync(
        CustomDashboardId dashboardId,
        int revisionNumber,
        CancellationToken ct)
        => await context.DashboardRevisions
            .Where(r => r.DashboardId == dashboardId && r.RevisionNumber == revisionNumber)
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

    public async Task<int> CountByDashboardIdAsync(CustomDashboardId dashboardId, CancellationToken ct)
        => await context.DashboardRevisions
            .CountAsync(r => r.DashboardId == dashboardId, ct);

    public async Task AddAsync(DashboardRevision revision, CancellationToken ct)
        => await context.DashboardRevisions.AddAsync(revision, ct);
}
