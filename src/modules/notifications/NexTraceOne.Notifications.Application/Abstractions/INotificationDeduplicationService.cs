namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de deduplicação básica de notificações.
/// Evita spam e duplicação óbvia ao verificar se uma notificação idêntica
/// já foi criada recentemente para o mesmo destinatário, tipo e entidade de origem.
/// </summary>
public interface INotificationDeduplicationService
{
    /// <summary>
    /// Verifica se uma notificação duplicada existe no intervalo recente.
    /// </summary>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="recipientUserId">Id do destinatário.</param>
    /// <param name="eventType">Tipo de evento (NotificationType).</param>
    /// <param name="sourceEntityId">Id da entidade de origem (pode ser null).</param>
    /// <param name="windowMinutes">Janela temporal em minutos para verificar duplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se já existe uma notificação duplicada recente.</returns>
    Task<bool> IsDuplicateAsync(
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        string? sourceEntityId,
        int windowMinutes = 5,
        CancellationToken cancellationToken = default);
}
