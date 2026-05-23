using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Observability;

/// <summary>
/// Implementação do port IDashboardObservabilityReader.
/// Roteia todas as consultas para IObservabilityProvider (Elastic ou ClickHouse)
/// e ITelemetryQueryService, ocultando o backend do consumidor.
/// Todas as operações aplicam degradação graciosa: em caso de falha retornam
/// IsBackendAvailable = false com lista vazia, nunca lançam excepções.
/// </summary>
internal sealed class DashboardObservabilityReader(
    IServiceProvider serviceProvider,
    ILogger<DashboardObservabilityReader> logger) : IDashboardObservabilityReader
{
    private readonly IObservabilityProvider? _observabilityProvider = serviceProvider.GetService<IObservabilityProvider>();
    private readonly ITelemetryQueryService? _telemetryQueryService = serviceProvider.GetService<ITelemetryQueryService>();

    private static bool IsAvailable<T>(T? service, ILogger logger, string name) where T : class
    {
        if (service is null)
        {
            logger.LogWarning("{Service} is not registered — observability features will be degraded.", name);
            return false;
        }
        return true;
    }
    /// <inheritdoc/>
    public string BackendName => _observabilityProvider?.ProviderName ?? "None";

    /// <inheritdoc/>
    public async Task<DashboardLogsResult> QueryLogsAsync(
        DashboardLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar disponibilidade do backend antes de consultar
            if (!IsAvailable(_observabilityProvider, logger, nameof(IObservabilityProvider)))
                return new DashboardLogsResult([], 0, IsBackendAvailable: false);

            var healthy = await _observabilityProvider!.IsHealthyAsync(cancellationToken);
            if (!healthy)
            {
                logger.LogWarning(
                    "Observability backend '{Backend}' reported unhealthy — returning empty logs result.",
                    BackendName);
                return new DashboardLogsResult([], 0, IsBackendAvailable: false);
            }

            var filter = new LogQueryFilter
            {
                Environment      = request.Environment ?? string.Empty,
                From             = request.From,
                Until            = request.Until,
                ServiceName      = request.ServiceName,
                Level            = request.Severity,
                MessageContains  = request.SearchText,
                Limit            = request.Limit,
            };

            var entries = await _observabilityProvider!.QueryLogsAsync(filter, cancellationToken);

            var mapped = entries
                .Select(e => new DashboardLogEntry(
                    Timestamp:   e.Timestamp,
                    Severity:    e.Level,
                    ServiceName: e.ServiceName,
                    Message:     e.Message,
                    TraceId:     e.TraceId,
                    Environment: e.Environment))
                .ToList();

            return new DashboardLogsResult(mapped, mapped.Count, IsBackendAvailable: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to query logs from observability backend '{Backend}'. Returning degraded result.",
                BackendName);
            return new DashboardLogsResult([], 0, IsBackendAvailable: false);
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardMetricsResult> QueryMetricsAsync(
        DashboardMetricsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsAvailable(_observabilityProvider, logger, nameof(IObservabilityProvider)))
                return new DashboardMetricsResult([], request.MetricName, IsBackendAvailable: false);

            var healthy = await _observabilityProvider!.IsHealthyAsync(cancellationToken);
            if (!healthy)
            {
                logger.LogWarning(
                    "Observability backend '{Backend}' reported unhealthy — returning empty metrics result.",
                    BackendName);
                return new DashboardMetricsResult([], request.MetricName, IsBackendAvailable: false);
            }

            var filter = new MetricQueryFilter
            {
                Environment = request.Environment ?? "production",
                From        = request.From,
                Until       = request.Until,
                MetricName  = request.MetricName,
                ServiceName = request.ServiceName,
            };

            var points = await _observabilityProvider!.QueryMetricsAsync(filter, cancellationToken);

            var mapped = points
                .Select(p => new DashboardMetricPoint(
                    Timestamp:   p.Timestamp,
                    Value:       p.Value,
                    MetricName:  p.MetricName,
                    ServiceName: p.ServiceName))
                .ToList();

            return new DashboardMetricsResult(mapped, request.MetricName, IsBackendAvailable: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to query metrics from observability backend '{Backend}'. Returning degraded result.",
                BackendName);
            return new DashboardMetricsResult([], request.MetricName, IsBackendAvailable: false);
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardTracesResult> QueryTracesAsync(
        DashboardTracesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsAvailable(_observabilityProvider, logger, nameof(IObservabilityProvider)))
                return new DashboardTracesResult([], IsBackendAvailable: false);

            var healthy = await _observabilityProvider!.IsHealthyAsync(cancellationToken);
            if (!healthy)
            {
                logger.LogWarning(
                    "Observability backend '{Backend}' reported unhealthy — returning empty traces result.",
                    BackendName);
                return new DashboardTracesResult([], IsBackendAvailable: false);
            }

            var filter = new TraceQueryFilter
            {
                Environment   = request.Environment ?? string.Empty,
                From          = request.From,
                Until         = request.Until,
                ServiceName   = request.ServiceName,
                MinDurationMs = request.MinDurationMs,
                HasErrors     = request.HasErrors,
                Limit         = request.Limit,
            };

            var traces = await _observabilityProvider!.QueryTracesAsync(filter, cancellationToken);

            var mapped = traces
                .Select(t => new DashboardTraceEntry(
                    TraceId:       t.TraceId,
                    ServiceName:   t.ServiceName,
                    OperationName: t.OperationName,
                    DurationMs:    t.DurationMs,
                    HasErrors:     t.HasErrors,
                    StartTime:     t.StartTime,
                    SpanCount:     t.SpanCount))
                .ToList();

            return new DashboardTracesResult(mapped, IsBackendAvailable: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to query traces from observability backend '{Backend}'. Returning degraded result.",
                BackendName);
            return new DashboardTracesResult([], IsBackendAvailable: false);
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardErrorsResult> QueryTopErrorsAsync(
        DashboardErrorsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsAvailable(_telemetryQueryService, logger, nameof(ITelemetryQueryService)))
                return new DashboardErrorsResult([], 0, IsBackendAvailable: false);

            var errors = await _telemetryQueryService!.GetTopErrorsByEnvironmentAsync(
                request.Environment ?? "production",
                request.From,
                request.Until,
                request.Top,
                cancellationToken);

            var mapped = errors
                .Select(e => new DashboardErrorEntry(
                    Message:     e.ErrorMessage,
                    ServiceName: e.ServiceName,
                    Count:       (int)e.Count,
                    Severity:    e.Level,
                    LastSeen:    e.LastSeen))
                .ToList();

            var totalErrorCount = mapped.Sum(e => e.Count);

            return new DashboardErrorsResult(mapped, totalErrorCount, IsBackendAvailable: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to query top errors from observability backend '{Backend}'. Returning degraded result.",
                BackendName);
            return new DashboardErrorsResult([], 0, IsBackendAvailable: false);
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardServiceHealthResult> QueryServiceHealthAsync(
        DashboardServiceHealthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsAvailable(_observabilityProvider, logger, nameof(IObservabilityProvider)))
                return new DashboardServiceHealthResult([], IsBackendAvailable: false);

            var healthy = await _observabilityProvider!.IsHealthyAsync(cancellationToken);
            if (!healthy)
            {
                logger.LogWarning(
                    "Observability backend '{Backend}' reported unhealthy — returning empty service health result.",
                    BackendName);
                return new DashboardServiceHealthResult([], IsBackendAvailable: false);
            }

            // Consultar todos os traces do período para calcular saúde por serviço
            var filter = new TraceQueryFilter
            {
                Environment = request.Environment ?? string.Empty,
                From        = request.From,
                Until       = request.Until,
                Limit       = 1000,
            };

            var traces = await _observabilityProvider!.QueryTracesAsync(filter, cancellationToken);

            // Agrupar por serviço e calcular métricas de saúde
            var grouped = traces
                .GroupBy(t => t.ServiceName)
                .Select(g =>
                {
                    var traceList  = g.ToList();
                    var traceCount = traceList.Count;
                    var errorCount = traceList.Count(t => t.HasErrors);
                    var errorRate  = traceCount > 0 ? (double)errorCount / traceCount : 0.0;
                    var avgLatency = traceCount > 0
                        ? traceList.Average(t => t.DurationMs)
                        : 0.0;

                    // Classificar saúde com base na error rate
                    var healthStatus = errorRate switch
                    {
                        < 0.01 => "healthy",
                        < 0.05 => "degraded",
                        _      => "critical",
                    };

                    return new DashboardServiceHealthEntry(
                        ServiceName:   g.Key,
                        HealthStatus:  healthStatus,
                        ErrorRate:     errorRate,
                        AvgLatencyMs:  avgLatency,
                        TraceCount:    traceCount);
                })
                .OrderByDescending(s => s.ErrorRate)
                .ToList();

            return new DashboardServiceHealthResult(grouped, IsBackendAvailable: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to query service health from observability backend '{Backend}'. Returning degraded result.",
                BackendName);
            return new DashboardServiceHealthResult([], IsBackendAvailable: false);
        }
    }
}
