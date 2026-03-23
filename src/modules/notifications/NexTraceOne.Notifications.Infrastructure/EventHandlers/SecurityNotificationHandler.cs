using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de segurança do módulo Identity Access.
/// Gera notificações internas para break-glass, JIT access, role changes e access reviews.
/// Fase 5: adicionados UserRoleChanged, JitAccessGranted e AccessReviewPending.
/// </summary>
internal sealed class SecurityNotificationHandler(
    INotificationModule notificationModule,
    ILogger<SecurityNotificationHandler> logger)
    : IIntegrationEventHandler<BreakGlassActivatedIntegrationEvent>,
      IIntegrationEventHandler<UserRoleChangedIntegrationEvent>,
      IIntegrationEventHandler<JitAccessGrantedIntegrationEvent>,
      IIntegrationEventHandler<AccessReviewPendingIntegrationEvent>
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

    public async Task HandleAsync(UserRoleChangedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing UserRoleChanged notification for user {UserId}, role {RoleName}",
            @event.UserId, @event.RoleName);

        if (@event.TenantId is null)
        {
            logger.LogWarning("UserRoleChanged event missing TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.RoleName
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.UserRoleChanged,
            Category = nameof(NotificationCategory.Security),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"Role changed — {@event.RoleName}",
            Message = $"Your role has been changed to {@event.RoleName}. Contact your administrator if this was unexpected.",
            SourceModule = "Identity",
            SourceEntityType = "UserRole",
            SourceEntityId = @event.UserId.ToString(),
            ActionUrl = $"/settings/profile",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.UserId],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(JitAccessGrantedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing JitAccessGranted notification for user {UserId}, resource {Resource}",
            @event.UserId, @event.Resource);

        if (@event.TenantId is null)
        {
            logger.LogWarning("JitAccessGranted event missing TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.Resource,
            @event.GrantedBy,
            ExpiresAt = @event.ExpiresAt.ToString("O")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.JitAccessGranted,
            Category = nameof(NotificationCategory.Security),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"JIT access granted — {@event.Resource}",
            Message = $"Just-in-time access to {@event.Resource} has been granted by {@event.GrantedBy}. Expires at {@event.ExpiresAt:g}.",
            SourceModule = "Identity",
            SourceEntityType = "JitAccess",
            SourceEntityId = @event.UserId.ToString(),
            ActionUrl = $"/security/jit-access",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.UserId],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(AccessReviewPendingIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing AccessReviewPending notification for review {ReviewId}, scope {ReviewScope}",
            @event.ReviewId, @event.ReviewScope);

        if (@event.AssigneeUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("AccessReviewPending event missing AssigneeUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ReviewScope,
            DueDate = @event.DueDate.ToString("O")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.AccessReviewPending,
            Category = nameof(NotificationCategory.Security),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = $"Access review pending — {@event.ReviewScope}",
            Message = $"An access review for {@event.ReviewScope} is pending. Due date: {@event.DueDate:d}. Complete the review.",
            SourceModule = "Identity",
            SourceEntityType = "AccessReview",
            SourceEntityId = @event.ReviewId.ToString(),
            ActionUrl = $"/security/access-reviews/{@event.ReviewId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.AssigneeUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
