using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração para persistência e consulta de notificações na central interna.
/// Implementação será fornecida na camada Infrastructure (EF Core / PostgreSQL).
/// </summary>
public interface INotificationStore
{
    /// <summary>Persiste uma nova notificação.</summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>Obtém uma notificação por Id.</summary>
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

    /// <summary>Lista notificações de um utilizador com filtros opcionais e paginação.</summary>
    Task<IReadOnlyList<Notification>> ListAsync(
        Guid recipientUserId,
        NotificationStatus? status = null,
        NotificationCategory? category = null,
        NotificationSeverity? minSeverity = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Conta notificações não lidas de um utilizador.</summary>
    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    /// <summary>Marca todas as notificações não lidas de um utilizador como lidas.</summary>
    Task MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    /// <summary>Lista notificações não lidas/não tratadas elegíveis para escalação.</summary>
    /// <param name="tenantId">Tenant ao qual as notificações pertencem.</param>
    /// <param name="olderThan">Retornar apenas notificações criadas antes desta data.</param>
    /// <param name="severities">Severidades alvo para escalação (Critical, ActionRequired).</param>
    /// <param name="skip">Offset de paginação.</param>
    /// <param name="take">Número máximo de resultados.</param>
    Task<IReadOnlyList<Notification>> ListForEscalationAsync(
        Guid tenantId,
        DateTimeOffset olderThan,
        IReadOnlyList<NotificationSeverity> severities,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Lista notificações não lidas de um tenant num intervalo de tempo, elegíveis para digest.</summary>
    /// <param name="tenantId">Tenant alvo.</param>
    /// <param name="since">Retornar apenas notificações criadas desde esta data.</param>
    /// <param name="skip">Offset de paginação.</param>
    /// <param name="take">Número máximo de resultados.</param>
    Task<IReadOnlyList<Notification>> ListForDigestAsync(
        Guid tenantId,
        DateTimeOffset since,
        int skip = 0,
        int take = 500,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas na entidade.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
