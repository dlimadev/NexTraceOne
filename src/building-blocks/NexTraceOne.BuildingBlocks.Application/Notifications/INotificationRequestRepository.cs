using NexTraceOne.BuildingBlocks.Core.Notifications;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Repositório para persistência e consulta de requisições de notificação.
/// Cada requisição representa uma notificação a ser enviada a um ou mais destinatários,
/// com metadados de template, severidade, categoria e correlação.
/// </summary>
public interface INotificationRequestRepository
{
    /// <summary>
    /// Obtém uma requisição de notificação pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador da notificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Requisição de notificação ou null se não encontrada.</returns>
    Task<NotificationRequest?> GetByIdAsync(
        NotificationId id,
        CancellationToken ct = default);

    /// <summary>
    /// Lista requisições de notificação de um tenant com paginação.
    /// </summary>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="page">Número da página (1-based).</param>
    /// <param name="pageSize">Quantidade de itens por página.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Tupla com itens da página e total de registros.</returns>
    Task<(IReadOnlyList<NotificationRequest> Items, int TotalCount)> ListByTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém requisições de notificação expiradas que ainda não foram marcadas.
    /// Usado pelo job de expiração para atualizar status de notificações vencidas.
    /// </summary>
    /// <param name="referenceTime">Data/hora UTC de referência para comparação de expiração.</param>
    /// <param name="batchSize">Tamanho do lote para processamento.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de requisições expiradas.</returns>
    Task<IReadOnlyList<NotificationRequest>> GetExpiredAsync(
        DateTimeOffset referenceTime,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>Persiste uma nova requisição de notificação.</summary>
    /// <param name="request">Requisição a ser persistida.</param>
    void Add(NotificationRequest request);
}
