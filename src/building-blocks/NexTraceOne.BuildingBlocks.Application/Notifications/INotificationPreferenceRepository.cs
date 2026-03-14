using NexTraceOne.BuildingBlocks.Domain.Notifications;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Repositório para persistência de preferências de notificação dos usuários.
/// Cada preferência define canal e severidade mínima por categoria para um usuário/tenant.
/// </summary>
public interface INotificationPreferenceRepository
{
    /// <summary>
    /// Obtém todas as preferências de notificação de um usuário em um tenant.
    /// </summary>
    /// <param name="userId">Id do usuário.</param>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de preferências do usuário no tenant.</returns>
    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém as preferências de um usuário para uma categoria específica.
    /// </summary>
    /// <param name="userId">Id do usuário.</param>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="category">Categoria de notificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de preferências do usuário para a categoria.</returns>
    Task<IReadOnlyList<NotificationPreference>> GetByUserAndCategoryAsync(
        Guid userId,
        Guid tenantId,
        NotificationCategory category,
        CancellationToken ct = default);

    /// <summary>Persiste uma nova preferência de notificação.</summary>
    /// <param name="preference">Preferência a ser persistida.</param>
    void Add(NotificationPreference preference);

    /// <summary>Remove uma preferência de notificação existente.</summary>
    /// <param name="preference">Preferência a ser removida.</param>
    void Remove(NotificationPreference preference);
}
