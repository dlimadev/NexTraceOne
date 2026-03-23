using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Resolve o título e a mensagem internos de uma notificação a partir do tipo de evento
/// e de um conjunto de parâmetros contextuais.
/// </summary>
public interface INotificationTemplateResolver
{
    /// <summary>
    /// Resolve o template para o tipo de evento fornecido.
    /// Retorna título, mensagem e se a notificação requer ação.
    /// </summary>
    NotificationTemplate Resolve(string eventType, IReadOnlyDictionary<string, string> parameters);
}

/// <summary>
/// Template materializado de uma notificação.
/// </summary>
public sealed record NotificationTemplate(
    string Title,
    string Message,
    NotificationCategory Category,
    NotificationSeverity Severity,
    bool RequiresAction);
