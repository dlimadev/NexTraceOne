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
    ResolvedNotificationTemplate Resolve(string eventType, IReadOnlyDictionary<string, string> parameters);
}

/// <summary>
/// Conteúdo resolvido de um template de notificação em memória.
/// Distingue-se da entidade persistida NotificationTemplate (domínio).
/// </summary>
public sealed record ResolvedNotificationTemplate(
    string Title,
    string Message,
    NotificationCategory Category,
    NotificationSeverity Severity,
    bool RequiresAction);
