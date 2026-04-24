using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Ingestion;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NotifyDeploymentFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment.NotifyDeployment;
using ProcessIngestionPayloadFeature = NexTraceOne.Integrations.Application.Features.ProcessIngestionPayload.ProcessIngestionPayload;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de eventos de deployment.
/// Recebe notificações de pipelines CI/CD (GitHub Actions, GitLab CI, Jenkins, Azure DevOps)
/// e correlaciona automaticamente cada evento com uma Release no módulo ChangeGovernance.
/// </summary>
internal static class DeploymentEventEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de deployment no grupo raiz de deployments.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/events", async (
            HttpContext httpContext,
            DeploymentEventRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeploymentEventEndpoints));

            // Record ingestion event received — lazily resolved so the endpoint works
            // without IIngestionMetricsCollector registered (backward-compat).
            var metrics = httpContext.RequestServices.GetService<IIngestionMetricsCollector>();
            metrics?.RecordEventReceived("system", request.Provider);
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // Find or create connector
            var connector = await connectorRepo.GetByNameAsync(request.Provider, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: request.Provider.ToLowerInvariant().Replace(" ", "-"),
                    connectorType: "CI/CD",
                    description: $"Auto-registered {request.Provider} connector",
                    provider: request.Provider,
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            // Find or create source
            var source = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, request.Source ?? "default", ct);
            if (source is null)
            {
                source = IngestionSource.Create(
                    connectorId: connector.Id,
                    name: request.Source ?? "default",
                    sourceType: "Webhook",
                    dataDomain: null,
                    description: $"Deployment events from {request.Provider}",
                    endpoint: null,
                    expectedIntervalMinutes: 30,
                    utcNow: clock.UtcNow);
                await sourceRepo.AddAsync(source, ct);
            }

            // Create execution
            var execution = IngestionExecution.Start(
                connectorId: connector.Id,
                sourceId: source.Id,
                correlationId: correlationId,
                utcNow: clock.UtcNow);

            execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);

            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            // ── Correlação automática deploy event → Release ──────────────────────
            // Se o evento contém dados suficientes, dispatch para ChangeGovernance.
            // Falhas não bloqueiam a resposta ao pipeline — são registadas e descartadas.
            Guid? releaseId = null;
            bool? isNewRelease = null;

            if (!string.IsNullOrWhiteSpace(request.ServiceName)
                && !string.IsNullOrWhiteSpace(request.Version)
                && !string.IsNullOrWhiteSpace(request.Environment)
                && !string.IsNullOrWhiteSpace(request.CommitSha))
            {
                try
                {
                    var pipelineSource = string.IsNullOrWhiteSpace(request.Source)
                        ? request.Provider
                        : $"{request.Provider}/{request.Source}";

                    var notifyCommand = new NotifyDeploymentFeature.Command(
                        ApiAssetId: null,
                        ServiceName: request.ServiceName,
                        Version: request.Version,
                        Environment: request.Environment,
                        PipelineSource: pipelineSource,
                        CommitSha: request.CommitSha,
                        ExternalDeploymentId: correlationId);

                    var notifyResult = await sender.Send(notifyCommand, ct);

                    if (notifyResult.IsSuccess)
                    {
                        releaseId = notifyResult.Value.ReleaseId;
                        isNewRelease = notifyResult.Value.IsNewRelease;
                    }
                    else
                    {
                        logger.LogWarning(
                            "Deploy event → Release correlation returned failure for {ServiceName}@{Version} in {Environment}: {Error}",
                            request.ServiceName, request.Version, request.Environment, notifyResult.Error?.Message);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Unexpected error correlating deploy event to Release for {ServiceName}@{Version} in {Environment}",
                        request.ServiceName, request.Version, request.Environment);
                }
            }

            // ── Semantic payload processing ───────────────────────────────────────
            // Only triggered when release was NOT already correlated via NotifyDeployment.
            string processingStatus;
            if (releaseId.HasValue)
            {
                processingStatus = "release_correlated";
            }
            else
            {
                try
                {
                    var rawPayload = System.Text.Json.JsonSerializer.Serialize(request);
                    var processCmd = new ProcessIngestionPayloadFeature.Command(execution.Id.Value, rawPayload);
                    var processResult = await sender.Send(processCmd, ct);
                    processingStatus = processResult.IsSuccess ? processResult.Value.Status : "metadata_recorded";
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to dispatch ProcessIngestionPayload for execution {ExecutionId} — falling back to metadata_recorded",
                        execution.Id.Value);
                    processingStatus = "metadata_recorded";
                }
            }

            return Results.Accepted(null, new
            {
                message = "Deployment event received",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                releaseId,
                isNewRelease
            });
        })
        .WithName("PostDeploymentEvent")
        .WithSummary("Notify a deployment event from a CI/CD pipeline")
        .WithDescription("Receives deployment event notifications from CI/CD platforms")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
