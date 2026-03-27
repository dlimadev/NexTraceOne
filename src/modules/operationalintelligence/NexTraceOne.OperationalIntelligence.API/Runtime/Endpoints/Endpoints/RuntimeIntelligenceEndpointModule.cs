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
            return result.ToCreatedResult("/api/v1/runtime/snapshots/{0}", localizer);
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
            return result.ToCreatedResult("/api/v1/runtime/observability/{0}", localizer);
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
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
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
    }
}
