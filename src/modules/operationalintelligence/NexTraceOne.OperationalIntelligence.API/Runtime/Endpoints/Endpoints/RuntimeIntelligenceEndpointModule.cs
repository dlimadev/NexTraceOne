using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using IngestRuntimeSnapshotFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestRuntimeSnapshot.IngestRuntimeSnapshot;
using GetRuntimeHealthFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeHealth.GetRuntimeHealth;
using GetObservabilityScoreFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetObservabilityScore.GetObservabilityScore;
using ComputeObservabilityDebtFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ComputeObservabilityDebt.ComputeObservabilityDebt;
using DetectRuntimeDriftFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectRuntimeDrift.DetectRuntimeDrift;
using GetDriftFindingsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetDriftFindings.GetDriftFindings;
using GetReleaseHealthTimelineFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetReleaseHealthTimeline.GetReleaseHealthTimeline;
using CompareReleaseRuntimeFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareReleaseRuntime.CompareReleaseRuntime;
using EstablishRuntimeBaselineFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.EstablishRuntimeBaseline.EstablishRuntimeBaseline;
using CompareEnvironmentsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareEnvironments.CompareEnvironments;
using CorrelateTraceToChangeFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateTraceToChange.CorrelateTraceToChange;
using CreateChaosExperimentFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateChaosExperiment.CreateChaosExperiment;
using ListChaosExperimentsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListChaosExperiments.ListChaosExperiments;
using CorrelateServiceMetricsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateServiceMetrics.CorrelateServiceMetrics;
using DetectLogAnomalyFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectLogAnomaly.DetectLogAnomaly;
using GetTopologyAwareAlertsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTopologyAwareAlerts.GetTopologyAwareAlerts;

namespace NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo RuntimeIntelligence.
/// Agrupa endpoints por responsabilidade: saúde, observabilidade, drift e comparação.
///
/// Endpoints disponíveis:
/// - POST   /snapshots              → Ingerir snapshot de runtime
/// - GET    /health                  → Saúde atual do serviço
/// - GET    /observability           → Score de observabilidade
/// - POST   /observability/assess    → Avaliar dívida de observabilidade
/// - POST   /drift/detect           → Detectar drift contra baseline
/// - GET    /drift                   → Listar findings de drift
/// - GET    /timeline               → Timeline de saúde por release
/// - GET    /compare                → Comparar runtime entre releases
/// </summary>
public sealed class RuntimeIntelligenceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/runtime").RequireRateLimiting("operations");

        group.MapPost("/snapshots", async (
            IngestRuntimeSnapshotFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/runtime/snapshots/{r.SnapshotId}", localizer);
        })
        .RequirePermission("operations:runtime:write");

        group.MapGet("/health", async (
            string serviceName,
            string environment,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetRuntimeHealthFeature.Query(serviceName, environment);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");

        group.MapGet("/observability", async (
            string serviceName,
            string environment,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetObservabilityScoreFeature.Query(serviceName, environment);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");

        group.MapPost("/observability/assess", async (
            ComputeObservabilityDebtFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/runtime/observability/{r.ProfileId}", localizer);
        })
        .RequirePermission("operations:runtime:write");

        group.MapPost("/drift/detect", async (
            DetectRuntimeDriftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:write");

        group.MapGet("/drift", async (
            string serviceName,
            string environment,
            bool? unacknowledgedOnly,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetDriftFindingsFeature.Query(serviceName, environment, unacknowledgedOnly ?? false, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");

        group.MapGet("/timeline", async (
            string serviceName,
            string environment,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetReleaseHealthTimelineFeature.Query(serviceName, environment, windowStart, windowEnd);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");

        group.MapGet("/compare", async (
            string serviceName,
            string environment,
            DateTimeOffset beforeStart,
            DateTimeOffset beforeEnd,
            DateTimeOffset afterStart,
            DateTimeOffset afterEnd,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new CompareReleaseRuntimeFeature.Query(serviceName, environment, beforeStart, beforeEnd, afterStart, afterEnd);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");

        // ── P6.5 — Operational Consistency: baseline + cross-environment comparison ──

        group.MapPost("/baselines", async (
            EstablishRuntimeBaselineFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:write");

        group.MapPost("/compare-environments", async (
            CompareEnvironmentsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:write");

        // ── P5.4 — Observability Correlation Engine ───────────────────────────────

        group.MapGet("/correlate-trace", async (
            string traceId,
            string serviceId,
            string environment,
            DateTimeOffset traceTimestamp,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new CorrelateTraceToChangeFeature.Query(traceId, serviceId, environment, traceTimestamp);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:read");

        group.MapPost("/detect-log-anomaly", async (
            DetectLogAnomalyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:write");

        // ── P5.4 — Metric Correlation + Topology-Aware Alerting ──
        group.MapPost("/correlate-metrics", async (
            CorrelateServiceMetricsFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:read");

        group.MapPost("/topology-alerts", async (
            GetTopologyAwareAlertsFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:read");

        // ── Chaos Engineering: experiment planning ────────────────────────────────

        group.MapPost("/chaos/experiments", async (
            CreateChaosExperimentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:write");

        group.MapGet("/chaos/experiments", async (
            string? serviceName,
            string? environment,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new ListChaosExperimentsFeature.Query(serviceName, environment, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runtime:read");
    }
}
