using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Política de notificações obrigatórias da plataforma.
/// Eventos de segurança, incidentes críticos e aprovações não podem ser desativados pelo utilizador.
/// </summary>
public interface IMandatoryNotificationPolicy
{
    /// <summary>Verifica se o evento/categoria/severidade é obrigatório (o utilizador não pode desativar).</summary>
    bool IsMandatory(string eventType, NotificationCategory category, NotificationSeverity severity);

    /// <summary>Retorna os canais obrigatórios para o evento/categoria/severidade.</summary>
    IReadOnlyList<DeliveryChannel> GetMandatoryChannels(
        string eventType,
        NotificationCategory category,
        NotificationSeverity severity);
}
