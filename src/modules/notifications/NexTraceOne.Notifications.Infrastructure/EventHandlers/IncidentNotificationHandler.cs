using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de incidentes do módulo Operational Intelligence.
/// Gera notificações internas quando incidentes são criados, escalados ou resolvidos.
/// Fase 5: adicionados IncidentResolved, AnomalyDetected e HealthDegradation.
/// </summary>
internal sealed class IncidentNotificationHandler(
    INotificationModule notificationModule,
    ILogger<IncidentNotificationHandler> logger)
    : IIntegrationEventHandler<IncidentCreatedIntegrationEvent>,
      IIntegrationEventHandler<IncidentEscalatedIntegrationEvent>,
      IIntegrationEventHandler<IncidentResolvedIntegrationEvent>,
      IIntegrationEventHandler<AnomalyDetectedIntegrationEvent>,
      IIntegrationEventHandler<HealthDegradationIntegrationEvent>
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

    public async Task HandleAsync(IncidentResolvedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing IncidentResolved notification for incident {IncidentId}, service {ServiceName}",
            @event.IncidentId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("IncidentResolved event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.ResolvedBy
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.IncidentResolved,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"Incident resolved — {@event.ServiceName}",
            Message = $"The incident for service {@event.ServiceName} has been resolved by {@event.ResolvedBy}.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Incident",
            SourceEntityId = @event.IncidentId.ToString(),
            ActionUrl = $"/incidents/{@event.IncidentId}",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(AnomalyDetectedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing AnomalyDetected notification for anomaly {AnomalyId}, service {ServiceName}",
            @event.AnomalyId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("AnomalyDetected event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.AnomalyType,
            @event.Description
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.AnomalyDetected,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Anomaly detected — {@event.ServiceName}",
            Message = $"A {FormatAnomalyType(@event.AnomalyType)} anomaly has been detected for service {@event.ServiceName}: {@event.Description}. Investigate promptly.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Anomaly",
            SourceEntityId = @event.AnomalyId.ToString(),
            ActionUrl = $"/operations/anomalies/{@event.AnomalyId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(HealthDegradationIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing HealthDegradation notification for service {ServiceId}, name {ServiceName}",
            @event.ServiceId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("HealthDegradation event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.PreviousStatus,
            @event.CurrentStatus
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.HealthDegradation,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Health degradation — {@event.ServiceName}",
            Message = $"Service {@event.ServiceName} health has degraded from {@event.PreviousStatus} to {@event.CurrentStatus}. Monitor and investigate.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Service",
            SourceEntityId = @event.ServiceId.ToString(),
            ActionUrl = $"/services/{@event.ServiceId}/health",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    private static string FormatAnomalyType(string anomalyType) =>
        anomalyType.ToLowerInvariant() switch
        {
            "runtime" => "runtime",
            "performance" => "performance",
            "drift" => "configuration drift",
            _ => anomalyType.ToLowerInvariant()
        };
}
