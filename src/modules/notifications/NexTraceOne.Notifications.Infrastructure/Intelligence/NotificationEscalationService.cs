using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de escalação para notificações críticas não tratadas.
///
/// Os thresholds são recebidos como parâmetros opcionais por <see cref="ShouldEscalate"/>,
/// permitindo que o caller (ex: <see cref="NotificationEscalationScanJob"/>) os leia de
/// configuração e os passe dinamicamente.
/// Os defaults (30 min para Critical, 120 min para ActionRequired) são usados quando
/// os valores de configuração não estão disponíveis.
///
/// A escalação marca a notificação e pode accionar reenvio por canal mais forte.
/// </summary>
internal sealed class NotificationEscalationService(
    ILogger<NotificationEscalationService> logger) : INotificationEscalationService
{
    private const int DefaultCriticalThresholdMinutes = 30;
    private const int DefaultActionRequiredThresholdMinutes = 120;

    /// <inheritdoc/>
    public bool ShouldEscalate(
        Notification notification,
        int criticalThresholdMinutes = DefaultCriticalThresholdMinutes,
        int actionRequiredThresholdMinutes = DefaultActionRequiredThresholdMinutes)
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
            NotificationSeverity.Critical => age > TimeSpan.FromMinutes(criticalThresholdMinutes),
            NotificationSeverity.ActionRequired when notification.RequiresAction
                => age > TimeSpan.FromMinutes(actionRequiredThresholdMinutes),
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
