using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de gestão de preferências de notificação dos utilizadores.
/// Permite consultar e atualizar preferências por categoria e canal.
/// Quando não existe preferência explícita, aplica-se o fallback padrão da plataforma.
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Obtém as preferências de um utilizador para todas as categorias e canais.
    /// </summary>
    Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se um canal está habilitado para uma categoria e utilizador.
    /// Retorna o valor da preferência explícita ou o fallback padrão.
    /// </summary>
    Task<bool> IsChannelEnabledAsync(
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza a preferência de um utilizador para uma categoria e canal.
    /// </summary>
    Task UpdatePreferenceAsync(
        Guid tenantId,
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        bool enabled,
        CancellationToken cancellationToken = default);
}
