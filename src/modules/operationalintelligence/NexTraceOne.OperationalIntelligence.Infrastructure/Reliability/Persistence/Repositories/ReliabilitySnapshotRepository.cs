using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para ReliabilitySnapshot.
/// </summary>
internal sealed class ReliabilitySnapshotRepository(ReliabilityDbContext context)
    : IReliabilitySnapshotRepository
{
    public async Task<IReadOnlyList<ReliabilitySnapshot>> GetHistoryAsync(
        string serviceId, Guid tenantId, int maxCount, CancellationToken ct)
        => await context.ReliabilitySnapshots
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .Take(maxCount)
            .ToListAsync(ct);

    public async Task AddAsync(ReliabilitySnapshot snapshot, CancellationToken ct)
    {
        await context.ReliabilitySnapshots.AddAsync(snapshot, ct);
        await context.CommitAsync(ct);
    }

    public async Task<ReliabilitySnapshot?> GetLatestAsync(
        string serviceId, Guid tenantId, CancellationToken ct)
        => await context.ReliabilitySnapshots
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(ct);
}
