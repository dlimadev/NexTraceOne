using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de sincronização de consumidores e dependências.
/// Recebe declarações de consumidores de contratos e dependências entre serviços
/// e regista a execução de ingestão para rastreabilidade.
/// </summary>
internal static class ConsumerSyncEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de sincronização de consumidores no grupo raiz de consumers.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/sync", async (
            HttpContext httpContext,
            ConsumerSyncRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var connector = await connectorRepo.GetByNameAsync("consumer-sync", ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: "consumer-sync",
                    connectorType: "Dependencies",
                    description: "Consumer and dependency updates",
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

            execution.CompleteSuccess(
                itemsProcessed: request.Consumers?.Count ?? 1,
                itemsSucceeded: request.Consumers?.Count ?? 1,
                utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);

            await executionRepo.AddAsync(execution, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Consumer update received",
                status = "accepted",
                processingStatus = "metadata_recorded",
                note = "Update metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
                correlationId,
                executionId = execution.Id.Value
            });
        })
        .WithName("PostConsumerSync")
        .WithSummary("Synchronize service consumers and dependency declarations")
        .WithDescription("Receives consumer/dependency update notifications")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
