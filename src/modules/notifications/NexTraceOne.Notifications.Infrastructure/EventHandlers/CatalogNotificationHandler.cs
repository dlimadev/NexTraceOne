using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de catálogo e contratos do módulo Catalog.
/// Gera notificações internas quando contratos são publicados, breaking changes são detetadas
/// ou validações de contrato falham.
/// Fase 5: handler completo de domínio de contratos.
/// </summary>
internal sealed class CatalogNotificationHandler(
    INotificationModule notificationModule,
    ILogger<CatalogNotificationHandler> logger)
    : IIntegrationEventHandler<ContractPublishedIntegrationEvent>,
      IIntegrationEventHandler<BreakingChangeDetectedIntegrationEvent>,
      IIntegrationEventHandler<ContractValidationFailedIntegrationEvent>
{
    public async Task HandleAsync(ContractPublishedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ContractPublished notification for contract {ContractId}, service {ServiceName}",
            @event.ContractId, @event.ServiceName);

        if (@event.PublisherUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ContractPublished event missing PublisherUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ContractName,
            @event.ServiceName,
            @event.Version
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ContractPublished,
            Category = nameof(NotificationCategory.Contract),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"Contract published — {@event.ContractName} v{@event.Version}",
            Message = $"Contract {@event.ContractName} version {@event.Version} has been published for service {@event.ServiceName}.",
            SourceModule = "Catalog",
            SourceEntityType = "Contract",
            SourceEntityId = @event.ContractId.ToString(),
            ActionUrl = $"/contracts/{@event.ContractId}",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.PublisherUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(BreakingChangeDetectedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing BreakingChangeDetected notification for contract {ContractId}, service {ServiceName}",
            @event.ContractId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("BreakingChangeDetected event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ContractName,
            @event.ServiceName,
            @event.Description
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.BreakingChangeDetected,
            Category = nameof(NotificationCategory.Contract),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Breaking change — {@event.ContractName}",
            Message = $"A breaking change has been detected in contract {@event.ContractName} for service {@event.ServiceName}: {@event.Description}. Review impact on consumers.",
            SourceModule = "Catalog",
            SourceEntityType = "Contract",
            SourceEntityId = @event.ContractId.ToString(),
            ActionUrl = $"/contracts/{@event.ContractId}/changes",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(ContractValidationFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ContractValidationFailed notification for contract {ContractId}, service {ServiceName}",
            @event.ContractId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ContractValidationFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ContractName,
            @event.ServiceName,
            @event.ValidationError
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ContractValidationFailed,
            Category = nameof(NotificationCategory.Contract),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Validation failed — {@event.ContractName}",
            Message = $"Contract {@event.ContractName} for service {@event.ServiceName} failed validation: {@event.ValidationError}. Fix and revalidate.",
            SourceModule = "Catalog",
            SourceEntityType = "Contract",
            SourceEntityId = @event.ContractId.ToString(),
            ActionUrl = $"/contracts/{@event.ContractId}/validation",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
