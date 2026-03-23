using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração de persistência para preferências de notificação.
/// Isola a camada de aplicação dos detalhes de infraestrutura.
/// </summary>
public interface INotificationPreferenceStore
{
    /// <summary>Obtém todas as preferências de um utilizador.</summary>
    Task<IReadOnlyList<NotificationPreference>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém uma preferência específica por utilizador, categoria e canal.</summary>
    Task<NotificationPreference?> GetAsync(
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova preferência.</summary>
    Task AddAsync(
        NotificationPreference preference,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações pendentes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
