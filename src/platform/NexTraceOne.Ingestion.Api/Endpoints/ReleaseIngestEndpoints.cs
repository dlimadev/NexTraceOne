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
        group.MapPost("/{releaseId:guid}/feature-flags", async (
            HttpContext httpContext,
            Guid releaseId,
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

            var connector = await GetOrCreateConnectorAsync(connectorRepo, request.FlagProvider, "FeatureFlags", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "feature-flags", request.FlagProvider, clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? flagStateResult = null;
            string processingStatus;

            try
            {
                var command = new RecordFeatureFlagStateFeature.Command(
                    ReleaseId: releaseId,
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
                        releaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording feature flag state for release {ReleaseId}", releaseId);
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
                releaseId,
                flagState = flagStateResult
            });
        })
        .WithName("PostRecordFeatureFlagState")
        .WithSummary("Record active feature flag state for a release pre-deploy")
        .WithDescription(
            "Records the state of active feature flags (LaunchDarkly, Split.io, Unleash) " +
            "immediately before a deploy. Used to enrich the Change Advisory and detect flag-induced regressions.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRecordCanaryRollout(RouteGroupBuilder group)
    {
        group.MapPost("/{releaseId:guid}/canary", async (
            HttpContext httpContext,
            Guid releaseId,
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

            var connector = await GetOrCreateConnectorAsync(connectorRepo, request.SourceSystem, "Canary", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "canary-rollout", request.SourceSystem, clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? rolloutResult = null;
            string processingStatus;

            try
            {
                var command = new RecordCanaryRolloutFeature.Command(
                    ReleaseId: releaseId,
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
                        releaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording canary rollout for release {ReleaseId}", releaseId);
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
                releaseId,
                rollout = rolloutResult
            });
        })
        .WithName("PostRecordCanaryRollout")
        .WithSummary("Record canary deployment rollout progress for a release")
        .WithDescription(
            "Records the current rollout percentage of a canary deployment " +
            "(Argo Rollouts, Flagger, Split.io). Multiple records per release track evolution over time.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRecordObservationMetrics(RouteGroupBuilder group)
    {
        group.MapPost("/{releaseId:guid}/observations", async (
            HttpContext httpContext,
            Guid releaseId,
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

            const string observationConnectorName = "post-change-observer";
            var connector = await GetOrCreateConnectorAsync(connectorRepo, observationConnectorName, "APM", clock, ct);
            var source = await GetOrCreateSourceAsync(sourceRepo, connector.Id, "observations", "APM Agent", clock, ct);
            var execution = IngestionExecution.Start(connector.Id, source.Id, correlationId, clock.UtcNow);

            object? observationResult = null;
            string processingStatus;

            try
            {
                var command = new RecordObservationMetricsFeature.Command(
                    ReleaseId: releaseId,
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
                        releaseId, request.Phase, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error recording observation metrics for release {ReleaseId} phase {Phase}",
                    releaseId, request.Phase);
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
                releaseId,
                observation = observationResult
            });
        })
        .WithName("PostRecordObservationMetrics")
        .WithSummary("Submit post-release observation metrics to trigger automatic post-change verification")
        .WithDescription(
            "Submits observed operational metrics (error rate, latency, throughput) for a post-release window. " +
            "Triggers automatic comparison against the pre-release baseline and progresses the PostReleaseReview. " +
            "Should be called by APM agents or OTel collectors after each observation phase.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRegisterRollback(RouteGroupBuilder group)
    {
        group.MapPost("/{releaseId:guid}/rollback", async (
            HttpContext httpContext,
            Guid releaseId,
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
                    ReleaseId: releaseId,
                    OriginalReleaseId: request.OriginalReleaseId);

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
                        releaseId, request.OriginalReleaseId, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error registering rollback for release {ReleaseId}", releaseId);
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
                releaseId,
                originalReleaseId = request.OriginalReleaseId
            });
        })
        .WithName("PostRegisterRollback")
        .WithSummary("Register a release rollback event from a CI/CD pipeline")
        .WithDescription(
            "Records that a release has been rolled back to a previous release. " +
            "Used to feed Rollback Intelligence and Change-to-Incident correlation data.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
