using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Preferências de notificação de um utilizador.
/// Define como e quando o utilizador deseja receber notificações,
/// permitindo opt-in/opt-out por categoria e canal.
/// Quando não existe preferência explícita, aplica-se o fallback padrão da plataforma.
/// </summary>
public sealed class NotificationPreference : Entity<NotificationPreferenceId>
{
    private NotificationPreference() { } // EF Core

    private NotificationPreference(
        NotificationPreferenceId id,
        Guid tenantId,
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        bool enabled)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Category = category;
        Channel = channel;
        Enabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Id do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Id do utilizador que definiu esta preferência.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Categoria de notificação a que esta preferência se aplica.</summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>Canal de entrega a que esta preferência se aplica.</summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>Se true, o utilizador deseja receber notificações desta categoria neste canal.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Data/hora UTC da última atualização desta preferência.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cria uma nova preferência de notificação.
    /// </summary>
    public static NotificationPreference Create(
        Guid tenantId,
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        bool enabled)
    {
        return new NotificationPreference(
            new NotificationPreferenceId(Guid.NewGuid()),
            tenantId,
            userId,
            category,
            channel,
            enabled);
    }

    /// <summary>Atualiza o estado da preferência (ativar ou desativar).</summary>
    public void Update(bool enabled)
    {
        Enabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
