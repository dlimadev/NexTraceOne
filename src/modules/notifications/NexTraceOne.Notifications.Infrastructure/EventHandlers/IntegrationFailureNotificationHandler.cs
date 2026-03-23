using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de falha de integração do módulo Operational Intelligence.
/// Gera notificações internas quando integrações ou ingestões falham.
/// </summary>
internal sealed class IntegrationFailureNotificationHandler(
    INotificationModule notificationModule,
    ILogger<IntegrationFailureNotificationHandler> logger)
    : IIntegrationEventHandler<IntegrationFailedIntegrationEvent>
{
    public async Task HandleAsync(IntegrationFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing IntegrationFailed notification for integration {IntegrationId}, name {IntegrationName}",
            @event.IntegrationId, @event.IntegrationName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("IntegrationFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.IntegrationName,
            @event.ErrorMessage
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.IntegrationFailed,
            Category = nameof(NotificationCategory.Integration),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Integration failed — {@event.IntegrationName}",
            Message = $"Integration {@event.IntegrationName} has failed: {@event.ErrorMessage}. Check configuration and retry.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Integration",
            SourceEntityId = @event.IntegrationId.ToString(),
            ActionUrl = $"/integrations/{@event.IntegrationId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
