using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para BurnRateSnapshot.</summary>
internal sealed class BurnRateSnapshotRepository(ReliabilityDbContext context) : IBurnRateSnapshotRepository
{
    public async Task<BurnRateSnapshot?> GetLatestAsync(SloDefinitionId sloId, BurnRateWindow window, Guid tenantId, CancellationToken ct)
        => await context.BurnRateSnapshots
            .AsNoTracking()
            .Where(s => s.SloDefinitionId == sloId && s.Window == window && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BurnRateSnapshot>> GetHistoryAsync(SloDefinitionId sloId, Guid tenantId, int maxCount, CancellationToken ct)
        => await context.BurnRateSnapshots
            .AsNoTracking()
            .Where(s => s.SloDefinitionId == sloId && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .Take(maxCount)
            .ToListAsync(ct);

    public async Task AddAsync(BurnRateSnapshot snapshot, CancellationToken ct)
    {
        await context.BurnRateSnapshots.AddAsync(snapshot, ct);
        await context.CommitAsync(ct);
    }
}
