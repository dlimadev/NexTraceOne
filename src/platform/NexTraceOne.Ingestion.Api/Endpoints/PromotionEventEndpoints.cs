using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using ProcessIngestionPayloadFeature = NexTraceOne.Integrations.Application.Features.ProcessIngestionPayload.ProcessIngestionPayload;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de eventos de promoção entre ambientes.
/// Recebe notificações de promoção de release (ex: staging → production)
/// e regista a execução de ingestão correspondente para rastreabilidade.
/// </summary>
internal static class PromotionEventEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de promoção no grupo raiz de promotions.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/events", async (
            HttpContext httpContext,
            PromotionEventRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(PromotionEventEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            var connector = await connectorRepo.GetByNameAsync("promotions", ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: "promotions",
                    connectorType: "Promotions",
                    description: "Environment promotion events",
                    provider: "Internal",
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

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

            // ── Semantic payload processing ───────────────────────────────────────
            string processingStatus;
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
                    "Failed to dispatch ProcessIngestionPayload for promotion execution {ExecutionId}",
                    execution.Id.Value);
                processingStatus = "metadata_recorded";
            }

            return Results.Accepted(null, new
            {
                message = "Promotion event received",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value
            });
        })
        .WithName("PostPromotionEvent")
        .WithSummary("Notify a promotion event between environments")
        .WithDescription("Receives promotion event notifications")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
