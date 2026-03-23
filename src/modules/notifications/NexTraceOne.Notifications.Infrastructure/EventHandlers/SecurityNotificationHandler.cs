using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de segurança do módulo Identity Access.
/// Gera notificações internas quando acessos break-glass de emergência são ativados.
/// </summary>
internal sealed class SecurityNotificationHandler(
    INotificationModule notificationModule,
    ILogger<SecurityNotificationHandler> logger)
    : IIntegrationEventHandler<BreakGlassActivatedIntegrationEvent>
{
    public async Task HandleAsync(BreakGlassActivatedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing BreakGlassActivated notification for user {UserId}",
            @event.UserId);

        if (@event.TenantId is null)
        {
            logger.LogWarning("BreakGlassActivated event missing TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ActivatedBy,
            @event.Resource,
            @event.Reason
        });

        // Break-glass notifica o próprio utilizador e admins
        // Nesta fase, o owner recebe. Admins serão adicionados na Fase 3 com role resolution.
        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.BreakGlassActivated,
            Category = nameof(NotificationCategory.Security),
            Severity = nameof(NotificationSeverity.Critical),
            Title = "Break-glass access activated",
            Message = $"Emergency break-glass access was activated by {@event.ActivatedBy} for resource {@event.Resource}. Reason: {@event.Reason}. Review immediately.",
            SourceModule = "Identity",
            SourceEntityType = "BreakGlassAccess",
            SourceEntityId = @event.UserId.ToString(),
            ActionUrl = $"/security/break-glass/{@event.UserId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.UserId],
            PayloadJson = payload
        }, ct);
    }
}
