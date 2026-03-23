using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de supressão de notificações.
///
/// Regras de supressão:
///   1. Notificação já acknowledged para a mesma entidade recentemente → suprimir
///   2. Notificação snoozed activa para o mesmo tipo/entidade → suprimir
///   3. Grupo correlato activo já tem notificação não tratada → suprimir se elegível
///
/// Regras de segurança:
///   - Notificações Critical nunca são suprimidas
///   - Notificações obrigatórias (BreakGlass, Approval, Compliance) nunca são suprimidas
/// </summary>
internal sealed class NotificationSuppressionService(
    NotificationsDbContext context,
    IMandatoryNotificationPolicy mandatoryPolicy) : INotificationSuppressionService
{
    /// <inheritdoc/>
    public async Task<SuppressionResult> EvaluateAsync(
        NotificationRequest request,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        // Notificações obrigatórias nunca são suprimidas
        var category = Enum.TryParse<NotificationCategory>(request.Category, true, out var cat)
            ? cat : NotificationCategory.Informational;
        var severity = Enum.TryParse<NotificationSeverity>(request.Severity, true, out var sev)
            ? sev : NotificationSeverity.Info;

        if (mandatoryPolicy.IsMandatory(request.EventType, category, severity))
            return SuppressionResult.Allow();

        if (severity == NotificationSeverity.Critical)
            return SuppressionResult.Allow();

        if (!request.TenantId.HasValue)
            return SuppressionResult.Allow();

        var tenantId = request.TenantId.Value;

        // Regra 1: Já acknowledged para mesma entidade recentemente (últimos 30 min)
        if (!string.IsNullOrWhiteSpace(request.SourceEntityId))
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-30);
            var alreadyAcknowledged = await context.Notifications
                .AnyAsync(n => n.TenantId == tenantId
                            && n.RecipientUserId == recipientUserId
                            && n.EventType == request.EventType
                            && n.SourceEntityId == request.SourceEntityId
                            && n.Status == NotificationStatus.Acknowledged
                            && n.AcknowledgedAt >= cutoff,
                    cancellationToken);

            if (alreadyAcknowledged)
                return SuppressionResult.SuppressWith("Already acknowledged for same entity within 30 minutes");
        }

        // Regra 2: Snoozed activa para o mesmo tipo/entidade
        if (!string.IsNullOrWhiteSpace(request.SourceEntityId))
        {
            var now = DateTimeOffset.UtcNow;
            var isSnoozed = await context.Notifications
                .AnyAsync(n => n.TenantId == tenantId
                            && n.RecipientUserId == recipientUserId
                            && n.EventType == request.EventType
                            && n.SourceEntityId == request.SourceEntityId
                            && n.SnoozedUntil != null
                            && n.SnoozedUntil > now,
                    cancellationToken);

            if (isSnoozed)
                return SuppressionResult.SuppressWith("Active snooze exists for same event/entity");
        }

        return SuppressionResult.Allow();
    }
}
