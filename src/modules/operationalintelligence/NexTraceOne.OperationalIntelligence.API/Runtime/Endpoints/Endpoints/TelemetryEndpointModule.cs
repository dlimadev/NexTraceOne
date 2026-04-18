using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using NexTraceOne.BuildingBlocks.Security.Extensions;

namespace NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints.Endpoints;

/// <summary>
/// Endpoints REST para consulta de telemetria: logs, traces e métricas.
/// Expõe IObservabilityProvider e ITelemetryQueryService ao frontend,
/// permitindo pesquisa, filtragem e correlação de sinais.
///
/// Endpoints disponíveis:
/// - GET    /api/v1/telemetry/logs                → Pesquisa de logs
/// - GET    /api/v1/telemetry/traces              → Pesquisa de traces
/// - GET    /api/v1/telemetry/traces/{traceId}    → Detalhe de trace com spans
/// - GET    /api/v1/telemetry/metrics             → Consulta de métricas
/// - GET    /api/v1/telemetry/errors/top          → Top errors por ambiente
/// - GET    /api/v1/telemetry/latency/compare     → Comparação de latência entre ambientes
/// - GET    /api/v1/telemetry/correlate/{traceId} → Correlação log↔trace
/// - GET    /api/v1/telemetry/health              → Health check do provider
///
/// Auto-descoberto pelo ApiHost via reflexão (convenção *EndpointModule + MapEndpoints).
/// </summary>
public sealed class TelemetryEndpointModule
{
    /// <summary>Limite máximo de resultados para pesquisa de logs (custo proporcional ao volume).</summary>
    private const int MaxLogResults = 1000;

    /// <summary>Limite máximo de resultados para pesquisa de traces (cada trace contém múltiplos spans).</summary>
    private const int MaxTraceResults = 500;

    /// <summary>Limite máximo de top errors retornados.</summary>
    private const int MaxTopErrors = 100;

    /// <summary>Registra endpoints de telemetria no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/telemetry").RequireRateLimiting("operations");

        // ── Logs ────────────────────────────────────────────────────────────

        group.MapGet("/logs", async (
            string environment,
            DateTimeOffset from,
            DateTimeOffset until,
            IObservabilityProvider provider,
            CancellationToken ct,
            string? serviceName = null,
            string? level = null,
            string? messageContains = null,
            string? traceId = null,
            int limit = 100) =>
        {
            if (limit is < 1 or > MaxLogResults) limit = 100;

            var filter = new LogQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                ServiceName = serviceName,
                Level = level,
                MessageContains = messageContains,
                TraceId = traceId,
                Limit = limit
            };

            var logs = await provider.QueryLogsAsync(filter, ct);
            return Results.Ok(logs);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Traces ──────────────────────────────────────────────────────────

        group.MapGet("/traces", async (
            string environment,
            DateTimeOffset from,
            DateTimeOffset until,
            IObservabilityProvider provider,
            CancellationToken ct,
            string? serviceName = null,
            string? operationName = null,
            double? minDurationMs = null,
            bool? hasErrors = null,
            string? serviceKind = null,
            int limit = 50) =>
        {
            if (limit is < 1 or > MaxTraceResults) limit = 50;

            var filter = new TraceQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                ServiceName = serviceName,
                OperationName = operationName,
                MinDurationMs = minDurationMs,
                HasErrors = hasErrors,
                ServiceKind = serviceKind,
                Limit = limit
            };

            var traces = await provider.QueryTracesAsync(filter, ct);
            return Results.Ok(traces);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Trace Detail ────────────────────────────────────────────────────

        group.MapGet("/traces/{traceId}", async (
            string traceId,
            IObservabilityProvider provider,
            CancellationToken ct) =>
        {
            var detail = await provider.GetTraceDetailAsync(traceId, ct);
            return detail is not null
                ? Results.Ok(detail)
                : Results.NotFound();
        })
        .RequirePermission("operations:telemetry:read");

        // ── Metrics ─────────────────────────────────────────────────────────

        group.MapGet("/metrics", async (
            string environment,
            DateTimeOffset from,
            DateTimeOffset until,
            string metricName,
            IObservabilityProvider provider,
            CancellationToken ct,
            string? serviceName = null) =>
        {
            var filter = new MetricQueryFilter
            {
                Environment = environment,
                From = from,
                Until = until,
                MetricName = metricName,
                ServiceName = serviceName
            };

            var metrics = await provider.QueryMetricsAsync(filter, ct);
            return Results.Ok(metrics);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Top Errors ──────────────────────────────────────────────────────

        group.MapGet("/errors/top", async (
            string environment,
            DateTimeOffset from,
            DateTimeOffset until,
            ITelemetryQueryService queryService,
            CancellationToken ct,
            int top = 10) =>
        {
            if (top is < 1 or > MaxTopErrors) top = 10;

            var errors = await queryService.GetTopErrorsByEnvironmentAsync(
                environment, from, until, top, ct);
            return Results.Ok(errors);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Latency Comparison ──────────────────────────────────────────────

        group.MapGet("/latency/compare", async (
            string serviceName,
            string environmentA,
            string environmentB,
            DateTimeOffset from,
            DateTimeOffset until,
            ITelemetryQueryService queryService,
            CancellationToken ct) =>
        {
            var comparison = await queryService.CompareLatencyAsync(
                serviceName, environmentA, environmentB, from, until, ct);
            return Results.Ok(comparison);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Correlate by TraceId ────────────────────────────────────────────

        group.MapGet("/correlate/{traceId}", async (
            string traceId,
            ITelemetryQueryService queryService,
            CancellationToken ct) =>
        {
            var signals = await queryService.CorrelateByTraceIdAsync(traceId, ct);
            return Results.Ok(signals);
        })
        .RequirePermission("operations:telemetry:read");

        // ── Provider Health ─────────────────────────────────────────────────

        group.MapGet("/health", async (
            IObservabilityProvider provider,
            ICollectionModeStrategy collectionMode,
            CancellationToken ct) =>
        {
            var providerHealthy = await provider.IsHealthyAsync(ct);
            var collectionModeHealthy = await collectionMode.IsHealthyAsync(ct);
            return Results.Ok(new
            {
                provider = provider.ProviderName,
                providerHealthy,
                collectionMode = collectionMode.ModeName,
                collectionModeHealthy,
                healthy = providerHealthy
            });
        })
        .RequirePermission("operations:telemetry:read");
    }
}
