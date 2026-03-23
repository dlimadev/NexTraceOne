using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Orquestrador central da plataforma de notificações.
/// Recebe pedidos de notificação, decide se notifica, quem, por qual canal,
/// com qual template e com qual severidade. Aplica regras de roteamento,
/// resolve destinatários e despacha para a central interna e canais externos.
/// </summary>
public interface INotificationOrchestrator
{
    /// <summary>
    /// Processa um pedido de notificação.
    /// Resolve destinatários, aplica regras de roteamento, persiste na central interna
    /// e despacha para canais externos elegíveis.
    /// </summary>
    Task<NotificationResult> ProcessAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);
}
