using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class AnomalySnapshotRepository(TelemetryStoreDbContext context)
    : IAnomalySnapshotWriter, IAnomalySnapshotReader
{
    public async Task WriteAsync(AnomalySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        context.AnomalySnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveAsync(Guid anomalyId, DateTimeOffset resolvedAt, CancellationToken cancellationToken = default)
    {
        var anomaly = await context.AnomalySnapshots.FindAsync([anomalyId], cancellationToken);

        if (anomaly is null)
        {
            return;
        }

        context.Entry(anomaly).CurrentValues.SetValues(anomaly with { ResolvedAt = resolvedAt });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnomalySnapshot>> GetActiveByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.AnomalySnapshots
            .AsNoTracking()
            .Where(a => a.ServiceId == serviceId
                        && a.Environment == environment
                        && a.ResolvedAt == null)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnomalySnapshot>> GetByTimeRangeAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.AnomalySnapshots
            .AsNoTracking()
            .Where(a => a.Environment == environment
                        && a.DetectedAt >= from
                        && a.DetectedAt <= until)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(cancellationToken);
    }
}
