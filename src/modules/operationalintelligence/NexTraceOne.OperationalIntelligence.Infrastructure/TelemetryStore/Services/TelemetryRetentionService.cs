using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Services;

/// <summary>
/// Implementação do serviço de retenção de telemetria.
/// Gere o ciclo de vida dos dados: consolidação de minuto para hora e purga de dados expirados.
/// Executado periodicamente via Quartz.NET background job.
/// </summary>
public sealed class TelemetryRetentionService : ITelemetryRetentionService
{
    private const int MinuteMetricsRetentionDays = 7;
    private const int HourlyMetricsRetentionDays = 90;
    private const int AnomalyRetentionDays = 180;
    private const int TelemetryReferenceRetentionDays = 30;
    private const int TopologyStaleDays = 30;

    private readonly TelemetryStoreDbContext _dbContext;
    private readonly ILogger<TelemetryRetentionService> _logger;

    public TelemetryRetentionService(
        TelemetryStoreDbContext dbContext,
        ILogger<TelemetryRetentionService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ConsolidateMinuteToHourlyAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting minute-to-hourly consolidation for target hour {TargetHour}",
            targetHour);

        try
        {
            var hourStart = new DateTimeOffset(
                targetHour.Year, targetHour.Month, targetHour.Day,
                targetHour.Hour, 0, 0, targetHour.Offset);
            var hourEnd = hourStart.AddHours(1);

            await ConsolidateServiceMetricsAsync(hourStart, hourEnd, cancellationToken);
            await ConsolidateDependencyMetricsAsync(hourStart, hourEnd, cancellationToken);

            _logger.LogInformation(
                "Completed minute-to-hourly consolidation for target hour {TargetHour}",
                targetHour);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error during minute-to-hourly consolidation for target hour {TargetHour}",
                targetHour);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeExpiredMinuteMetricsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting purge of expired minute metrics");

        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-MinuteMetricsRetentionDays);

            var deletedServices = await _dbContext.ServiceMetricsSnapshots
                .Where(s => s.AggregationLevel == AggregationLevel.OneMinute
                            && s.IntervalStart < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedDependencies = await _dbContext.DependencyMetricsSnapshots
                .Where(d => d.AggregationLevel == AggregationLevel.OneMinute
                            && d.IntervalStart < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Purged {ServiceCount} service and {DependencyCount} dependency expired minute metrics (cutoff: {Cutoff})",
                deletedServices, deletedDependencies, cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error purging expired minute metrics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeExpiredHourlyMetricsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting purge of expired hourly metrics");

        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-HourlyMetricsRetentionDays);

            var deletedServices = await _dbContext.ServiceMetricsSnapshots
                .Where(s => s.AggregationLevel == AggregationLevel.OneHour
                            && s.IntervalStart < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedDependencies = await _dbContext.DependencyMetricsSnapshots
                .Where(d => d.AggregationLevel == AggregationLevel.OneHour
                            && d.IntervalStart < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Purged {ServiceCount} service and {DependencyCount} dependency expired hourly metrics (cutoff: {Cutoff})",
                deletedServices, deletedDependencies, cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error purging expired hourly metrics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeExpiredAnomalySnapshotsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting purge of expired anomaly snapshots");

        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-AnomalyRetentionDays);

            var deleted = await _dbContext.AnomalySnapshots
                .Where(a => a.ResolvedAt != null && a.ResolvedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Purged {Count} expired resolved anomaly snapshots (cutoff: {Cutoff})",
                deleted, cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error purging expired anomaly snapshots");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeExpiredTelemetryReferencesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting purge of expired telemetry references");

        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-TelemetryReferenceRetentionDays);

            var deleted = await _dbContext.TelemetryReferences
                .Where(r => r.OriginalTimestamp < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Purged {Count} expired telemetry references (cutoff: {Cutoff})",
                deleted, cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error purging expired telemetry references");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeStaleTopologyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting purge of stale topology entries");

        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-TopologyStaleDays);

            var deleted = await _dbContext.ObservedTopologyEntries
                .Where(t => t.LastSeenAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Purged {Count} stale topology entries (cutoff: {Cutoff})",
                deleted, cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error purging stale topology entries");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ExecuteFullRetentionCycleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full retention cycle");

        await PurgeExpiredMinuteMetricsAsync(cancellationToken);
        await PurgeExpiredHourlyMetricsAsync(cancellationToken);
        await PurgeExpiredAnomalySnapshotsAsync(cancellationToken);
        await PurgeExpiredTelemetryReferencesAsync(cancellationToken);
        await PurgeStaleTopologyAsync(cancellationToken);

        _logger.LogInformation("Completed full retention cycle");
    }

    private async Task ConsolidateServiceMetricsAsync(
        DateTimeOffset hourStart,
        DateTimeOffset hourEnd,
        CancellationToken cancellationToken)
    {
        var minuteSnapshots = await _dbContext.ServiceMetricsSnapshots
            .Where(s => s.AggregationLevel == AggregationLevel.OneMinute
                        && s.IntervalStart >= hourStart
                        && s.IntervalStart < hourEnd)
            .ToListAsync(cancellationToken);

        if (minuteSnapshots.Count == 0) return;

        var groups = minuteSnapshots
            .GroupBy(s => new { s.ServiceId, s.Environment, s.TenantId });

        foreach (var group in groups)
        {
            var snapshots = group.ToList();

            var hourlySnapshot = new ServiceMetricsSnapshot
            {
                ServiceId = group.Key.ServiceId,
                ServiceName = snapshots[0].ServiceName,
                Environment = group.Key.Environment,
                TenantId = group.Key.TenantId,
                AggregationLevel = AggregationLevel.OneHour,
                IntervalStart = hourStart,
                IntervalEnd = hourEnd,
                RequestCount = snapshots.Sum(s => s.RequestCount),
                RequestsPerMinute = snapshots.Average(s => s.RequestsPerMinute),
                RequestsPerHour = snapshots.Sum(s => s.RequestCount),
                ErrorCount = snapshots.Sum(s => s.ErrorCount),
                ErrorRatePercent = snapshots.Sum(s => s.RequestCount) > 0
                    ? (double)snapshots.Sum(s => s.ErrorCount) / snapshots.Sum(s => s.RequestCount) * 100
                    : 0,
                LatencyAvgMs = snapshots.Average(s => s.LatencyAvgMs),
                LatencyP50Ms = snapshots.Average(s => s.LatencyP50Ms),
                LatencyP95Ms = snapshots.Average(s => s.LatencyP95Ms),
                LatencyP99Ms = snapshots.Average(s => s.LatencyP99Ms),
                LatencyMaxMs = snapshots.Max(s => s.LatencyMaxMs),
                CpuAvgPercent = snapshots.Where(s => s.CpuAvgPercent.HasValue)
                    .Select(s => s.CpuAvgPercent!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                MemoryAvgMb = snapshots.Where(s => s.MemoryAvgMb.HasValue)
                    .Select(s => s.MemoryAvgMb!.Value)
                    .DefaultIfEmpty()
                    .Average()
            };

            _dbContext.ServiceMetricsSnapshots.Add(hourlySnapshot);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consolidated {Count} minute service metric snapshots into {GroupCount} hourly snapshots for {HourStart}",
            minuteSnapshots.Count, groups.Count(), hourStart);
    }

    private async Task ConsolidateDependencyMetricsAsync(
        DateTimeOffset hourStart,
        DateTimeOffset hourEnd,
        CancellationToken cancellationToken)
    {
        var minuteSnapshots = await _dbContext.DependencyMetricsSnapshots
            .Where(d => d.AggregationLevel == AggregationLevel.OneMinute
                        && d.IntervalStart >= hourStart
                        && d.IntervalStart < hourEnd)
            .ToListAsync(cancellationToken);

        if (minuteSnapshots.Count == 0) return;

        var groups = minuteSnapshots
            .GroupBy(d => new
            {
                d.SourceServiceId,
                d.TargetServiceId,
                d.Environment,
                d.TenantId
            });

        foreach (var group in groups)
        {
            var snapshots = group.ToList();
            var totalCalls = snapshots.Sum(d => d.CallCount);
            var totalErrors = snapshots.Sum(d => d.ErrorCount);

            var hourlySnapshot = new DependencyMetricsSnapshot
            {
                SourceServiceId = group.Key.SourceServiceId,
                SourceServiceName = snapshots[0].SourceServiceName,
                TargetServiceId = group.Key.TargetServiceId,
                TargetServiceName = snapshots[0].TargetServiceName,
                Environment = group.Key.Environment,
                TenantId = group.Key.TenantId,
                AggregationLevel = AggregationLevel.OneHour,
                IntervalStart = hourStart,
                IntervalEnd = hourEnd,
                CallCount = totalCalls,
                ErrorCount = totalErrors,
                ErrorRatePercent = totalCalls > 0
                    ? (double)totalErrors / totalCalls * 100
                    : 0,
                LatencyAvgMs = snapshots.Average(d => d.LatencyAvgMs),
                LatencyP95Ms = snapshots.Average(d => d.LatencyP95Ms),
                LatencyP99Ms = snapshots.Average(d => d.LatencyP99Ms)
            };

            _dbContext.DependencyMetricsSnapshots.Add(hourlySnapshot);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consolidated {Count} minute dependency metric snapshots into {GroupCount} hourly snapshots for {HourStart}",
            minuteSnapshots.Count, groups.Count(), hourStart);
    }
}
