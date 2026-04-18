using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de sinais de runtime.
/// Recebe sinais e marcadores operacionais de serviços em execução
/// e regista a execução de ingestão para rastreabilidade.
/// </summary>
internal static class RuntimeSignalEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de sinais de runtime no grupo raiz de runtime.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
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
                note = "Signal metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
                correlationId,
                executionId = execution.Id.Value
            });
        })
        .WithName("PostRuntimeSignal")
        .WithSummary("Submit a runtime signal or operational marker from a monitored service")
        .WithDescription("Receives runtime signals and markers from monitored services")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
