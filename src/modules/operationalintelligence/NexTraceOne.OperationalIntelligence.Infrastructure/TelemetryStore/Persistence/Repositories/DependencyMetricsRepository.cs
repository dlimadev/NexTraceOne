using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class DependencyMetricsRepository(TelemetryStoreDbContext context)
    : IDependencyMetricsWriter, IDependencyMetricsReader
{
    public async Task WriteAsync(DependencyMetricsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        context.DependencyMetricsSnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task WriteBatchAsync(
        IReadOnlyList<DependencyMetricsSnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        context.DependencyMetricsSnapshots.AddRange(snapshots);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DependencyMetricsSnapshot>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.DependencyMetricsSnapshots
            .AsNoTracking()
            .Where(d => (d.SourceServiceId == serviceId || d.TargetServiceId == serviceId)
                        && d.Environment == environment
                        && d.IntervalStart >= from
                        && d.IntervalStart <= until)
            .OrderBy(d => d.IntervalStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DependencyMetricsSnapshot>> GetTopDependenciesAsync(
        string environment,
        string orderByMetric,
        int top,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var query = context.DependencyMetricsSnapshots
            .AsNoTracking()
            .Where(d => d.Environment == environment
                        && d.IntervalStart >= from
                        && d.IntervalStart <= until)
            .GroupBy(d => new { d.SourceServiceId, d.TargetServiceId })
            .Select(g => g.OrderByDescending(d => d.IntervalStart).First());

        var ordered = orderByMetric.ToLowerInvariant() switch
        {
            "errorrate" or "errorratepercent" => query.OrderByDescending(d => d.ErrorRatePercent),
            "latencyp95" or "latencyp95ms" => query.OrderByDescending(d => d.LatencyP95Ms),
            "latencyp99" or "latencyp99ms" => query.OrderByDescending(d => d.LatencyP99Ms),
            "latencyavg" or "latencyavgms" => query.OrderByDescending(d => d.LatencyAvgMs),
            _ => query.OrderByDescending(d => d.CallCount),
        };

        return await ordered
            .Take(top)
            .ToListAsync(cancellationToken);
    }
}
