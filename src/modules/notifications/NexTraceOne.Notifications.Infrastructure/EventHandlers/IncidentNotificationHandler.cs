using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de incidentes do módulo Operational Intelligence.
/// Gera notificações internas quando incidentes são criados ou escalados.
/// </summary>
internal sealed class IncidentNotificationHandler(
    INotificationModule notificationModule,
    ILogger<IncidentNotificationHandler> logger)
    : IIntegrationEventHandler<IncidentCreatedIntegrationEvent>,
      IIntegrationEventHandler<IncidentEscalatedIntegrationEvent>
{
    public async Task HandleAsync(IncidentCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing IncidentCreated notification for incident {IncidentId}, service {ServiceName}",
            @event.IncidentId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("IncidentCreated event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.IncidentSeverity,
            @event.Description
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Incident created — {@event.ServiceName}",
            Message = $"A new incident with severity {@event.IncidentSeverity} has been created for service {@event.ServiceName}. Investigate and take action.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Incident",
            SourceEntityId = @event.IncidentId.ToString(),
            ActionUrl = $"/incidents/{@event.IncidentId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(IncidentEscalatedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing IncidentEscalated notification for incident {IncidentId}",
            @event.IncidentId);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("IncidentEscalated event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.PreviousSeverity,
            @event.NewSeverity
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.IncidentEscalated,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Incident escalated — {@event.ServiceName}",
            Message = $"An incident for service {@event.ServiceName} has been escalated from {@event.PreviousSeverity} to {@event.NewSeverity}. Immediate attention required.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Incident",
            SourceEntityId = @event.IncidentId.ToString(),
            ActionUrl = $"/incidents/{@event.IncidentId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
