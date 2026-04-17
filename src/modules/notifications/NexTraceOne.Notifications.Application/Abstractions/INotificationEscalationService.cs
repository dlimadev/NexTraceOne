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
    /// <param name="criticalThresholdMinutes">
    /// Minutos sem acknowledge para considerar escalação de notificações Critical.
    /// Defaults para 30 se não especificado.
    /// </param>
    /// <param name="actionRequiredThresholdMinutes">
    /// Minutos sem acknowledge para considerar escalação de notificações ActionRequired.
    /// Defaults para 120 se não especificado.
    /// </param>
    /// <returns>True se a notificação deve ser escalada.</returns>
    bool ShouldEscalate(
        Notification notification,
        int criticalThresholdMinutes = 30,
        int actionRequiredThresholdMinutes = 120);

    /// <summary>
    /// Processa escalação: marca como escalada e gera acções de escalonamento.
    /// </summary>
    Task EscalateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
