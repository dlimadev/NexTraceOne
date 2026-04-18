using MediatR;
using NexTraceOne.Ingestion.Api.Security;
using GetReleaseHealthTimelineFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetReleaseHealthTimeline.GetReleaseHealthTimeline;
using GetRuntimeHealthFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeHealth.GetRuntimeHealth;
using ListReleasesByServiceFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleasesByService.ListReleasesByService;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de consulta de saúde de serviços em runtime.
/// Permite que orquestradores, load balancers, portais de governança e ferramentas externas
/// consultem o estado de saúde mais recente de um serviço registado no NexTraceOne.
/// </summary>
internal static class ServiceHealthQueryEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de saúde de serviços no grupo raiz de services.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        MapGetServiceHealth(group);
        MapGetServiceHealthHistory(group);
        MapGetServiceReleases(group);
    }

    private static void MapGetServiceHealth(RouteGroupBuilder group)
    {
        group.MapGet("/{serviceName}/health", async (
            HttpContext httpContext,
            string serviceName,
            ISender sender,
            ILoggerFactory loggerFactory,
            string environment,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ServiceHealthQueryEndpoints));

            try
            {
                var query = new GetRuntimeHealthFeature.Query(
                    ServiceName: serviceName,
                    Environment: environment);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new
                    {
                        message = result.Error.Message,
                        serviceName,
                        environment
                    });

                logger.LogWarning(
                    "GetRuntimeHealth failed for {ServiceName}/{Environment}: {Error}",
                    serviceName, environment, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error getting runtime health for {ServiceName}/{Environment}",
                    serviceName, environment);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetServiceRuntimeHealth")
        .WithSummary("Get the latest runtime health snapshot for a service in a given environment")
        .WithDescription(
            "Returns the most recent runtime health snapshot for a service: health status (Healthy/Degraded/Unhealthy), " +
            "latency percentiles, error rate, CPU, memory, active instances and capture timestamp. " +
            "Useful for external dashboards, service mesh health checks and incident correlation tools.")
        .Produces<GetRuntimeHealthFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetServiceHealthHistory(RouteGroupBuilder group)
    {
        group.MapGet("/{serviceName}/health/history", async (
            HttpContext httpContext,
            string serviceName,
            ISender sender,
            ILoggerFactory loggerFactory,
            string environment,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ServiceHealthQueryEndpoints));

            try
            {
                var query = new GetReleaseHealthTimelineFeature.Query(
                    ServiceName: serviceName,
                    Environment: environment,
                    WindowStart: windowStart,
                    WindowEnd: windowEnd);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning(
                    "GetReleaseHealthTimeline failed for {ServiceName}/{Environment}: {Error}",
                    serviceName, environment, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error getting health history for {ServiceName}/{Environment}",
                    serviceName, environment);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetServiceHealthHistory")
        .WithSummary("Get health timeline for a service within a time window")
        .WithDescription(
            "Returns a chronological list of runtime health snapshots captured for a service between " +
            "windowStart and windowEnd. Useful for visualising health evolution around a release, " +
            "correlating degradation with deployments, and driving post-change verification. " +
            "Pass the deploy timestamp as windowStart and a post-release observation period as windowEnd.")
        .Produces<GetReleaseHealthTimelineFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetServiceReleases(RouteGroupBuilder group)
    {
        group.MapGet("/{serviceName}/releases", async (
            HttpContext httpContext,
            string serviceName,
            ISender sender,
            ILoggerFactory loggerFactory,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ServiceHealthQueryEndpoints));

            try
            {
                var query = new ListReleasesByServiceFeature.Query(
                    ServiceName: serviceName,
                    Page: page,
                    PageSize: pageSize);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning(
                    "ListReleasesByService failed for {ServiceName}: {Error}",
                    serviceName, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error listing releases for {ServiceName}", serviceName);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetServiceReleases")
        .WithSummary("List releases for a service by service name")
        .WithDescription(
            "Returns a paginated list of releases for the specified service, ordered by most recent first. " +
            "Includes version, environment, deployment status, change level, change score, " +
            "and external key identifiers (externalReleaseId, externalSystem) for CI/CD system correlation. " +
            "Useful for release history dashboards, change intelligence feeds and governance tooling.")
        .Produces<ListReleasesByServiceFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
