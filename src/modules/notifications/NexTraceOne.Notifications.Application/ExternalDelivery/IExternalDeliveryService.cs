using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.ExternalDelivery;

/// <summary>
/// Serviço central de entrega externa de notificações.
/// Coordena roteamento, dispatch e delivery log para canais externos (Email, Teams).
/// </summary>
public interface IExternalDeliveryService
{
    /// <summary>
    /// Processa a entrega externa de uma notificação pelos canais elegíveis.
    /// Cria delivery records, invoca dispatchers e regista status.
    /// Em caso de falha transitória, agenda retry via NotificationDelivery.ScheduleRetry().
    /// </summary>
    Task ProcessExternalDeliveryAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reexecuta a entrega de um registo de delivery existente (scheduled retry).
    /// Chamado pelo NotificationDeliveryRetryJob para processar retries agendados.
    /// </summary>
    Task RetryDeliveryAsync(
        NotificationDelivery delivery,
        Notification notification,
        CancellationToken cancellationToken = default);
}
