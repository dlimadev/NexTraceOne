using MediatR;
using NexTraceOne.Ingestion.Api.Security;
using GetRuntimeHealthFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeHealth.GetRuntimeHealth;

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
}
