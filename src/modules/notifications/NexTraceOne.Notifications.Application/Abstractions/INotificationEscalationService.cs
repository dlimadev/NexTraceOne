using NexTraceOne.Notifications.Domain.Entities;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de escalação para notificações críticas não tratadas.
/// Identifica notificações que precisam de escalação e age sobre elas.
/// </summary>
public interface INotificationEscalationService
{
    /// <summary>
    /// Verifica se uma notificação deve ser escalada com base no seu estado e tempo.
    /// </summary>
    /// <param name="notification">A notificação a avaliar.</param>
    /// <returns>True se a notificação deve ser escalada.</returns>
    bool ShouldEscalate(Notification notification);

    /// <summary>
    /// Processa escalação: marca como escalada e gera acções de escalonamento.
    /// </summary>
    Task EscalateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
