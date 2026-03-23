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
    /// </summary>
    Task ProcessExternalDeliveryAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
