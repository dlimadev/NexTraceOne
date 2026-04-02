using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Services;

/// <summary>
/// Implementação do serviço de agregação de telemetria.
/// Consolida métricas de minuto em intervalos maiores e atualiza topologia observada.
/// </summary>
public sealed class TelemetryAggregationService : ITelemetryAggregationService
{
    private readonly TelemetryStoreDbContext _dbContext;
    private readonly ILogger<TelemetryAggregationService> _logger;

    public TelemetryAggregationService(
        TelemetryStoreDbContext dbContext,
        ILogger<TelemetryAggregationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task AggregateServiceMetricsAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting service metrics aggregation for target hour {TargetHour}",
            targetHour);

        try
        {
            var hourStart = new DateTimeOffset(
                targetHour.Year, targetHour.Month, targetHour.Day,
                targetHour.Hour, 0, 0, targetHour.Offset);
            var hourEnd = hourStart.AddHours(1);

            var minuteSnapshots = await _dbContext.ServiceMetricsSnapshots
                .Where(s => s.AggregationLevel == AggregationLevel.OneMinute
                            && s.IntervalStart >= hourStart
                            && s.IntervalStart < hourEnd)
                .ToListAsync(cancellationToken);

            if (minuteSnapshots.Count == 0)
            {
                _logger.LogInformation(
                    "No minute service metrics found for target hour {TargetHour}, skipping aggregation",
                    targetHour);
                return;
            }

            var groups = minuteSnapshots
                .GroupBy(s => new { s.ServiceId, s.ServiceName, s.Environment, s.TenantId });

            var aggregatedCount = 0;
            foreach (var group in groups)
            {
                var snapshots = group.ToList();
                var totalRequests = snapshots.Sum(s => s.RequestCount);
                var totalErrors = snapshots.Sum(s => s.ErrorCount);

                var sortedByP50 = snapshots.OrderBy(s => s.LatencyP50Ms).ToList();
                var sortedByP95 = snapshots.OrderBy(s => s.LatencyP95Ms).ToList();
                var sortedByP99 = snapshots.OrderBy(s => s.LatencyP99Ms).ToList();

                var hourlySnapshot = new ServiceMetricsSnapshot
                {
                    ServiceId = group.Key.ServiceId,
                    ServiceName = group.Key.ServiceName,
                    Environment = group.Key.Environment,
                    TenantId = group.Key.TenantId,
                    AggregationLevel = AggregationLevel.OneHour,
                    IntervalStart = hourStart,
                    IntervalEnd = hourEnd,
                    RequestCount = totalRequests,
                    RequestsPerMinute = snapshots.Average(s => s.RequestsPerMinute),
                    RequestsPerHour = totalRequests,
                    ErrorCount = totalErrors,
                    ErrorRatePercent = totalRequests > 0
                        ? (double)totalErrors / totalRequests * 100
                        : 0,
                    LatencyAvgMs = snapshots.Average(s => s.LatencyAvgMs),
                    LatencyP50Ms = CalculatePercentile(sortedByP50, s => s.LatencyP50Ms, 0.50),
                    LatencyP95Ms = CalculatePercentile(sortedByP95, s => s.LatencyP95Ms, 0.95),
                    LatencyP99Ms = CalculatePercentile(sortedByP99, s => s.LatencyP99Ms, 0.99),
                    LatencyMaxMs = snapshots.Max(s => s.LatencyMaxMs),
                    CpuAvgPercent = snapshots.Any(s => s.CpuAvgPercent.HasValue)
                        ? snapshots.Where(s => s.CpuAvgPercent.HasValue).Average(s => s.CpuAvgPercent!.Value)
                        : null,
                    MemoryAvgMb = snapshots.Any(s => s.MemoryAvgMb.HasValue)
                        ? snapshots.Where(s => s.MemoryAvgMb.HasValue).Average(s => s.MemoryAvgMb!.Value)
                        : null
                };

                _dbContext.ServiceMetricsSnapshots.Add(hourlySnapshot);
                aggregatedCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Aggregated {SnapshotCount} minute service metrics into {GroupCount} hourly snapshots for {TargetHour}",
                minuteSnapshots.Count, aggregatedCount, targetHour);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error aggregating service metrics for target hour {TargetHour}",
                targetHour);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AggregateDependencyMetricsAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting dependency metrics aggregation for target hour {TargetHour}",
            targetHour);

        try
        {
            var hourStart = new DateTimeOffset(
                targetHour.Year, targetHour.Month, targetHour.Day,
                targetHour.Hour, 0, 0, targetHour.Offset);
            var hourEnd = hourStart.AddHours(1);

            var minuteSnapshots = await _dbContext.DependencyMetricsSnapshots
                .Where(d => d.AggregationLevel == AggregationLevel.OneMinute
                            && d.IntervalStart >= hourStart
                            && d.IntervalStart < hourEnd)
                .ToListAsync(cancellationToken);

            if (minuteSnapshots.Count == 0)
            {
                _logger.LogInformation(
                    "No minute dependency metrics found for target hour {TargetHour}, skipping aggregation",
                    targetHour);
                return;
            }

            var groups = minuteSnapshots
                .GroupBy(d => new
                {
                    d.SourceServiceId,
                    d.SourceServiceName,
                    d.TargetServiceId,
                    d.TargetServiceName,
                    d.Environment,
                    d.TenantId
                });

            var aggregatedCount = 0;
            foreach (var group in groups)
            {
                var snapshots = group.ToList();
                var totalCalls = snapshots.Sum(d => d.CallCount);
                var totalErrors = snapshots.Sum(d => d.ErrorCount);

                var hourlySnapshot = new DependencyMetricsSnapshot
                {
                    SourceServiceId = group.Key.SourceServiceId,
                    SourceServiceName = group.Key.SourceServiceName,
                    TargetServiceId = group.Key.TargetServiceId,
                    TargetServiceName = group.Key.TargetServiceName,
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
                    LatencyP95Ms = CalculatePercentile(snapshots.OrderBy(d => d.LatencyP95Ms).ToList(), d => d.LatencyP95Ms, 0.95),
                    LatencyP99Ms = CalculatePercentile(snapshots.OrderBy(d => d.LatencyP99Ms).ToList(), d => d.LatencyP99Ms, 0.99)
                };

                _dbContext.DependencyMetricsSnapshots.Add(hourlySnapshot);
                aggregatedCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Aggregated {SnapshotCount} minute dependency metrics into {GroupCount} hourly snapshots for {TargetHour}",
                minuteSnapshots.Count, aggregatedCount, targetHour);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error aggregating dependency metrics for target hour {TargetHour}",
                targetHour);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateObservedTopologyAsync(
        DateTimeOffset targetHour,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting observed topology update for target hour {TargetHour}",
            targetHour);

        try
        {
            var hourStart = new DateTimeOffset(
                targetHour.Year, targetHour.Month, targetHour.Day,
                targetHour.Hour, 0, 0, targetHour.Offset);
            var hourEnd = hourStart.AddHours(1);

            var minuteSnapshots = await _dbContext.DependencyMetricsSnapshots
                .Where(d => d.AggregationLevel == AggregationLevel.OneMinute
                            && d.IntervalStart >= hourStart
                            && d.IntervalStart < hourEnd)
                .ToListAsync(cancellationToken);

            if (minuteSnapshots.Count == 0)
            {
                _logger.LogInformation(
                    "No minute dependency metrics found for target hour {TargetHour}, skipping topology update",
                    targetHour);
                return;
            }

            var edges = minuteSnapshots
                .GroupBy(d => new
                {
                    d.SourceServiceId,
                    d.SourceServiceName,
                    d.TargetServiceId,
                    d.TargetServiceName,
                    d.Environment,
                    d.TenantId
                });

            var createdCount = 0;
            var updatedCount = 0;

            foreach (var edge in edges)
            {
                var snapshots = edge.ToList();
                var callCount = snapshots.Sum(d => d.CallCount);
                var errorCount = snapshots.Sum(d => d.ErrorCount);
                var hasNoErrors = errorCount == 0;
                var now = DateTimeOffset.UtcNow;

                var existing = await _dbContext.ObservedTopologyEntries
                    .FirstOrDefaultAsync(t =>
                        t.SourceServiceId == edge.Key.SourceServiceId
                        && t.TargetServiceId == edge.Key.TargetServiceId
                        && t.Environment == edge.Key.Environment
                        && t.TenantId == edge.Key.TenantId,
                        cancellationToken);

                if (existing is not null)
                {
                    var newTotalCalls = existing.TotalCallCount + callCount;
                    var confidenceScore = CalculateConfidenceScore(newTotalCalls, hasNoErrors);

                    var updated = existing with
                    {
                        LastSeenAt = now,
                        TotalCallCount = newTotalCalls,
                        ConfidenceScore = confidenceScore,
                        UpdatedAt = now
                    };
                    _dbContext.Entry(existing).CurrentValues.SetValues(updated);
                    updatedCount++;
                }
                else
                {
                    var confidenceScore = CalculateConfidenceScore(callCount, hasNoErrors);

                    var entry = new ObservedTopologyEntry
                    {
                        SourceServiceId = edge.Key.SourceServiceId,
                        SourceServiceName = edge.Key.SourceServiceName,
                        TargetServiceId = edge.Key.TargetServiceId,
                        TargetServiceName = edge.Key.TargetServiceName,
                        CommunicationType = "http",
                        Environment = edge.Key.Environment,
                        TenantId = edge.Key.TenantId,
                        ConfidenceScore = confidenceScore,
                        FirstSeenAt = now,
                        LastSeenAt = now,
                        TotalCallCount = callCount,
                        IsShadowDependency = true,
                        UpdatedAt = now
                    };

                    _dbContext.ObservedTopologyEntries.Add(entry);
                    createdCount++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated observed topology for {TargetHour}: {Created} created, {Updated} updated",
                targetHour, createdCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error updating observed topology for target hour {TargetHour}",
                targetHour);
            throw;
        }
    }

    /// <summary>
    /// Calcula o score de confiança da aresta de topologia observada.
    /// Fórmula: min(1.0, totalCalls/1000) * 0.7 + (hasNoErrors ? 0.3 : 0.0)
    /// - Volume component (70%): cresce linearmente até 1000 chamadas, depois satura.
    ///   1000 chamadas representam tráfego consistente suficiente para alta confiança.
    /// - Reliability component (30%): bónus de confiança quando não há erros observados.
    ///   Dependências com erros recebem penalização no score.
    /// </summary>
    private static double CalculateConfidenceScore(long totalCallCount, bool hasNoErrors)
    {
        var volumeComponent = Math.Min(1.0, totalCallCount / 1000.0) * 0.7;
        var reliabilityComponent = hasNoErrors ? 0.3 : 0.0;
        return volumeComponent + reliabilityComponent;
    }

    private static double CalculatePercentile<T>(
        List<T> sortedItems,
        Func<T, double> selector,
        double percentile)
    {
        if (sortedItems.Count == 0) return 0;
        if (sortedItems.Count == 1) return selector(sortedItems[0]);

        var index = (int)Math.Ceiling(percentile * sortedItems.Count) - 1;
        index = Math.Clamp(index, 0, sortedItems.Count - 1);
        return selector(sortedItems[index]);
    }
}
