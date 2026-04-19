using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using IngestExternalReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease.IngestExternalRelease;
using RecordCanaryRolloutFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordCanaryRollout.RecordCanaryRollout;
using RecordFeatureFlagStateFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordFeatureFlagState.RecordFeatureFlagState;
using RecordObservationMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordObservationMetrics.RecordObservationMetrics;
using RegisterRollbackFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterRollback.RegisterRollback;
using ResolveReleaseByExternalKeyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ResolveReleaseByExternalKey.ResolveReleaseByExternalKey;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de releases e eventos associados (feature flags, canary, observações, rollbacks).
/// Permitem que sistemas externos (Azure DevOps Release, Jenkins, Argo Rollouts, APM agents)
/// alimentem o NexTraceOne com dados de ciclo de vida de releases para Change Intelligence.
/// </summary>
internal static class ReleaseIngestEndpoints
{
    /// <summary>
    /// Mapeia todos os endpoints de ingestão de releases no grupo raiz de releases.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        MapIngestExternalRelease(group);
        MapRecordFeatureFlagState(group);
        MapRecordCanaryRollout(group);
        MapRecordObservationMetrics(group);
        MapRegisterRollback(group);
    }

    private static void MapIngestExternalRelease(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            HttpContext httpContext,
            IngestExternalReleaseRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ReleaseIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            var connector = await GetOrCreateConnectorAsync(connectorRepo, request.ExternalSystem, "Release", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "releases", request.ExternalSystem, clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            Guid? releaseId = null;
            bool? isNew = null;
            string processingStatus;

            try
            {
                var workItems = request.WorkItems?
                    .Select(w => new IngestExternalReleaseFeature.ExternalWorkItemRef(w.Id, w.System))
                    .ToList()
                    .AsReadOnly();

                var command = new IngestExternalReleaseFeature.Command(
                    ExternalReleaseId: request.ExternalReleaseId,
                    ExternalSystem: request.ExternalSystem,
                    ServiceName: request.ServiceName,
                    Version: request.Version,
                    TargetEnvironment: request.TargetEnvironment,
                    Description: request.Description,
                    CommitShas: request.CommitShas?.AsReadOnly(),
                    WorkItems: workItems,
                    TriggerPromotion: request.TriggerPromotion,
                    EnvironmentId: request.EnvironmentId);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    releaseId = result.Value.ReleaseId;
                    isNew = result.Value.IsNew;
                    processingStatus = isNew == true ? "release_created" : "release_already_exists";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected the release", null, clock.UtcNow);
                    logger.LogWarning(
                        "IngestExternalRelease rejected for {ServiceName}@{Version}/{TargetEnvironment}: {Error}",
                        request.ServiceName, request.Version, request.TargetEnvironment, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error ingesting release {ExternalReleaseId} for {ServiceName}@{Version}",
                    request.ExternalReleaseId, request.ServiceName, request.Version);
            }

            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Release ingestion accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId,
                isNew,
                serviceName = request.ServiceName,
                version = request.Version,
                targetEnvironment = request.TargetEnvironment
            });
        })
        .WithName("PostIngestExternalRelease")
        .WithSummary("Ingest a release from an external release management system")
        .WithDescription(
            "Records a release created by an external system (Azure DevOps, Jira, Jenkins, GitLab) " +
            "into NexTraceOne for Change Intelligence, advisory scoring and promotion governance. " +
            "Idempotent: re-ingesting the same ExternalReleaseId+ExternalSystem returns the existing release.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRecordFeatureFlagState(RouteGroupBuilder group)
    {
        group.MapPost("/feature-flags", async (
            HttpContext httpContext,
            RecordFeatureFlagStateRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ReleaseIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Resolução da release por GUID interno ou chave natural externa ──
            var resolvedReleaseId = await ResolveReleaseIdAsync(sender, request.ReleaseId, request.ExternalReleaseId, request.ExternalSystem, ct);
            if (resolvedReleaseId is null)
            {
                logger.LogWarning("Feature flag state rejected: could not resolve release from provided identifiers");
                return Results.BadRequest(new
                {
                    message = "Cannot resolve release. Provide either 'releaseId' (internal GUID) or both 'externalReleaseId' and 'externalSystem'.",
                    code = "release.identifier_required"
                });
            }

            if (resolvedReleaseId == Guid.Empty)
            {
                return Results.NotFound(new
                {
                    message = "Release not found for the provided external key.",
                    code = "release.not_found",
                    externalReleaseId = request.ExternalReleaseId,
                    externalSystem = request.ExternalSystem
                });
            }

            var connector = await GetOrCreateConnectorAsync(connectorRepo, request.FlagProvider, "FeatureFlags", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "feature-flags", request.FlagProvider, clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? flagStateResult = null;
            string processingStatus;

            try
            {
                var command = new RecordFeatureFlagStateFeature.Command(
                    ReleaseId: resolvedReleaseId.Value,
                    ActiveFlagCount: request.ActiveFlagCount,
                    CriticalFlagCount: request.CriticalFlagCount,
                    NewFeatureFlagCount: request.NewFeatureFlagCount,
                    FlagProvider: request.FlagProvider,
                    FlagsJson: request.FlagsJson);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    flagStateResult = new
                    {
                        stateId = result.Value.StateId,
                        activeFlags = result.Value.ActiveFlagCount,
                        criticalFlags = result.Value.CriticalFlagCount,
                        recordedAt = result.Value.RecordedAt
                    };
                    processingStatus = "feature_flags_recorded";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected feature flag state", null, clock.UtcNow);
                    logger.LogWarning(
                        "RecordFeatureFlagState rejected for release {ReleaseId}: {Error}",
                        resolvedReleaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording feature flag state for release {ReleaseId}", resolvedReleaseId);
            }

            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Feature flag state accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId = resolvedReleaseId,
                flagState = flagStateResult
            });
        })
        .WithName("PostRecordFeatureFlagState")
        .WithSummary("Record active feature flag state for a release pre-deploy")
        .WithDescription(
            "Records the state of active feature flags (LaunchDarkly, Split.io, Unleash) " +
            "immediately before a deploy. Used to enrich the Change Advisory and detect flag-induced regressions. " +
            "Accepts either the internal NexTraceOne 'releaseId' (GUID) or the external 'externalReleaseId' + " +
            "'externalSystem' pair — external systems do not need to know the internal identifier.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRecordCanaryRollout(RouteGroupBuilder group)
    {
        group.MapPost("/canary", async (
            HttpContext httpContext,
            RecordCanaryRolloutRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ReleaseIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Resolução da release por GUID interno ou chave natural externa ──
            var resolvedReleaseId = await ResolveReleaseIdAsync(sender, request.ReleaseId, request.ExternalReleaseId, request.ExternalSystem, ct);
            if (resolvedReleaseId is null)
            {
                logger.LogWarning("Canary rollout rejected: could not resolve release from provided identifiers");
                return Results.BadRequest(new
                {
                    message = "Cannot resolve release. Provide either 'releaseId' (internal GUID) or both 'externalReleaseId' and 'externalSystem'.",
                    code = "release.identifier_required"
                });
            }

            if (resolvedReleaseId == Guid.Empty)
            {
                return Results.NotFound(new
                {
                    message = "Release not found for the provided external key.",
                    code = "release.not_found",
                    externalReleaseId = request.ExternalReleaseId,
                    externalSystem = request.ExternalSystem
                });
            }

            var connector = await GetOrCreateConnectorAsync(connectorRepo, request.SourceSystem, "Canary", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "canary-rollout", request.SourceSystem, clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? rolloutResult = null;
            string processingStatus;

            try
            {
                var command = new RecordCanaryRolloutFeature.Command(
                    ReleaseId: resolvedReleaseId.Value,
                    RolloutPercentage: request.RolloutPercentage,
                    ActiveInstances: request.ActiveInstances,
                    TotalInstances: request.TotalInstances,
                    SourceSystem: request.SourceSystem,
                    IsPromoted: request.IsPromoted,
                    IsAborted: request.IsAborted);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    rolloutResult = new
                    {
                        rolloutId = result.Value.RolloutId,
                        rolloutPercentage = result.Value.RolloutPercentage,
                        isPromoted = result.Value.IsPromoted,
                        isAborted = result.Value.IsAborted,
                        recordedAt = result.Value.RecordedAt
                    };
                    processingStatus = "canary_rollout_recorded";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected canary rollout", null, clock.UtcNow);
                    logger.LogWarning(
                        "RecordCanaryRollout rejected for release {ReleaseId}: {Error}",
                        resolvedReleaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording canary rollout for release {ReleaseId}", resolvedReleaseId);
            }

            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Canary rollout event accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId = resolvedReleaseId,
                rollout = rolloutResult
            });
        })
        .WithName("PostRecordCanaryRollout")
        .WithSummary("Record canary deployment rollout progress for a release")
        .WithDescription(
            "Records the current rollout percentage of a canary deployment " +
            "(Argo Rollouts, Flagger, Split.io). Multiple records per release track evolution over time. " +
            "Accepts either the internal NexTraceOne 'releaseId' (GUID) or the external 'externalReleaseId' + " +
            "'externalSystem' pair — external systems do not need to know the internal identifier.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRecordObservationMetrics(RouteGroupBuilder group)
    {
        group.MapPost("/observations", async (
            HttpContext httpContext,
            RecordObservationMetricsRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ReleaseIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Resolução da release por GUID interno ou chave natural externa ──
            var resolvedReleaseId = await ResolveReleaseIdAsync(sender, request.ReleaseId, request.ExternalReleaseId, request.ExternalSystem, ct);
            if (resolvedReleaseId is null)
            {
                logger.LogWarning("Observation metrics rejected: could not resolve release from provided identifiers");
                return Results.BadRequest(new
                {
                    message = "Cannot resolve release. Provide either 'releaseId' (internal GUID) or both 'externalReleaseId' and 'externalSystem'.",
                    code = "release.identifier_required"
                });
            }

            if (resolvedReleaseId == Guid.Empty)
            {
                return Results.NotFound(new
                {
                    message = "Release not found for the provided external key.",
                    code = "release.not_found",
                    externalReleaseId = request.ExternalReleaseId,
                    externalSystem = request.ExternalSystem
                });
            }

            const string observationConnectorName = "post-change-observer";
            var connector = await GetOrCreateConnectorAsync(connectorRepo, observationConnectorName, "APM", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "observations", "APM Agent", clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? observationResult = null;
            string processingStatus;

            try
            {
                var command = new RecordObservationMetricsFeature.Command(
                    ReleaseId: resolvedReleaseId.Value,
                    Phase: request.Phase,
                    WindowStartsAt: request.WindowStartsAt,
                    WindowEndsAt: request.WindowEndsAt,
                    RequestsPerMinute: request.RequestsPerMinute,
                    ErrorRate: request.ErrorRate,
                    AvgLatencyMs: request.AvgLatencyMs,
                    P95LatencyMs: request.P95LatencyMs,
                    P99LatencyMs: request.P99LatencyMs,
                    Throughput: request.Throughput);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    observationResult = new
                    {
                        windowId = result.Value.ObservationWindowId,
                        reviewId = result.Value.ReviewId,
                        phase = result.Value.Phase,
                        outcome = result.Value.Outcome,
                        confidenceScore = result.Value.ConfidenceScore,
                        summary = result.Value.Summary,
                        reviewCompleted = result.Value.ReviewCompleted
                    };
                    processingStatus = result.Value.Outcome == "Skipped" ? "verification_skipped" : "observation_recorded";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected observation metrics", null, clock.UtcNow);
                    logger.LogWarning(
                        "RecordObservationMetrics rejected for release {ReleaseId} phase {Phase}: {Error}",
                        resolvedReleaseId, request.Phase, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording observation metrics for release {ReleaseId} phase {Phase}",
                    resolvedReleaseId, request.Phase);
            }

            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Post-release observation accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId = resolvedReleaseId,
                observation = observationResult
            });
        })
        .WithName("PostRecordObservationMetrics")
        .WithSummary("Submit post-release observation metrics to trigger automatic post-change verification")
        .WithDescription(
            "Submits observed operational metrics (error rate, latency, throughput) for a post-release window. " +
            "Triggers automatic comparison against the pre-release baseline and progresses the PostReleaseReview. " +
            "Should be called by APM agents or OTel collectors after each observation phase. " +
            "Accepts either the internal NexTraceOne 'releaseId' (GUID) or the external 'externalReleaseId' + " +
            "'externalSystem' pair — external systems do not need to know the internal identifier.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRegisterRollback(RouteGroupBuilder group)
    {
        group.MapPost("/rollback", async (
            HttpContext httpContext,
            RegisterRollbackRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ReleaseIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Resolução da release (rollback) por GUID interno ou chave natural externa ──
            var resolvedReleaseId = await ResolveReleaseIdAsync(sender, request.ReleaseId, request.ExternalReleaseId, request.ExternalSystem, ct);
            if (resolvedReleaseId is null)
            {
                logger.LogWarning("Rollback rejected: could not resolve rollback release from provided identifiers");
                return Results.BadRequest(new
                {
                    message = "Cannot resolve rollback release. Provide either 'releaseId' (internal GUID) or both 'externalReleaseId' and 'externalSystem'.",
                    code = "release.identifier_required"
                });
            }

            if (resolvedReleaseId == Guid.Empty)
            {
                return Results.NotFound(new
                {
                    message = "Rollback release not found for the provided external key.",
                    code = "release.not_found",
                    externalReleaseId = request.ExternalReleaseId,
                    externalSystem = request.ExternalSystem
                });
            }

            // ── Resolução da release original por GUID interno ou chave natural externa ──
            var resolvedOriginalReleaseId = await ResolveReleaseIdAsync(sender, request.OriginalReleaseId, request.OriginalExternalReleaseId, request.OriginalExternalSystem ?? request.ExternalSystem, ct);
            if (resolvedOriginalReleaseId is null)
            {
                logger.LogWarning("Rollback rejected: could not resolve original release from provided identifiers");
                return Results.BadRequest(new
                {
                    message = "Cannot resolve original release. Provide either 'originalReleaseId' (internal GUID) or both 'originalExternalReleaseId' and 'externalSystem'.",
                    code = "original_release.identifier_required"
                });
            }

            if (resolvedOriginalReleaseId == Guid.Empty)
            {
                return Results.NotFound(new
                {
                    message = "Original release not found for the provided external key.",
                    code = "original_release.not_found",
                    originalExternalReleaseId = request.OriginalExternalReleaseId,
                    externalSystem = request.ExternalSystem
                });
            }

            const string rollbackConnectorName = "rollback-events";
            var connector = await connectorRepo.GetByNameAsync(rollbackConnectorName, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: rollbackConnectorName,
                    connectorType: "CI/CD",
                    description: "Rollback events from CI/CD pipelines",
                    provider: "Pipeline",
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var execution = IngestionExecution.Start(connector.Id, null, correlationId, clock.UtcNow);

            string processingStatus;

            try
            {
                var command = new RegisterRollbackFeature.Command(
                    ReleaseId: resolvedReleaseId.Value,
                    OriginalReleaseId: resolvedOriginalReleaseId.Value);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    processingStatus = "rollback_registered";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected rollback", null, clock.UtcNow);
                    logger.LogWarning(
                        "RegisterRollback rejected for release {ReleaseId} → original {OriginalReleaseId}: {Error}",
                        resolvedReleaseId, resolvedOriginalReleaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error registering rollback for release {ReleaseId}", resolvedReleaseId);
            }

            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Rollback registration accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId = resolvedReleaseId,
                originalReleaseId = resolvedOriginalReleaseId
            });
        })
        .WithName("PostRegisterRollback")
        .WithSummary("Register a release rollback event from a CI/CD pipeline")
        .WithDescription(
            "Records that a release has been rolled back to a previous release. " +
            "Used to feed Rollback Intelligence and Change-to-Incident correlation data. " +
            "Accepts either the internal NexTraceOne 'releaseId'/'originalReleaseId' (GUIDs) or the external " +
            "'externalReleaseId'/'originalExternalReleaseId' + 'externalSystem' pairs. " +
            "Use 'originalExternalSystem' when the original release belongs to a different CI/CD system than the rollback release.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve o identificador interno de uma release a partir de um GUID interno ou de uma chave natural externa.
    /// Retorna <c>null</c> se nenhum identificador válido foi fornecido.
    /// Retorna <see cref="Guid.Empty"/> se a release não foi encontrada pela chave externa.
    /// </summary>
    private static async Task<Guid?> ResolveReleaseIdAsync(
        ISender sender,
        Guid? releaseId,
        string? externalReleaseId,
        string? externalSystem,
        CancellationToken ct)
    {
        if (releaseId.HasValue && releaseId.Value != Guid.Empty)
            return releaseId.Value;

        if (!string.IsNullOrWhiteSpace(externalReleaseId) && !string.IsNullOrWhiteSpace(externalSystem))
        {
            var query = new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? result.Value.ReleaseId : Guid.Empty;
        }

        return null;
    }

    private static async Task<IntegrationConnector> GetOrCreateConnectorAsync(
        IIntegrationConnectorRepository repo,
        string name,
        string type,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var connector = await repo.GetByNameAsync(name, ct);
        if (connector is not null)
            return connector;

        connector = IntegrationConnector.Create(
            name: name.ToLowerInvariant().Replace(" ", "-"),
            connectorType: type,
            description: $"Auto-registered {name} connector",
            provider: name,
            endpoint: null,
            environment: null,
            authenticationMode: null,
            pollingMode: null,
            allowedTeams: null,
            utcNow: clock.UtcNow);
        await repo.AddAsync(connector, ct);
        return connector;
    }

    private static async Task<IngestionSource> GetOrCreateSourceAsync(
        IIngestionSourceRepository repo,
        IntegrationConnectorId connectorId,
        string sourceName,
        string provider,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var source = await repo.GetByConnectorAndNameAsync(connectorId, sourceName, ct);
        if (source is not null)
            return source;

        source = IngestionSource.Create(
            connectorId: connectorId,
            name: sourceName,
            sourceType: "Webhook",
            dataDomain: "ChangeIntelligence",
            description: $"{sourceName} events from {provider}",
            endpoint: null,
            expectedIntervalMinutes: 30,
            utcNow: clock.UtcNow);
        await repo.AddAsync(source, ct);
        return source;
    }
}
