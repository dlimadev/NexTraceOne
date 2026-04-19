using MediatR;
using NexTraceOne.Ingestion.Api.Security;
using GetDoraMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDoraMetrics.GetDoraMetrics;
using GetChangesSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary.GetChangesSummary;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de relatórios e métricas de alto nível para consumo por sistemas externos,
/// portais de governança, dashboards e integrações de pipeline.
///
/// Endpoints expostos:
/// - GET /api/v1/reports/dora             — métricas DORA (Deployment Frequency, Lead Time, CFR, MTTR)
/// - GET /api/v1/reports/changes-summary  — contadores agregados de mudanças por equipa/ambiente/período
/// </summary>
internal static class ReportEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de relatórios no grupo de rotas de reports.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        MapGetDoraMetrics(group);
        MapGetChangesSummary(group);
    }

    // ── DORA Metrics ───────────────────────────────────────────────────────

    private static void MapGetDoraMetrics(RouteGroupBuilder group)
    {
        group.MapGet("/dora", async (
            HttpContext httpContext,
            ISender sender,
            ILoggerFactory loggerFactory,
            string? serviceName,
            string? teamName,
            string? environment,
            int days,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReportEndpoints));

            try
            {
                var query = new GetDoraMetricsFeature.Query(
                    ServiceName: serviceName,
                    TeamName: teamName,
                    Environment: environment ?? "Production",
                    Days: days > 0 ? days : 30);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning(
                    "GetDoraMetrics failed for service={Service} team={Team} env={Env}: {Error}",
                    serviceName, teamName, environment, result.Error?.Message);
                return Results.Problem(
                    result.Error?.Message ?? "Query failed",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error computing DORA metrics");
                return Results.Problem(
                    "An unexpected error occurred",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetDoraMetrics")
        .WithSummary("Get DORA metrics for a service, team or organisation")
        .WithDescription(
            "Calculates the four DORA engineering metrics: Deployment Frequency, Lead Time for Changes, " +
            "Change Failure Rate and Time to Restore Service (MTTR). " +
            "Each metric is classified as Elite / High / Medium / Low according to DORA benchmark thresholds. " +
            "Filter by serviceName and/or teamName to scope the calculation. " +
            "Use 'days' to control the rolling time window (default 30, max 365). " +
            "Requires integrations:read scope.")
        .Produces<GetDoraMetricsFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    // ── Changes Summary ────────────────────────────────────────────────────

    private static void MapGetChangesSummary(RouteGroupBuilder group)
    {
        group.MapGet("/changes-summary", async (
            HttpContext httpContext,
            ISender sender,
            ILoggerFactory loggerFactory,
            string? teamName,
            string? environment,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReportEndpoints));

            try
            {
                var query = new GetChangesSummaryFeature.Query(
                    TeamName: teamName,
                    Environment: environment,
                    From: from,
                    To: to);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning(
                    "GetChangesSummary failed for team={Team} env={Env}: {Error}",
                    teamName, environment, result.Error?.Message);
                return Results.Problem(
                    result.Error?.Message ?? "Query failed",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting changes summary");
                return Results.Problem(
                    "An unexpected error occurred",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetChangesSummary")
        .WithSummary("Get aggregate change counters for a team, environment or time window")
        .WithDescription(
            "Returns aggregate counters of changes in a given scope: total changes, validated changes, " +
            "changes needing attention, suspected regressions, and changes correlated with incidents. " +
            "Use teamName to filter by team, environment to filter by environment, " +
            "and from/to to define the time window. " +
            "Ideal for engineering health dashboards, release governance reports and CI/CD status feeds. " +
            "Requires integrations:read scope.")
        .Produces<GetChangesSummaryFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}
