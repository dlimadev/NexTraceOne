using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Integrations.Contracts;
using NexTraceOne.Integrations.Domain.Events;

namespace NexTraceOne.Integrations.Infrastructure.EventHandlers;

/// <summary>
/// Converte o domain event IngestionPayloadProcessedDomainEvent num integration event
/// publicado para consumidores downstream (Change Intelligence, Operational Intelligence).
///
/// Padrão: domain event → integration event → handlers cross-module.
/// O domain event percorre o outbox de IntegrationsDbContext;
/// este handler é invocado quando o ModuleOutboxProcessorJob o processa.
/// </summary>
internal sealed class IngestionPayloadProcessedDomainEventHandler(
    IEventBus eventBus,
    ICurrentTenant currentTenant,
    ILogger<IngestionPayloadProcessedDomainEventHandler> logger)
    : IIntegrationEventHandler<IngestionPayloadProcessedDomainEvent>
{
    public async Task HandleAsync(IngestionPayloadProcessedDomainEvent @event, CancellationToken ct = default)
    {
        var integrationEvent = new IntegrationEvents.IngestionPayloadProcessedIntegrationEvent(
            ExecutionId: @event.ExecutionId,
            ServiceName: @event.ServiceName,
            Environment: @event.Environment,
            Version: @event.Version,
            CommitSha: @event.CommitSha,
            ChangeType: @event.ChangeType,
            ProcessedAt: @event.ProcessedAt,
            TenantId: currentTenant.Id);

        await eventBus.PublishAsync(integrationEvent, ct);

        logger.LogInformation(
            "IngestionPayloadProcessed integration event published for execution {ExecutionId}, service {ServiceName}",
            @event.ExecutionId,
            @event.ServiceName);
    }
}
