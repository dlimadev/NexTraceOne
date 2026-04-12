using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

internal sealed class ChangeCostImpactRepository(GovernanceDbContext context)
    : IChangeCostImpactRepository
{
    public async Task<ChangeCostImpact?> GetByIdAsync(
        ChangeCostImpactId id, CancellationToken ct)
        => await context.ChangeCostImpacts.SingleOrDefaultAsync(i => i.Id == id, ct);

    public async Task<ChangeCostImpact?> GetByReleaseIdAsync(
        Guid releaseId, CancellationToken ct)
        => await context.ChangeCostImpacts.SingleOrDefaultAsync(i => i.ReleaseId == releaseId, ct);

    public async Task<IReadOnlyList<ChangeCostImpact>> ListCostliestAsync(
        int top, CancellationToken ct)
        => await context.ChangeCostImpacts
            .OrderByDescending(i => Math.Abs(i.CostDelta))
            .Take(top)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ChangeCostImpact>> ListByServiceAsync(
        string serviceName, CancellationToken ct)
        => await context.ChangeCostImpacts
            .Where(i => i.ServiceName == serviceName)
            .OrderByDescending(i => i.RecordedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(ChangeCostImpact impact, CancellationToken ct)
        => await context.ChangeCostImpacts.AddAsync(impact, ct);
}
