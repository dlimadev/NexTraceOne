using NexTraceOne.BuildingBlocks.Core.Notifications;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Repositório para persistência e consulta de registros de entrega de notificação.
/// Cada delivery rastreia uma tentativa de envio por canal específico,
/// permitindo monitoramento de retries, falhas e status por provedor.
/// </summary>
public interface INotificationDeliveryRepository
{
    /// <summary>
    /// Obtém um registro de entrega pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador da entrega.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Registro de entrega ou null se não encontrado.</returns>
    Task<NotificationDelivery?> GetByIdAsync(
        NotificationDeliveryId id,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém todos os registros de entrega de uma notificação.
    /// </summary>
    /// <param name="notificationRequestId">Id da notificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de entregas da notificação.</returns>
    Task<IReadOnlyList<NotificationDelivery>> GetByNotificationRequestIdAsync(
        NotificationId notificationRequestId,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém entregas pendentes de retry (com status Failed e tentativas abaixo do limite).
    /// </summary>
    /// <param name="maxAttempts">Número máximo de tentativas antes de desistir.</param>
    /// <param name="batchSize">Tamanho do lote para processamento.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de entregas elegíveis para retry.</returns>
    Task<IReadOnlyList<NotificationDelivery>> GetPendingRetryAsync(
        int maxAttempts,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>Persiste um novo registro de entrega.</summary>
    /// <param name="delivery">Registro de entrega a ser persistido.</param>
    void Add(NotificationDelivery delivery);
}
