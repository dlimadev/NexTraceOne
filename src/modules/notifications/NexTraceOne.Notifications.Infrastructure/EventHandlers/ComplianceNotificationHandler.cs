using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Governance.Contracts;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de compliance do módulo Governance.
/// Gera notificações internas quando verificações de compliance falham.
/// </summary>
internal sealed class ComplianceNotificationHandler(
    INotificationModule notificationModule,
    ILogger<ComplianceNotificationHandler> logger)
    : IIntegrationEventHandler<IntegrationEvents.ComplianceCheckFailedIntegrationEvent>
{
    public async Task HandleAsync(
        IntegrationEvents.ComplianceCheckFailedIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ComplianceCheckFailed notification for report {ReportId}, service {ServiceName}",
            @event.ReportId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ComplianceCheckFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.GapCount,
            @event.ReportId
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ComplianceCheckFailed,
            Category = nameof(NotificationCategory.Compliance),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Compliance check failed — {@event.ServiceName}",
            Message = $"{@event.GapCount} compliance gap(s) detected for {@event.ServiceName}. Review and remediate.",
            SourceModule = "Governance",
            SourceEntityType = "ComplianceReport",
            SourceEntityId = @event.ReportId,
            ActionUrl = $"/governance/compliance/{@event.ReportId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
