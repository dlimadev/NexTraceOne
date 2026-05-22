using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;

namespace NexTraceOne.Catalog.Infrastructure.Graph.EventHandlers;

/// <summary>
/// Consome evento de deployment para:
/// 1. Registar sinal operacional no catálogo (overlay de health via NodeHealthRecord).
/// 2. Fazer upsert de AssetDeploymentState — mantém "qual versão está onde".
/// </summary>
internal sealed class DeploymentEventReceivedCatalogHandler(
    INodeHealthRepository nodeHealthRepository,
    IAssetDeploymentStateRepository deploymentStateRepository,
    IDiscoveredServiceRepository discoveredServiceRepository,
    ICatalogGraphUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<DeploymentEventReceivedCatalogHandler> logger) : IIntegrationEventHandler<DeploymentEventReceivedEvent>
{
    public async Task HandleAsync(DeploymentEventReceivedEvent @event, CancellationToken ct = default)
    {
        var isProduction = string.Equals(@event.Environment, "production", StringComparison.OrdinalIgnoreCase)
            || string.Equals(@event.Environment, "prod", StringComparison.OrdinalIgnoreCase);

        // ── Overlay de health ────────────────────────────────────────────────
        var status = isProduction ? HealthStatus.Degraded : HealthStatus.Healthy;
        var score = isProduction ? 0.75m : 0.35m;

        var record = NodeHealthRecord.Create(
            nodeId: @event.DeploymentId,
            nodeType: NodeType.Service,
            overlayMode: OverlayMode.Health,
            status: status,
            score: score,
            factorsJson: $$"""
                {"source":"ChangeGovernance","event":"DeploymentEventReceived","version":"{{@event.Version}}","environment":"{{@event.Environment}}"}
                """,
            calculatedAt: clock.UtcNow,
            sourceSystem: @event.SourceSystem);

        nodeHealthRepository.Add(record);

        // ── Upsert AssetDeploymentState ──────────────────────────────────────
        // Tenta correlacionar com um ServiceAsset via DiscoveredService
        var discovered = await discoveredServiceRepository
            .GetByNameAndEnvironmentAsync(@event.SourceSystem, @event.Environment, ct);

        if (discovered?.MatchedServiceAssetId is Guid matchedId)
        {
            var serviceAssetId = ServiceAssetId.From(matchedId);
            var existing = await deploymentStateRepository
                .GetByServiceAndEnvironmentAsync(serviceAssetId, @event.Environment, ct);

            if (existing is null)
            {
                deploymentStateRepository.Add(
                    AssetDeploymentState.Record(
                        serviceAssetId,
                        tenantId: Guid.Empty,
                        environment: @event.Environment,
                        imageTag: @event.Version,
                        releaseName: @event.DeploymentId.ToString(),
                        deployedAt: @event.ReceivedAt));
            }
            else
            {
                existing.UpdateDeployment(@event.Version, @event.DeploymentId.ToString(), @event.ReceivedAt);
            }
        }

        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Catalog deployment signal recorded for {DeploymentId} ({Environment})",
            @event.DeploymentId, @event.Environment);
    }
}
