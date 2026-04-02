using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Services;

/// <summary>
/// Implementação do serviço de consultas de telemetria orientadas ao produto.
/// Delega ao IObservabilityProvider para obtenção de dados crus e aplica
/// lógica de análise, correlação e agregação orientada ao negócio.
/// </summary>
public sealed class TelemetryQueryService : ITelemetryQueryService
{
    private readonly IObservabilityProvider _provider;
    private readonly ILogger<TelemetryQueryService> _logger;

    public TelemetryQueryService(
        IObservabilityProvider provider,
        ILogger<TelemetryQueryService> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ErrorFrequency>> GetTopErrorsByEnvironmentAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new LogQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                Level = "Error",
                Limit = 1000
            };

            var logs = await _provider.QueryLogsAsync(filter, cancellationToken);

            if (logs.Count == 0)
            {
                return [];
            }

            var grouped = logs
                .GroupBy(l => l.Message)
                .Select(g => new ErrorFrequency
                {
                    ErrorMessage = g.Key,
                    Count = g.Count(),
                    ServiceName = g
                        .GroupBy(l => l.ServiceName)
                        .OrderByDescending(sg => sg.Count())
                        .First()
                        .Key,
                    LastSeen = g.Max(l => l.Timestamp),
                    Level = g
                        .GroupBy(l => l.Level)
                        .OrderByDescending(lg => lg.Count())
                        .First()
                        .Key
                })
                .OrderByDescending(e => e.Count)
                .Take(top)
                .ToList();

            return grouped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to get top errors for environment {Environment} [{From} – {Until}]",
                environment, from, until);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<LatencyComparison> CompareLatencyAsync(
        string serviceName,
        string environmentA,
        string environmentB,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterA = new TraceQueryFilter
            {
                Environment = environmentA,
                From = from,
                Until = until,
                ServiceName = serviceName,
                Limit = 1000
            };

            var filterB = new TraceQueryFilter
            {
                Environment = environmentB,
                From = from,
                Until = until,
                ServiceName = serviceName,
                Limit = 1000
            };

            var tracesATask = _provider.QueryTracesAsync(filterA, cancellationToken);
            var tracesBTask = _provider.QueryTracesAsync(filterB, cancellationToken);

            await Task.WhenAll(tracesATask, tracesBTask);

            var tracesA = tracesATask.Result;
            var tracesB = tracesBTask.Result;

            var (p50A, p95A, p99A) = CalculatePercentiles(tracesA);
            var (p50B, p95B, p99B) = CalculatePercentiles(tracesB);

            var driftP95 = p95A > 0
                ? ((p95B - p95A) / p95A) * 100.0
                : 0.0;

            return new LatencyComparison
            {
                ServiceName = serviceName,
                EnvironmentA = environmentA,
                EnvironmentB = environmentB,
                LatencyP50MsA = p50A,
                LatencyP50MsB = p50B,
                LatencyP95MsA = p95A,
                LatencyP95MsB = p95B,
                LatencyP99MsA = p99A,
                LatencyP99MsB = p99B,
                DriftPercentP95 = Math.Round(driftP95, 2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to compare latency for {ServiceName} between {EnvA} and {EnvB}",
                serviceName, environmentA, environmentB);

            return new LatencyComparison
            {
                ServiceName = serviceName,
                EnvironmentA = environmentA,
                EnvironmentB = environmentB
            };
        }
    }

    /// <inheritdoc />
    public async Task<PostReleaseAnalysis> AnalyzePostReleaseImpactAsync(
        string serviceName,
        string environment,
        DateTimeOffset releaseTimestamp,
        TimeSpan windowBefore,
        TimeSpan windowAfter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var beforeFrom = releaseTimestamp - windowBefore;
            var afterUntil = releaseTimestamp + windowAfter;

            var errorRateBeforeTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = beforeFrom,
                Until = releaseTimestamp,
                MetricName = "error_rate",
                ServiceName = serviceName
            }, cancellationToken);

            var errorRateAfterTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = releaseTimestamp,
                Until = afterUntil,
                MetricName = "error_rate",
                ServiceName = serviceName
            }, cancellationToken);

            var latencyBeforeTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = beforeFrom,
                Until = releaseTimestamp,
                MetricName = "request_duration",
                ServiceName = serviceName
            }, cancellationToken);

            var latencyAfterTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = releaseTimestamp,
                Until = afterUntil,
                MetricName = "request_duration",
                ServiceName = serviceName
            }, cancellationToken);

            var throughputBeforeTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = beforeFrom,
                Until = releaseTimestamp,
                MetricName = "throughput",
                ServiceName = serviceName
            }, cancellationToken);

            var throughputAfterTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = releaseTimestamp,
                Until = afterUntil,
                MetricName = "throughput",
                ServiceName = serviceName
            }, cancellationToken);

            await Task.WhenAll(
                errorRateBeforeTask, errorRateAfterTask,
                latencyBeforeTask, latencyAfterTask,
                throughputBeforeTask, throughputAfterTask);

            var errorRateBefore = AverageMetricValue(errorRateBeforeTask.Result);
            var errorRateAfter = AverageMetricValue(errorRateAfterTask.Result);
            var latencyBefore = PercentileMetricValue(latencyBeforeTask.Result, 0.95);
            var latencyAfter = PercentileMetricValue(latencyAfterTask.Result, 0.95);
            var throughputBefore = AverageMetricValue(throughputBeforeTask.Result);
            var throughputAfter = AverageMetricValue(throughputAfterTask.Result);

            var hasDegradation = errorRateAfter > errorRateBefore * 1.2
                || latencyAfter > latencyBefore * 1.3;

            var impactScore = CalculateImpactScore(
                errorRateBefore, errorRateAfter,
                latencyBefore, latencyAfter,
                throughputBefore, throughputAfter);

            return new PostReleaseAnalysis
            {
                ServiceName = serviceName,
                Environment = environment,
                ReleaseTimestamp = releaseTimestamp,
                ErrorRateBefore = Math.Round(errorRateBefore, 4),
                ErrorRateAfter = Math.Round(errorRateAfter, 4),
                LatencyP95MsBefore = Math.Round(latencyBefore, 2),
                LatencyP95MsAfter = Math.Round(latencyAfter, 2),
                ThroughputBefore = Math.Round(throughputBefore, 2),
                ThroughputAfter = Math.Round(throughputAfter, 2),
                HasDegradation = hasDegradation,
                ImpactScore = Math.Round(impactScore, 4)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to analyze post-release impact for {ServiceName} in {Environment} at {Release}",
                serviceName, environment, releaseTimestamp);

            return new PostReleaseAnalysis
            {
                ServiceName = serviceName,
                Environment = environment,
                ReleaseTimestamp = releaseTimestamp
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResourceUsageSummary>> GetResourceUsageByServiceAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cpuTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                MetricName = "cpu_usage"
            }, cancellationToken);

            var memoryTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                MetricName = "memory_usage"
            }, cancellationToken);

            await Task.WhenAll(cpuTask, memoryTask);

            var cpuMetrics = cpuTask.Result;
            var memoryMetrics = memoryTask.Result;

            var serviceNames = cpuMetrics.Select(m => m.ServiceName)
                .Union(memoryMetrics.Select(m => m.ServiceName))
                .Distinct();

            var summaries = serviceNames.Select(service =>
            {
                var cpuForService = cpuMetrics
                    .Where(m => m.ServiceName == service)
                    .ToList();

                var memForService = memoryMetrics
                    .Where(m => m.ServiceName == service)
                    .ToList();

                return new ResourceUsageSummary
                {
                    ServiceName = service,
                    Environment = environment,
                    CpuAvgPercent = cpuForService.Count > 0
                        ? Math.Round(cpuForService.Average(m => m.Value), 2)
                        : 0,
                    MemoryAvgMb = memForService.Count > 0
                        ? Math.Round(memForService.Average(m => m.Value), 2)
                        : 0
                };
            }).ToList();

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to get resource usage for environment {Environment} [{From} – {Until}]",
                environment, from, until);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperationRegression>> DetectPerformanceRegressionsAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var totalDuration = until - from;
            var midpoint = from + TimeSpan.FromTicks(totalDuration.Ticks / 2);

            var baselineFilter = new TraceQueryFilter
            {
                Environment = environment,
                From = from,
                Until = midpoint,
                Limit = 1000
            };

            var currentFilter = new TraceQueryFilter
            {
                Environment = environment,
                From = midpoint,
                Until = until,
                Limit = 1000
            };

            var baselineTask = _provider.QueryTracesAsync(baselineFilter, cancellationToken);
            var currentTask = _provider.QueryTracesAsync(currentFilter, cancellationToken);

            await Task.WhenAll(baselineTask, currentTask);

            var baselineTraces = baselineTask.Result;
            var currentTraces = currentTask.Result;

            var baselineByOp = baselineTraces
                .GroupBy(t => (t.ServiceName, t.OperationName))
                .ToDictionary(
                    g => g.Key,
                    g => CalculatePercentile(g.Select(t => t.DurationMs).ToList(), 0.95));

            var currentByOp = currentTraces
                .GroupBy(t => (t.ServiceName, t.OperationName))
                .ToDictionary(
                    g => g.Key,
                    g => CalculatePercentile(g.Select(t => t.DurationMs).ToList(), 0.95));

            const double regressionThreshold = 20.0;
            var regressions = new List<OperationRegression>();

            foreach (var (key, currentP95) in currentByOp)
            {
                if (!baselineByOp.TryGetValue(key, out var baselineP95) || baselineP95 <= 0)
                {
                    continue;
                }

                var regressionPercent = ((currentP95 - baselineP95) / baselineP95) * 100.0;

                if (regressionPercent > regressionThreshold)
                {
                    regressions.Add(new OperationRegression
                    {
                        ServiceName = key.ServiceName,
                        OperationName = key.OperationName,
                        BaselineP95Ms = Math.Round(baselineP95, 2),
                        CurrentP95Ms = Math.Round(currentP95, 2),
                        RegressionPercent = Math.Round(regressionPercent, 2),
                        DetectedAt = midpoint
                    });
                }
            }

            return regressions
                .OrderByDescending(r => r.RegressionPercent)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to detect performance regressions for environment {Environment} [{From} – {Until}]",
                environment, from, until);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<CorrelatedSignals> CorrelateByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var traceDetailTask = _provider.GetTraceDetailAsync(traceId, cancellationToken);

            var logFilter = new LogQueryFilter
            {
                Environment = string.Empty,
                From = DateTimeOffset.MinValue,
                Until = DateTimeOffset.MaxValue,
                TraceId = traceId,
                Limit = 500
            };
            var logsTask = _provider.QueryLogsAsync(logFilter, cancellationToken);

            await Task.WhenAll(traceDetailTask, logsTask);

            var traceDetail = traceDetailTask.Result;
            var logs = logsTask.Result;

            return new CorrelatedSignals
            {
                TraceId = traceId,
                Logs = logs,
                Spans = traceDetail?.Spans ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to correlate signals for trace {TraceId}", traceId);

            return new CorrelatedSignals { TraceId = traceId };
        }
    }

    /// <inheritdoc />
    public async Task<ReleaseRiskEvidence> GenerateReleaseRiskEvidenceAsync(
        string serviceName,
        string environment,
        DateTimeOffset releaseTimestamp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var windowBefore = TimeSpan.FromHours(1);
            var windowAfter = TimeSpan.FromHours(1);

            var analysis = await AnalyzePostReleaseImpactAsync(
                serviceName, environment, releaseTimestamp,
                windowBefore, windowAfter, cancellationToken);

            var regressions = await DetectPerformanceRegressionsAsync(
                environment, releaseTimestamp, releaseTimestamp + windowAfter,
                cancellationToken);

            var serviceRegressions = regressions
                .Where(r => r.ServiceName == serviceName)
                .ToList();

            var riskIndicators = new List<string>();
            var anomalyEvidence = new List<string>();

            if (analysis.HasDegradation)
            {
                riskIndicators.Add("Post-release degradation detected");
            }

            if (analysis.ErrorRateAfter > analysis.ErrorRateBefore && analysis.ErrorRateBefore > 0)
            {
                var increase = ((analysis.ErrorRateAfter - analysis.ErrorRateBefore) / analysis.ErrorRateBefore) * 100;
                anomalyEvidence.Add($"Error rate increased by {increase:F1}% after release");
            }

            if (analysis.LatencyP95MsAfter > analysis.LatencyP95MsBefore * 1.2 && analysis.LatencyP95MsBefore > 0)
            {
                anomalyEvidence.Add(
                    $"P95 latency increased from {analysis.LatencyP95MsBefore:F1}ms to {analysis.LatencyP95MsAfter:F1}ms");
            }

            if (analysis.ThroughputAfter < analysis.ThroughputBefore * 0.8 && analysis.ThroughputBefore > 0)
            {
                anomalyEvidence.Add("Throughput dropped significantly after release");
            }

            foreach (var regression in serviceRegressions)
            {
                riskIndicators.Add(
                    $"Performance regression in {regression.OperationName}: +{regression.RegressionPercent:F1}%");
            }

            var riskScore = CalculateRiskScore(analysis, serviceRegressions);

            var recommendation = riskScore switch
            {
                >= 0.8 => "High risk — consider immediate rollback",
                >= 0.5 => "Medium risk — monitor closely and prepare rollback",
                >= 0.3 => "Low-medium risk — investigate anomalies",
                _ => "Low risk — release appears stable"
            };

            return new ReleaseRiskEvidence
            {
                ServiceName = serviceName,
                Environment = environment,
                RiskScore = Math.Round(riskScore, 4),
                RiskIndicators = riskIndicators,
                AnomalyEvidence = anomalyEvidence,
                Recommendation = recommendation
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to generate release risk evidence for {ServiceName} in {Environment} at {Release}",
                serviceName, environment, releaseTimestamp);

            return new ReleaseRiskEvidence
            {
                ServiceName = serviceName,
                Environment = environment,
                Recommendation = "Unable to assess risk — telemetry data unavailable"
            };
        }
    }

    /// <inheritdoc />
    public async Task<EnvironmentTelemetrySnapshot> GetEnvironmentSnapshotForAiAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var errorLogsTask = _provider.QueryLogsAsync(new LogQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                Level = "Error",
                Limit = 1000
            }, cancellationToken);

            var tracesTask = _provider.QueryTracesAsync(new TraceQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                Limit = 1000
            }, cancellationToken);

            var errorRateTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                MetricName = "error_rate"
            }, cancellationToken);

            var throughputTask = _provider.QueryMetricsAsync(new MetricQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                MetricName = "throughput"
            }, cancellationToken);

            await Task.WhenAll(errorLogsTask, tracesTask, errorRateTask, throughputTask);

            var errorLogs = errorLogsTask.Result;
            var traces = tracesTask.Result;
            var errorRateMetrics = errorRateTask.Result;
            var throughputMetrics = throughputTask.Result;

            var activeServices = traces
                .Select(t => t.ServiceName)
                .Distinct()
                .ToList();

            var totalErrors = errorLogs.Count;
            var globalErrorRate = AverageMetricValue(errorRateMetrics);
            var globalLatencyP95 = CalculatePercentile(
                traces.Select(t => t.DurationMs).ToList(), 0.95);
            var totalThroughput = AverageMetricValue(throughputMetrics);

            var topErrorServices = errorLogs
                .GroupBy(l => l.ServiceName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var topLatencyServices = traces
                .GroupBy(t => t.ServiceName)
                .Select(g => new
                {
                    Service = g.Key,
                    P95 = CalculatePercentile(g.Select(t => t.DurationMs).ToList(), 0.95)
                })
                .OrderByDescending(x => x.P95)
                .Take(5)
                .Select(x => x.Service)
                .ToList();

            return new EnvironmentTelemetrySnapshot
            {
                Environment = environment,
                From = from,
                Until = until,
                ActiveServiceCount = activeServices.Count,
                TotalErrors = totalErrors,
                GlobalErrorRate = Math.Round(globalErrorRate, 4),
                GlobalLatencyP95Ms = Math.Round(globalLatencyP95, 2),
                TotalThroughput = Math.Round(totalThroughput, 2),
                TopErrorServices = topErrorServices,
                TopLatencyServices = topLatencyServices
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to get environment snapshot for {Environment} [{From} – {Until}]",
                environment, from, until);

            return new EnvironmentTelemetrySnapshot
            {
                Environment = environment,
                From = from,
                Until = until
            };
        }
    }

    #region Private helpers

    private static (double P50, double P95, double P99) CalculatePercentiles(
        IReadOnlyList<TraceSummary> traces)
    {
        if (traces.Count == 0)
        {
            return (0, 0, 0);
        }

        var durations = traces.Select(t => t.DurationMs).ToList();

        return (
            CalculatePercentile(durations, 0.50),
            CalculatePercentile(durations, 0.95),
            CalculatePercentile(durations, 0.99));
    }

    private static double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = new List<double>(values);
        sorted.Sort();

        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));

        return sorted[index];
    }

    private static double AverageMetricValue(IReadOnlyList<TelemetryMetricPoint> metrics)
    {
        return metrics.Count > 0 ? metrics.Average(m => m.Value) : 0;
    }

    private static double PercentileMetricValue(
        IReadOnlyList<TelemetryMetricPoint> metrics, double percentile)
    {
        if (metrics.Count == 0)
        {
            return 0;
        }

        var values = metrics.Select(m => m.Value).ToList();
        return CalculatePercentile(values, percentile);
    }

    private static double CalculateImpactScore(
        double errorRateBefore, double errorRateAfter,
        double latencyBefore, double latencyAfter,
        double throughputBefore, double throughputAfter)
    {
        var score = 0.0;

        if (errorRateBefore > 0)
        {
            var errorIncrease = Math.Max(0, (errorRateAfter - errorRateBefore) / errorRateBefore);
            score += Math.Min(errorIncrease, 1.0) * 0.4;
        }
        else if (errorRateAfter > 0)
        {
            score += 0.2;
        }

        if (latencyBefore > 0)
        {
            var latencyIncrease = Math.Max(0, (latencyAfter - latencyBefore) / latencyBefore);
            score += Math.Min(latencyIncrease, 1.0) * 0.35;
        }

        if (throughputBefore > 0)
        {
            var throughputDrop = Math.Max(0, (throughputBefore - throughputAfter) / throughputBefore);
            score += Math.Min(throughputDrop, 1.0) * 0.25;
        }

        return Math.Min(score, 1.0);
    }

    private static double CalculateRiskScore(
        PostReleaseAnalysis analysis,
        IReadOnlyList<OperationRegression> regressions)
    {
        var score = analysis.ImpactScore * 0.6;

        if (regressions.Count > 0)
        {
            var maxRegression = regressions.Max(r => r.RegressionPercent);
            var regressionFactor = Math.Min(maxRegression / 100.0, 1.0);
            score += regressionFactor * 0.4;
        }

        return Math.Min(score, 1.0);
    }

    #endregion
}
