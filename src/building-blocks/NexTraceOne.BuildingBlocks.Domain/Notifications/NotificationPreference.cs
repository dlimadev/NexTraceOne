using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Domain.Notifications;

/// <summary>
/// Preferência de notificação do usuário por categoria e canal.
/// Permite que cada usuário configure quais canais deseja receber
/// para cada categoria de notificação e a severidade mínima aceitável.
/// O orquestrador consulta estas preferências antes de despachar para os adapters.
/// </summary>
public sealed class NotificationPreference : Entity<NotificationPreferenceId>
{
    /// <summary>Identificador do usuário dono desta preferência.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Identificador do tenant no qual a preferência se aplica.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Categoria de notificação à qual esta preferência se refere.</summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>Canal de notificação preferido pelo usuário para esta categoria.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Indica se o canal está habilitado para esta categoria.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Severidade mínima para que notificações desta categoria sejam enviadas por este canal.
    /// Notificações com severidade abaixo deste valor são silenciadas neste canal.
    /// </summary>
    public NotificationSeverity MinimumSeverity { get; private set; }

    private NotificationPreference() { }

    /// <summary>
    /// Factory method para criação de uma preferência de notificação.
    /// </summary>
    /// <param name="userId">Id do usuário.</param>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="category">Categoria de notificação.</param>
    /// <param name="channel">Canal preferido.</param>
    /// <param name="isEnabled">Se o canal está habilitado.</param>
    /// <param name="minimumSeverity">Severidade mínima aceitável.</param>
    /// <returns>Instância de <see cref="NotificationPreference"/>.</returns>
    public static NotificationPreference Create(
        Guid userId,
        Guid tenantId,
        NotificationCategory category,
        NotificationChannel channel,
        bool isEnabled = true,
        NotificationSeverity minimumSeverity = NotificationSeverity.Info)
    {
        return new NotificationPreference
        {
            Id = new NotificationPreferenceId(Guid.NewGuid()),
            UserId = userId,
            TenantId = tenantId,
            Category = category,
            Channel = channel,
            IsEnabled = isEnabled,
            MinimumSeverity = minimumSeverity
        };
    }

    /// <summary>Habilita este canal para a categoria.</summary>
    public void Enable() => IsEnabled = true;

    /// <summary>Desabilita este canal para a categoria.</summary>
    public void Disable() => IsEnabled = false;

    /// <summary>Atualiza a severidade mínima aceitável para este canal.</summary>
    /// <param name="severity">Nova severidade mínima.</param>
    public void UpdateMinimumSeverity(NotificationSeverity severity)
        => MinimumSeverity = severity;
}
