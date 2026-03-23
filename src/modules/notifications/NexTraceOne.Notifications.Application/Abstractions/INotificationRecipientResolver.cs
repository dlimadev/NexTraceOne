using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Resolve destinatários a partir de um pedido de notificação.
/// Combina utilizadores explícitos com resolução futura de roles e equipas.
/// </summary>
public interface INotificationRecipientResolver
{
    /// <summary>Resolve a lista final de IDs de utilizadores destinatários.</summary>
    Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);
}
