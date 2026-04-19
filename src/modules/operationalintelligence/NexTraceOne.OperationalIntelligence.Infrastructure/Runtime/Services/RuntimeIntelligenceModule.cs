using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do contrato público do módulo RuntimeIntelligence.
/// Usa RuntimeIntelligenceDbContext para consultas de leitura cross-module.
/// </summary>
internal sealed class RuntimeIntelligenceModule(
    RuntimeIntelligenceDbContext context,
    ILogger<RuntimeIntelligenceModule> logger) : IRuntimeIntelligenceModule
{
    private const decimal MissingBaselinePenalty = 0.90m;
    private const decimal HighDriftPenalty = 0.85m;
    private const decimal CriticalDriftPenalty = 0.70m;
    private const decimal BaselineMinWeight = 0.85m;
    private const decimal BaselineVariableWeight = 0.15m;
    private const int MetricsMaxSamples = 50;
    private const int ForecastDays = 30;
    private const int TrendWindowDays = 28;

    /// <inheritdoc />
    public async Task<string?> GetCurrentHealthStatusAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching latest runtime health status for service '{ServiceName}' in environment '{Environment}'",
            serviceName,
            environment);

        var latestStatus = await context.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.CapturedAt)
            .Select(s => (HealthStatus?)s.HealthStatus)
            .FirstOrDefaultAsync(cancellationToken);

        return latestStatus?.ToString();
    }

    /// <inheritdoc />
    public async Task<decimal?> GetObservabilityScoreAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching runtime observability score for service '{ServiceName}' in environment '{Environment}'",
            serviceName,
            environment);

        var profileScore = await context.ObservabilityProfiles
            .AsNoTracking()
            .Where(p => p.ServiceName == serviceName && p.Environment == environment)
            .OrderByDescending(p => p.LastAssessedAt)
            .Select(p => (decimal?)p.ObservabilityScore)
            .FirstOrDefaultAsync(cancellationToken);

        if (!profileScore.HasValue)
            return null;

        var baselineConfidence = await context.RuntimeBaselines
            .AsNoTracking()
            .Where(b => b.ServiceName == serviceName && b.Environment == environment)
            .OrderByDescending(b => b.EstablishedAt)
            .Select(b => (decimal?)b.ConfidenceScore)
            .FirstOrDefaultAsync(cancellationToken);

        var hasOpenHighDrift = await context.DriftFindings
            .AsNoTracking()
            .AnyAsync(
                d => d.ServiceName == serviceName
                     && d.Environment == environment
                     && !d.IsResolved
                     && d.Severity == DriftSeverity.High,
                cancellationToken);

        var hasOpenCriticalDrift = await context.DriftFindings
            .AsNoTracking()
            .AnyAsync(
                d => d.ServiceName == serviceName
                     && d.Environment == environment
                     && !d.IsResolved
                     && d.Severity == DriftSeverity.Critical,
                cancellationToken);

        var score = profileScore.Value;

        // Weighted score with baseline: 85% guaranteed + up to 15% based on confidence (0.85 to 1.00 range).
        score *= baselineConfidence.HasValue
            ? BaselineMinWeight + (Math.Clamp(baselineConfidence.Value, 0m, 1m) * BaselineVariableWeight)
            : MissingBaselinePenalty;

        if (hasOpenCriticalDrift)
        {
            score *= CriticalDriftPenalty;
        }
        else if (hasOpenHighDrift)
        {
            score *= HighDriftPenalty;
        }

        return Math.Round(Math.Clamp(score, 0m, 1m), 2);
    }

    /// <inheritdoc />
    public async Task<ServiceRuntimeMetrics?> GetServiceMetricsAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Aggregating runtime metrics for service '{ServiceName}' in environment '{Environment}'",
            serviceName,
            environment);

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

        var snapshots = await context.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceName == serviceName
                        && s.Environment == environment
                        && s.CapturedAt >= cutoff)
            .OrderByDescending(s => s.CapturedAt)
            .Take(MetricsMaxSamples)
            .Select(s => new { s.AvgLatencyMs, s.ErrorRate })
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
            return null;

        var avgLatency = (long)Math.Round(snapshots.Average(s => (double)s.AvgLatencyMs));
        var avgErrorRate = Math.Round(snapshots.Average(s => s.ErrorRate), 4);

        return new ServiceRuntimeMetrics(avgLatency, avgErrorRate, snapshots.Count);
    }

    /// <inheritdoc />
    public async Task<PlatformAverageMetrics?> GetPlatformAverageMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Computing platform-wide average metrics for capacity forecast.");

        var cutoffHistory = DateTimeOffset.UtcNow.AddDays(-TrendWindowDays);
        var cutoffCurrent = DateTimeOffset.UtcNow.AddHours(-24);

        // Fetch all snapshots in the 28-day window ordered chronologically
        var allSnapshots = await context.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.CapturedAt >= cutoffHistory)
            .OrderBy(s => s.CapturedAt)
            .Select(s => new { s.CpuUsagePercent, s.MemoryUsageMb, s.CapturedAt })
            .ToListAsync(cancellationToken);

        if (allSnapshots.Count == 0)
            return null;

        // Current averages (last 24h)
        var recent = allSnapshots.Where(s => s.CapturedAt >= cutoffCurrent).ToList();
        var currentCpu = recent.Count > 0
            ? (double)recent.Average(s => s.CpuUsagePercent)
            : (double)allSnapshots.TakeLast(Math.Min(50, allSnapshots.Count)).Average(s => s.CpuUsagePercent);

        // Estimate memory as % of a reasonable host ceiling (e.g. 16 GB = 16384 MB)
        const double MemoryCeilingMb = 16384.0;
        var currentMemMb = recent.Count > 0
            ? (double)recent.Average(s => s.MemoryUsageMb)
            : (double)allSnapshots.TakeLast(Math.Min(50, allSnapshots.Count)).Average(s => s.MemoryUsageMb);
        var currentMemPct = Math.Min(currentMemMb / MemoryCeilingMb * 100.0, 100.0);

        // Linear regression: x = days since start, y = metric value
        var startTime = allSnapshots[0].CapturedAt;
        var cpuSlope = ComputeLinearSlope(
            allSnapshots.Select(s => ((s.CapturedAt - startTime).TotalDays, (double)s.CpuUsagePercent)).ToList());
        var memSlope = ComputeLinearSlope(
            allSnapshots.Select(s => ((s.CapturedAt - startTime).TotalDays, (double)s.MemoryUsageMb / MemoryCeilingMb * 100.0)).ToList());

        var forecastedCpu = Math.Clamp(currentCpu + cpuSlope * ForecastDays, 0, 100);
        var forecastedMem = Math.Clamp(currentMemPct + memSlope * ForecastDays, 0, 100);

        return new PlatformAverageMetrics(
            CurrentCpuPct: Math.Round(currentCpu, 1),
            CurrentMemoryPct: Math.Round(currentMemPct, 1),
            ForecastedCpuPct: Math.Round(forecastedCpu, 1),
            ForecastedMemoryPct: Math.Round(forecastedMem, 1),
            CpuTrend: ClassifyTrend(cpuSlope),
            MemoryTrend: ClassifyTrend(memSlope),
            SampleCount: allSnapshots.Count);
    }

    /// <summary>Computes the slope (units per day) of a simple linear regression over (x, y) pairs.</summary>
    private static double ComputeLinearSlope(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 2)
            return 0;

        var n = points.Count;
        var sumX = points.Sum(p => p.X);
        var sumY = points.Sum(p => p.Y);
        var sumXY = points.Sum(p => p.X * p.Y);
        var sumX2 = points.Sum(p => p.X * p.X);
        var denominator = n * sumX2 - sumX * sumX;

        return Math.Abs(denominator) < double.Epsilon ? 0 : (n * sumXY - sumX * sumY) / denominator;
    }

    /// <summary>Classifies a slope as "increasing", "decreasing" or "stable".</summary>
    private static string ClassifyTrend(double slopePerDay)
    {
        return slopePerDay switch
        {
            > 0.1 => "increasing",
            < -0.1 => "decreasing",
            _ => "stable"
        };
    }
}
