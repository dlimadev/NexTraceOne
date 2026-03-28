using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;

namespace NexTraceOne.Catalog.Infrastructure.Graph.EventHandlers;

/// <summary>
/// Consome evento de deployment para registrar sinal operacional no catálogo (overlay de health).
/// </summary>
internal sealed class DeploymentEventReceivedCatalogHandler(
    INodeHealthRepository nodeHealthRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<DeploymentEventReceivedCatalogHandler> logger) : IIntegrationEventHandler<DeploymentEventReceivedEvent>
{
    public async Task HandleAsync(DeploymentEventReceivedEvent @event, CancellationToken ct = default)
    {
        var isProduction = string.Equals(@event.Environment, "production", StringComparison.OrdinalIgnoreCase)
            || string.Equals(@event.Environment, "prod", StringComparison.OrdinalIgnoreCase);

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
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Catalog node health signal recorded for deployment {DeploymentId}",
            @event.DeploymentId);
    }
}
