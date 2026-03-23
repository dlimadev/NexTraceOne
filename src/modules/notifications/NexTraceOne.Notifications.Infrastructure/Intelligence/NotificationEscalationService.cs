using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de escalação para notificações críticas não tratadas.
///
/// Critérios de escalação:
///   - Critical + não acknowledged + mais de 30 minutos → escalável
///   - ActionRequired + não acknowledged + mais de 2 horas → escalável
///
/// A escalação marca a notificação e pode acionar reenvio por canal mais forte.
/// </summary>
internal sealed class NotificationEscalationService(
    ILogger<NotificationEscalationService> logger) : INotificationEscalationService
{
    private static readonly TimeSpan CriticalThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ActionRequiredThreshold = TimeSpan.FromHours(2);

    /// <inheritdoc/>
    public bool ShouldEscalate(Notification notification)
    {
        // Já escalada, acknowledged, ou arquivada — não escalável
        if (notification.IsEscalated
            || notification.Status is NotificationStatus.Acknowledged
                or NotificationStatus.Archived
                or NotificationStatus.Dismissed)
            return false;

        // Snoozed — não escalar enquanto está snoozed
        if (notification.IsSnoozed())
            return false;

        var age = DateTimeOffset.UtcNow - notification.CreatedAt;

        return notification.Severity switch
        {
            NotificationSeverity.Critical => age > CriticalThreshold,
            NotificationSeverity.ActionRequired when notification.RequiresAction => age > ActionRequiredThreshold,
            _ => false
        };
    }

    /// <inheritdoc/>
    public Task EscalateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        notification.MarkAsEscalated();

        logger.LogWarning(
            "Notification escalated: Id={NotificationId}, Type={EventType}, Severity={Severity}, Age={Age}",
            notification.Id.Value,
            notification.EventType,
            notification.Severity,
            DateTimeOffset.UtcNow - notification.CreatedAt);

        return Task.CompletedTask;
    }
}
