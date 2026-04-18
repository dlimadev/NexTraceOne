using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de sincronização de contratos de fontes externas.
/// Recebe contratos de APIs e eventos de sistemas externos
/// e regista a execução de ingestão para rastreabilidade e correlação futura.
/// </summary>
internal static class ContractSyncEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de sincronização de contratos no grupo raiz de contracts.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/sync", async (
            HttpContext httpContext,
            ContractSyncRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var connector = await connectorRepo.GetByNameAsync("contract-sync", ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: "contract-sync",
                    connectorType: "ContractImport",
                    description: "External contract synchronization",
                    provider: request.Provider ?? "External",
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
                itemsProcessed: request.Contracts?.Count ?? 1,
                itemsSucceeded: request.Contracts?.Count ?? 1,
                utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);

            await executionRepo.AddAsync(execution, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Contract sync received",
                status = "accepted",
                processingStatus = "metadata_recorded",
                note = "Sync metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
                correlationId,
                executionId = execution.Id.Value
            });
        })
        .WithName("PostContractSync")
        .WithSummary("Synchronize API or event contracts from an external source")
        .WithDescription("Receives contract synchronization from external sources")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
