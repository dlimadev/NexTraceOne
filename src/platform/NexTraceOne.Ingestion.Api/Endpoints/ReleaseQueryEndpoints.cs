using MediatR;
using NexTraceOne.Ingestion.Api.Security;
using GetBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using GetCanaryRolloutStatusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetCanaryRolloutStatus.GetCanaryRolloutStatus;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory.GetChangeAdvisory;
using GetChangeIntelligenceSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;
using GetHistoricalPatternInsightFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetHistoricalPatternInsight.GetHistoricalPatternInsight;
using GetPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview.GetPostReleaseReview;
using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using ListReleasesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases.ListReleases;
using ResolveReleaseByExternalKeyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ResolveReleaseByExternalKey.ResolveReleaseByExternalKey;

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
        MapGetChangeIntelligenceSummary(group);
        MapGetCanaryRolloutStatus(group);
        MapGetHistoricalPatternInsight(group);
        MapResolveByExternalKey(group);
        MapGetReleaseByExternalKey(group);
        MapGetAdvisoryByExternalKey(group);
        MapGetBlastRadiusByExternalKey(group);
        MapGetPostReleaseReviewByExternalKey(group);
        MapGetChangeIntelligenceSummaryByExternalKey(group);
        MapGetCanaryRolloutStatusByExternalKey(group);
        MapGetHistoricalPatternInsightByExternalKey(group);
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

    // ── Natural Key Routing endpoints ─────────────────────────────────────────

    private static void MapGetChangeIntelligenceSummary(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/intelligence", async (
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
                var query = new GetChangeIntelligenceSummaryFeature.Query(releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetChangeIntelligenceSummary failed for {ReleaseId}: {Error}",
                    releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting change intelligence summary for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetChangeIntelligenceSummary")
        .WithSummary("Get the complete change intelligence record for a release")
        .WithDescription(
            "Returns the full change intelligence record for a release: release metadata, risk score, " +
            "blast radius, external markers, performance baseline, post-release review, rollback assessment " +
            "and change event timeline. " +
            "This is the single most complete view of a change in NexTraceOne — " +
            "ideal for ITSM integrations, audit exports, evidence packs and governance portals.")
        .Produces<GetChangeIntelligenceSummaryFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetCanaryRolloutStatus(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/canary", async (
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
                var query = new GetCanaryRolloutStatusFeature.Query(releaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetCanaryRolloutStatus failed for {ReleaseId}: {Error}",
                    releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting canary rollout status for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetCanaryRolloutStatus")
        .WithSummary("Get the canary rollout status and confidence boost for a release")
        .WithDescription(
            "Returns the current canary deployment state for a release: rollout percentage, " +
            "active/total instances, promoted/aborted flags, and a confidence boost signal " +
            "(High / Medium / Low / Negative) used by the change advisory engine. " +
            "Useful for CI/CD orchestrators implementing progressive delivery gates and " +
            "for monitoring dashboards tracking canary health before full rollout.")
        .Produces<GetCanaryRolloutStatusFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetHistoricalPatternInsight(RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}/historical-pattern", async (
            HttpContext httpContext,
            Guid releaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            int? lookbackDays,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new GetHistoricalPatternInsightFeature.Query(releaseId, lookbackDays);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, releaseId });

                logger.LogWarning("GetHistoricalPatternInsight failed for {ReleaseId}: {Error}",
                    releaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting historical pattern insight for {ReleaseId}", releaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetHistoricalPatternInsight")
        .WithSummary("Get historical pattern analysis for similar releases")
        .WithDescription(
            "Analyses the historical pattern of releases similar to the given release " +
            "(same service, environment and change level) within a configurable lookback window. " +
            "Returns: total samples, success rate, rollback rate, failure rate, average change score, " +
            "pattern risk signal (Low / Moderate / High / Insufficient) and a human-readable rationale. " +
            "Use lookbackDays to override the default 90-day window (min 7, max 365). " +
            "Ideal for risk assessment tools, deployment gates and AI-assisted advisory enrichment.")
        .Produces<GetHistoricalPatternInsightFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapResolveByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/resolve", async (
            HttpContext httpContext,
            ISender sender,
            ILoggerFactory loggerFactory,
            string externalReleaseId,
            string externalSystem,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var query = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning("ResolveByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error resolving release {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ResolveReleaseByExternalKey")
        .WithSummary("Resolve a NexTraceOne release ID from an external system's natural key")
        .WithDescription(
            "Returns the internal NexTraceOne release identifier and status for a release identified by " +
            "its external system natural key (externalReleaseId + externalSystem). " +
            "Use this endpoint to bootstrap a pipeline session: call it once to obtain the internal 'releaseId', " +
            "then use the GUID-based routes for subsequent calls within the same pipeline execution.")
        .Produces<ResolveReleaseByExternalKeyFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetReleaseByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetReleaseFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning("GetReleaseByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting release {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetReleaseByExternalKey")
        .WithSummary("Get release details using the external system's natural key")
        .WithDescription(
            "Returns the details of a release identified by the external system natural key " +
            "(externalSystem + externalReleaseId). Intended for CI/CD pipelines and external tools " +
            "that do not have access to the internal NexTraceOne release GUID.")
        .Produces<GetReleaseFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetAdvisoryByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/advisory", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetChangeAdvisoryFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning("GetAdvisoryByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting advisory {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetChangeAdvisoryByExternalKey")
        .WithSummary("Get the change advisory for a release using the external system's natural key")
        .WithDescription(
            "Returns the Change Advisory for a release identified by the external system natural key. " +
            "Used by CI/CD pipelines to implement promotion gates without knowledge of the NexTraceOne internal ID.")
        .Produces<GetChangeAdvisoryFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetBlastRadiusByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/blast-radius", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetBlastRadiusFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning("GetBlastRadiusByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting blast radius {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetBlastRadiusReportByExternalKey")
        .WithSummary("Get the blast radius report for a release using the external system's natural key")
        .WithDescription(
            "Returns the blast radius analysis for a release identified by the external system natural key. " +
            "Used by external systems to assess the impact scope of a planned change without knowing the internal ID.")
        .Produces<GetBlastRadiusFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetPostReleaseReviewByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/post-release-review", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetPostReleaseReviewFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning("GetPostReleaseReviewByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting post-release review {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetPostReleaseReviewByExternalKey")
        .WithSummary("Get the post-release review for a release using the external system's natural key")
        .WithDescription(
            "Returns the automatic post-change verification review for a release identified by the external " +
            "system natural key. Used by external monitoring systems to check post-change verification status " +
            "without knowledge of the internal NexTraceOne release GUID.")
        .Produces<GetPostReleaseReviewFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    // ── NEW: natural-key variants for intelligence, canary, historical-pattern ──────────

    private static void MapGetChangeIntelligenceSummaryByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/intelligence", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetChangeIntelligenceSummaryFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning(
                    "GetChangeIntelligenceSummaryByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error getting change intelligence summary for {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetChangeIntelligenceSummaryByExternalKey")
        .WithSummary("Get the full change intelligence record for a release using the external system's natural key")
        .WithDescription(
            "Returns the complete change intelligence aggregate (release metadata, confidence score, blast radius, " +
            "external markers, baseline, post-release review, rollback assessment and event timeline) identified by " +
            "the external system natural key. " +
            "Intended for CI/CD pipelines, ITSM tools and governance portals that do not hold the internal NexTraceOne release GUID.")
        .Produces<GetChangeIntelligenceSummaryFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetCanaryRolloutStatusByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/canary", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetCanaryRolloutStatusFeature.Query(resolveResult.Value.ReleaseId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning(
                    "GetCanaryRolloutStatusByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error getting canary rollout status for {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetCanaryRolloutStatusByExternalKey")
        .WithSummary("Get the canary rollout status for a release using the external system's natural key")
        .WithDescription(
            "Returns the canary deployment status and confidence signal (High / Medium / Low / Negative) for a release " +
            "identified by the external system natural key. " +
            "Used by canary controllers and deployment orchestrators that do not hold the internal NexTraceOne release GUID " +
            "to decide whether to proceed with a full rollout.")
        .Produces<GetCanaryRolloutStatusFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetHistoricalPatternInsightByExternalKey(RouteGroupBuilder group)
    {
        group.MapGet("/by-external/{externalSystem}/{externalReleaseId}/historical-pattern", async (
            HttpContext httpContext,
            string externalSystem,
            string externalReleaseId,
            ISender sender,
            ILoggerFactory loggerFactory,
            int? lookbackDays,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(ReleaseQueryEndpoints));

            try
            {
                var resolveQuery = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
                var resolveResult = await sender.Send(resolveQuery, ct);

                if (!resolveResult.IsSuccess)
                    return Results.NotFound(new { message = resolveResult.Error?.Message, externalReleaseId, externalSystem });

                var query = new GetHistoricalPatternInsightFeature.Query(resolveResult.Value.ReleaseId, lookbackDays);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code?.Contains("not_found") == true)
                    return Results.NotFound(new { message = result.Error.Message, externalReleaseId, externalSystem });

                logger.LogWarning(
                    "GetHistoricalPatternInsightByExternalKey failed for {ExternalSystem}/{ExternalReleaseId}: {Error}",
                    externalSystem, externalReleaseId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error getting historical pattern insight for {ExternalSystem}/{ExternalReleaseId}",
                    externalSystem, externalReleaseId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetHistoricalPatternInsightByExternalKey")
        .WithSummary("Get historical pattern analysis for a release using the external system's natural key")
        .WithDescription(
            "Analyses the historical pattern of releases similar to the given release (same service, environment " +
            "and change level) identified by the external system natural key. " +
            "Returns: total samples, success rate, rollback rate, failure rate, average change score, " +
            "pattern risk signal (Low / Moderate / High / Insufficient) and a human-readable rationale. " +
            "Use lookbackDays to override the default 90-day window (min 7, max 365). " +
            "Ideal for risk assessment tools and deployment gates that do not hold the internal NexTraceOne release GUID.")
        .Produces<GetHistoricalPatternInsightFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
