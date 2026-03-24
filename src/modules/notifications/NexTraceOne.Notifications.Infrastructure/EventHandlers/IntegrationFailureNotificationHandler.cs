using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de falha de integração do módulo Operational Intelligence.
/// Gera notificações internas quando integrações, sincronizações ou conectores falham.
/// Fase 5: adicionados SyncFailed e ConnectorAuthFailed.
/// </summary>
internal sealed class IntegrationFailureNotificationHandler(
    INotificationModule notificationModule,
    ILogger<IntegrationFailureNotificationHandler> logger)
    : IIntegrationEventHandler<IntegrationFailedIntegrationEvent>,
      IIntegrationEventHandler<SyncFailedIntegrationEvent>,
      IIntegrationEventHandler<ConnectorAuthFailedIntegrationEvent>
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

    public async Task HandleAsync(SyncFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing SyncFailed notification for integration {IntegrationId}, name {IntegrationName}",
            @event.IntegrationId, @event.IntegrationName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("SyncFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.IntegrationName,
            @event.ErrorMessage
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.SyncFailed,
            Category = nameof(NotificationCategory.Integration),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Sync failed — {@event.IntegrationName}",
            Message = $"Synchronization for {@event.IntegrationName} has failed: {@event.ErrorMessage}. Check data source and retry.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Integration",
            SourceEntityId = @event.IntegrationId.ToString(),
            ActionUrl = $"/integrations/{@event.IntegrationId}/sync",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(ConnectorAuthFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ConnectorAuthFailed notification for connector {ConnectorId}, name {ConnectorName}",
            @event.ConnectorId, @event.ConnectorName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ConnectorAuthFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ConnectorName,
            @event.ErrorMessage
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ConnectorAuthFailed,
            Category = nameof(NotificationCategory.Integration),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Connector auth failed — {@event.ConnectorName}",
            Message = $"Authentication for connector {@event.ConnectorName} has failed: {@event.ErrorMessage}. Re-authenticate to restore data flow.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Connector",
            SourceEntityId = @event.ConnectorId.ToString(),
            ActionUrl = $"/integrations/connectors/{@event.ConnectorId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
