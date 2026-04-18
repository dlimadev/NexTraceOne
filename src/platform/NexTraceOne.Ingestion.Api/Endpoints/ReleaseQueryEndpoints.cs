using MediatR;
using NexTraceOne.Ingestion.Api.Security;
using GetBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory.GetChangeAdvisory;
using GetPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview.GetPostReleaseReview;
using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using ListReleasesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases.ListReleases;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de consulta de releases e dados de Change Intelligence.
/// Permite que sistemas externos (pipelines CI/CD, portais de governança, ferramentas ITSM)
/// consultem releases, advisories, blast radius e resultados de post-change verification
/// diretamente do NexTraceOne como source of truth de mudanças.
/// </summary>
internal static class ReleaseQueryEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de consulta de releases no grupo raiz de releases.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        MapListReleases(group);
        MapGetRelease(group);
        MapGetChangeAdvisory(group);
        MapGetBlastRadius(group);
        MapGetPostReleaseReview(group);
    }

    private static void MapListReleases(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpContext httpContext,
            ISender sender,
            ILoggerFactory loggerFactory,
            Guid? apiAssetId,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new ListReleasesFeature.Query(
                    ApiAssetId: apiAssetId,
                    Page: page,
                    PageSize: pageSize);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning("ListReleases returned failure: {Error}", result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error listing releases");
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetReleases")
        .WithSummary("List releases from NexTraceOne Change Intelligence")
        .WithDescription(
            "Returns a paginated list of releases registered in NexTraceOne. " +
            "Optionally filter by API asset. Useful for pipelines and external tools to enumerate known releases.")
        .Produces<ListReleasesFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetRelease(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}", async (
            HttpContext httpContext,
            Guid releaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new GetReleaseFeature.Query(ReleaseId: releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetRelease failed for {ReleaseId}: {Error}", releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting release {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetRelease")
        .WithSummary("Get details of a specific release")
        .WithDescription(
            "Returns the details of a release including service, version, environment, status, " +
            "change level and change score. Useful for pipelines checking release status before promotion.")
        .Produces<GetReleaseFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetChangeAdvisory(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/advisory", async (
            HttpContext httpContext,
            Guid releaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new GetChangeAdvisoryFeature.Query(ReleaseId: releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetChangeAdvisory failed for {ReleaseId}: {Error}", releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting change advisory for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetChangeAdvisory")
        .WithSummary("Get the change advisory and governance recommendation for a release")
        .WithDescription(
            "Returns the Change Advisory for a release: governance recommendation (Approve, Reject, " +
            "ApproveConditionally, NeedsMoreEvidence), overall confidence score, and individual factor analysis " +
            "(evidence completeness, blast radius, change score, rollback readiness, historical pattern). " +
            "Used by CI/CD pipelines to implement promotion gates and by ITSM systems for change approval workflows.")
        .Produces<GetChangeAdvisoryFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetBlastRadius(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/blast-radius", async (
            HttpContext httpContext,
            Guid releaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new GetBlastRadiusFeature.Query(ReleaseId: releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetBlastRadiusReport failed for {ReleaseId}: {Error}", releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting blast radius for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetBlastRadiusReport")
        .WithSummary("Get the blast radius report for a release")
        .WithDescription(
            "Returns the blast radius analysis for a release: total affected consumers, " +
            "direct consumers and transitive consumers. Used by external systems to assess " +
            "the impact scope of a planned change before promoting to production.")
        .Produces<GetBlastRadiusFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetPostReleaseReview(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/post-release-review", async (
            HttpContext httpContext,
            Guid releaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new GetPostReleaseReviewFeature.Query(ReleaseId: releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetPostReleaseReview failed for {ReleaseId}: {Error}", releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting post-release review for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetPostReleaseReview")
        .WithSummary("Get the post-release review and verification status for a release")
        .WithDescription(
            "Returns the automatic post-change verification review for a release: current phase, " +
            "outcome, confidence score and observation windows. " +
            "Used by external monitoring systems to check if a deployed release has passed post-change verification.")
        .Produces<GetPostReleaseReviewFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
