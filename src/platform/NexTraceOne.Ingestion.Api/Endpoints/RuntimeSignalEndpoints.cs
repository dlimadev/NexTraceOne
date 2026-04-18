using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using IngestRuntimeSnapshotFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestRuntimeSnapshot.IngestRuntimeSnapshot;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de sinais e snapshots de runtime.
/// Recebe sinais operacionais genéricos e snapshots estruturados de saúde
/// de serviços em execução, alimentando RuntimeIntelligence e correlação pós-release.
/// </summary>
internal static class RuntimeSignalEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de sinais e snapshots de runtime no grupo raiz de runtime.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        MapRuntimeSignal(group);
        MapRuntimeSnapshot(group);
    }

    private static void MapRuntimeSignal(RouteGroupBuilder group)
    {
        group.MapPost("/signals", async (
            HttpContext httpContext,
            RuntimeSignalRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var connector = await connectorRepo.GetByNameAsync("runtime-signals", ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: "runtime-signals",
                    connectorType: "Runtime",
                    description: "Runtime signals and markers",
                    provider: "Internal",
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var execution = IngestionExecution.Start(
                connectorId: connector.Id,
                sourceId: null,
                correlationId: correlationId,
                utcNow: clock.UtcNow);

            execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);

            await executionRepo.AddAsync(execution, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Runtime signal received",
                status = "accepted",
                processingStatus = "metadata_recorded",
                correlationId,
                executionId = execution.Id.Value
            });
        })
        .WithName("PostRuntimeSignal")
        .WithSummary("Submit a runtime signal or operational marker from a monitored service")
        .WithDescription(
            "Receives generic runtime signals and markers from monitored services. " +
            "For structured health metrics (latency, error rate, CPU, memory) use POST /runtime/snapshots instead.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapRuntimeSnapshot(RouteGroupBuilder group)
    {
        group.MapPost("/snapshots", async (
            HttpContext httpContext,
            IngestRuntimeSnapshotRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(RuntimeSignalEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Auditoria de ingestão ─────────────────────────────────────────────
            var sourceName = request.Source ?? "runtime-collector";
            var connector = await connectorRepo.GetByNameAsync(sourceName, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: sourceName.ToLowerInvariant().Replace(" ", "-"),
                    connectorType: "APM",
                    description: $"Auto-registered {sourceName} runtime collector",
                    provider: sourceName,
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var ingestionSource = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, "runtime-snapshots", ct);
            if (ingestionSource is null)
            {
                ingestionSource = IngestionSource.Create(
                    connectorId: connector.Id,
                    name: "runtime-snapshots",
                    sourceType: "ScheduledExport",
                    dataDomain: "RuntimeIntelligence",
                    description: $"Runtime health snapshots from {sourceName}",
                    endpoint: null,
                    expectedIntervalMinutes: 1,
                    utcNow: clock.UtcNow);
                await sourceRepo.AddAsync(ingestionSource, ct);
            }

            var execution = IngestionExecution.Start(connector.Id, ingestionSource.Id, correlationId, clock.UtcNow);

            // ── Dispatch para o domínio ───────────────────────────────────────────
            object? snapshotResult = null;
            string processingStatus;

            try
            {
                var command = new IngestRuntimeSnapshotFeature.Command(
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    AvgLatencyMs: request.AvgLatencyMs,
                    P99LatencyMs: request.P99LatencyMs,
                    ErrorRate: request.ErrorRate,
                    RequestsPerSecond: request.RequestsPerSecond,
                    CpuUsagePercent: request.CpuUsagePercent,
                    MemoryUsageMb: request.MemoryUsageMb,
                    ActiveInstances: request.ActiveInstances,
                    CapturedAt: request.CapturedAt,
                    Source: request.Source ?? sourceName);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    snapshotResult = new
                    {
                        snapshotId = result.Value.SnapshotId,
                        healthStatus = result.Value.HealthStatus,
                        capturedAt = result.Value.CapturedAt
                    };
                    processingStatus = "snapshot_recorded";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected runtime snapshot", null, clock.UtcNow);
                    logger.LogWarning(
                        "IngestRuntimeSnapshot rejected for {ServiceName}/{Environment}: {Error}",
                        request.ServiceName, request.Environment, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error ingesting runtime snapshot for {ServiceName}/{Environment}",
                    request.ServiceName, request.Environment);
            }

            ingestionSource.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(ingestionSource, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Runtime snapshot accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                serviceName = request.ServiceName,
                environment = request.Environment,
                snapshot = snapshotResult
            });
        })
        .WithName("PostIngestRuntimeSnapshot")
        .WithSummary("Submit a structured runtime health snapshot from an APM agent or OTel collector")
        .WithDescription(
            "Records a structured runtime health snapshot (latency percentiles, error rate, CPU, memory, " +
            "active instances) from APM agents or OpenTelemetry collectors. " +
            "Health status (Healthy/Degraded/Unhealthy) is automatically classified by the domain. " +
            "These snapshots power post-change verification and service reliability analysis.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
