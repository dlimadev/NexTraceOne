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
}
