using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Preferences;

/// <summary>
/// Política de notificações obrigatórias da plataforma NexTraceOne.
/// Define quais eventos e severidades não podem ser desativados pelo utilizador.
///
/// Regras de obrigatoriedade:
///   - BreakGlassActivated → obrigatório, InApp + Email + Teams
///   - IncidentCreated/IncidentEscalated com Critical → obrigatório, InApp + Email + Teams
///   - ApprovalPending → obrigatório, InApp + Email
///   - ComplianceCheckFailed → obrigatório, InApp + Email
///   - Qualquer severidade Critical → obrigatório, InApp + Email (mínimo)
///   - Outros eventos → não obrigatório (preferências do utilizador aplicam-se)
/// </summary>
internal sealed class MandatoryNotificationPolicy : IMandatoryNotificationPolicy
{
    private static readonly IReadOnlyList<DeliveryChannel> AllChannels =
        [DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams];

    private static readonly IReadOnlyList<DeliveryChannel> InAppAndEmail =
        [DeliveryChannel.InApp, DeliveryChannel.Email];

    /// <inheritdoc/>
    public bool IsMandatory(string eventType, NotificationCategory category, NotificationSeverity severity)
    {
        if (string.Equals(eventType, NotificationType.BreakGlassActivated, StringComparison.Ordinal))
            return true;

        if (IsIncidentCritical(eventType, severity))
            return true;

        if (string.Equals(eventType, NotificationType.ApprovalPending, StringComparison.Ordinal))
            return true;

        if (string.Equals(eventType, NotificationType.ComplianceCheckFailed, StringComparison.Ordinal))
            return true;

        if (severity == NotificationSeverity.Critical)
            return true;

        return false;
    }

    /// <inheritdoc/>
    public IReadOnlyList<DeliveryChannel> GetMandatoryChannels(
        string eventType,
        NotificationCategory category,
        NotificationSeverity severity)
    {
        if (string.Equals(eventType, NotificationType.BreakGlassActivated, StringComparison.Ordinal))
            return AllChannels;

        if (IsIncidentCritical(eventType, severity))
            return AllChannels;

        if (string.Equals(eventType, NotificationType.ApprovalPending, StringComparison.Ordinal))
            return InAppAndEmail;

        if (string.Equals(eventType, NotificationType.ComplianceCheckFailed, StringComparison.Ordinal))
            return InAppAndEmail;

        if (severity == NotificationSeverity.Critical)
            return InAppAndEmail;

        return [];
    }

    private static bool IsIncidentCritical(string eventType, NotificationSeverity severity) =>
        severity == NotificationSeverity.Critical &&
        (string.Equals(eventType, NotificationType.IncidentCreated, StringComparison.Ordinal) ||
         string.Equals(eventType, NotificationType.IncidentEscalated, StringComparison.Ordinal));
}
