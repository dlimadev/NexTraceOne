using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using IngestCostSnapshotFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.IngestCostSnapshot.IngestCostSnapshot;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de snapshots de custo de infraestrutura.
/// Permite que exportadores de billing (AWS, Azure, GCP) e plataformas FinOps
/// (Apptio, CloudHealth) alimentem o NexTraceOne com dados de custo
/// para análise contextual e correlação com releases e serviços.
/// </summary>
internal static class CostIngestEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de ingestão de custo no grupo raiz de costs.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/snapshots", async (
            HttpContext httpContext,
            IngestCostSnapshotRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(CostIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Auditoria de ingestão ─────────────────────────────────────────────
            var sourceName = request.Source ?? "finops-exporter";
            var connector = await connectorRepo.GetByNameAsync(sourceName, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: sourceName.ToLowerInvariant().Replace(" ", "-"),
                    connectorType: "FinOps",
                    description: $"Auto-registered {sourceName} cost exporter connector",
                    provider: sourceName,
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var ingestionSource = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, "cost-snapshots", ct);
            if (ingestionSource is null)
            {
                ingestionSource = IngestionSource.Create(
                    connectorId: connector.Id,
                    name: "cost-snapshots",
                    sourceType: "ScheduledExport",
                    dataDomain: "FinOps",
                    description: $"Cost snapshots from {sourceName}",
                    endpoint: null,
                    expectedIntervalMinutes: 60,
                    utcNow: clock.UtcNow);
                await sourceRepo.AddAsync(ingestionSource, ct);
            }

            var execution = IngestionExecution.Start(connector.Id, ingestionSource.Id, correlationId, clock.UtcNow);

            // ── Dispatch para o domínio ───────────────────────────────────────────
            object? snapshotResult = null;
            string processingStatus;

            try
            {
                var command = new IngestCostSnapshotFeature.Command(
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    TotalCost: request.TotalCost,
                    CpuCostShare: request.CpuCostShare,
                    MemoryCostShare: request.MemoryCostShare,
                    NetworkCostShare: request.NetworkCostShare,
                    StorageCostShare: request.StorageCostShare,
                    CapturedAt: request.CapturedAt,
                    Source: request.Source ?? sourceName,
                    Period: request.Period,
                    Currency: request.Currency);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    snapshotResult = new
                    {
                        snapshotId = result.Value.SnapshotId,
                        serviceName = result.Value.ServiceName,
                        environment = result.Value.Environment,
                        totalCost = result.Value.TotalCost,
                        currency = result.Value.Currency,
                        capturedAt = result.Value.CapturedAt
                    };
                    processingStatus = "cost_snapshot_recorded";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected cost snapshot", null, clock.UtcNow);
                    logger.LogWarning(
                        "IngestCostSnapshot rejected for {ServiceName}/{Environment} period {Period}: {Error}",
                        request.ServiceName, request.Environment, request.Period, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error ingesting cost snapshot for {ServiceName}/{Environment}",
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
                message = "Cost snapshot accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                serviceName = request.ServiceName,
                environment = request.Environment,
                period = request.Period,
                snapshot = snapshotResult
            });
        })
        .WithName("PostIngestCostSnapshot")
        .WithSummary("Ingest an infrastructure cost snapshot for FinOps contextual analysis")
        .WithDescription(
            "Records a cost snapshot from billing exporters (AWS Cost Explorer, Azure Cost Management, GCP Billing) " +
            "or FinOps platforms (Apptio, CloudHealth). Enables contextual cost correlation with releases and teams.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
