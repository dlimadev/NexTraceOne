using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Motor de roteamento de notificações.
/// Determina quais canais devem ser utilizados para uma notificação,
/// com base na severidade, categoria, preferências do utilizador e regras da plataforma.
/// </summary>
public interface INotificationRoutingEngine
{
    /// <summary>
    /// Resolve os canais de entrega elegíveis para uma notificação.
    /// Considera severidade, categoria, preferências do utilizador e regras de fallback.
    /// </summary>
    Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(
        Guid recipientUserId,
        NotificationCategory category,
        NotificationSeverity severity,
        CancellationToken cancellationToken = default);
}
