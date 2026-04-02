using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class ServiceMetricsRepository(TelemetryStoreDbContext context)
    : IServiceMetricsWriter, IServiceMetricsReader
{
    public async Task WriteAsync(ServiceMetricsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        context.ServiceMetricsSnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task WriteBatchAsync(
        IReadOnlyList<ServiceMetricsSnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        context.ServiceMetricsSnapshots.AddRange(snapshots);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceMetricsSnapshot>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        AggregationLevel? level = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.ServiceMetricsSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId
                        && s.Environment == environment
                        && s.IntervalStart >= from
                        && s.IntervalStart <= until);

        if (level.HasValue)
        {
            query = query.Where(s => s.AggregationLevel == level.Value);
        }

        return await query
            .OrderBy(s => s.IntervalStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceMetricsSnapshot?> GetLatestAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.ServiceMetricsSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.Environment == environment)
            .OrderByDescending(s => s.IntervalStart)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceMetricsSnapshot>> GetTopServicesAsync(
        string environment,
        string orderByMetric,
        int top,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var latestPerService = context.ServiceMetricsSnapshots
            .AsNoTracking()
            .Where(s => s.Environment == environment
                        && s.IntervalStart >= from
                        && s.IntervalStart <= until)
            .GroupBy(s => s.ServiceId)
            .Select(g => g.OrderByDescending(s => s.IntervalStart).First());

        var ordered = orderByMetric.ToLowerInvariant() switch
        {
            "errorrate" or "errorratepercent" => latestPerService.OrderByDescending(s => s.ErrorRatePercent),
            "latencyp95" or "latencyp95ms" => latestPerService.OrderByDescending(s => s.LatencyP95Ms),
            "latencyp99" or "latencyp99ms" => latestPerService.OrderByDescending(s => s.LatencyP99Ms),
            "latencyavg" or "latencyavgms" => latestPerService.OrderByDescending(s => s.LatencyAvgMs),
            "requestcount" => latestPerService.OrderByDescending(s => s.RequestCount),
            _ => latestPerService.OrderByDescending(s => s.RequestsPerMinute),
        };

        return await ordered
            .Take(top)
            .ToListAsync(cancellationToken);
    }
}
