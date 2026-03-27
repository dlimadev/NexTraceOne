using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração para persistência e consulta de templates de notificação.
/// Implementação fornecida na camada Infrastructure (EF Core / PostgreSQL).
/// </summary>
public interface INotificationTemplateStore
{
    /// <summary>Persiste um novo template.</summary>
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>Obtém um template por Id.</summary>
    Task<NotificationTemplate?> GetByIdAsync(NotificationTemplateId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista templates de um tenant, com filtros opcionais por tipo de evento e canal.
    /// </summary>
    Task<IReadOnlyList<NotificationTemplate>> ListAsync(
        Guid tenantId,
        string? eventType = null,
        DeliveryChannel? channel = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o template ativo mais específico para um tipo de evento e canal.
    /// Tenta encontrar um template específico do canal; se não existir, fallback para template genérico (Channel=null).
    /// </summary>
    Task<NotificationTemplate?> ResolveAsync(
        Guid tenantId,
        string eventType,
        DeliveryChannel channel,
        string locale = "en",
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas na entidade.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
