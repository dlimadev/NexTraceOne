using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Preferences;

/// <summary>
/// Implementação do serviço de preferências de notificação.
/// Gere preferências explícitas por utilizador e aplica defaults da plataforma
/// quando não existe preferência explícita para uma combinação categoria/canal.
///
/// Defaults da plataforma:
///   - InApp: sempre habilitado (para todas as categorias e severidades)
///   - Email: habilitado para ActionRequired, Warning e Critical
///   - Teams: habilitado para Warning e Critical
///   - Info severity → apenas InApp
/// </summary>
internal sealed class NotificationPreferenceService(
    INotificationPreferenceStore store,
    ILogger<NotificationPreferenceService> logger) : INotificationPreferenceService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await store.GetByUserIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsChannelEnabledAsync(
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        CancellationToken cancellationToken)
    {
        var preference = await store.GetAsync(userId, category, channel, cancellationToken);

        if (preference is not null)
        {
            logger.LogDebug(
                "Explicit preference found for user {UserId}, category {Category}, channel {Channel}: Enabled={Enabled}",
                userId, category, channel, preference.Enabled);
            return preference.Enabled;
        }

        var systemDefault = GetSystemDefault(channel);

        logger.LogDebug(
            "No explicit preference for user {UserId}, category {Category}, channel {Channel}. Using system default: {Default}",
            userId, category, channel, systemDefault);

        return systemDefault;
    }

    /// <inheritdoc/>
    public async Task UpdatePreferenceAsync(
        Guid tenantId,
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var existing = await store.GetAsync(userId, category, channel, cancellationToken);

        if (existing is not null)
        {
            existing.Update(enabled);
            logger.LogInformation(
                "Updated notification preference: user={UserId}, category={Category}, channel={Channel}, enabled={Enabled}",
                userId, category, channel, enabled);
        }
        else
        {
            var preference = NotificationPreference.Create(tenantId, userId, category, channel, enabled);
            await store.AddAsync(preference, cancellationToken);
            logger.LogInformation(
                "Created notification preference: user={UserId}, category={Category}, channel={Channel}, enabled={Enabled}",
                userId, category, channel, enabled);
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Defaults da plataforma por canal.
    /// InApp é sempre habilitado. Email e Teams habilitados por padrão para permitir
    /// que o routing engine decida com base na severidade.
    /// </summary>
    private static bool GetSystemDefault(DeliveryChannel channel) => channel switch
    {
        DeliveryChannel.InApp => true,
        DeliveryChannel.Email => true,
        DeliveryChannel.MicrosoftTeams => true,
        _ => false
    };
}
