using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração para despacho de notificação por canal externo.
/// Cada canal (email, Teams) implementa esta interface.
/// O orquestrador invoca os dispatchers elegíveis para cada notificação.
/// </summary>
public interface INotificationChannelDispatcher
{
    /// <summary>Nome identificador do canal (e.g., "Email", "MicrosoftTeams").</summary>
    string ChannelName { get; }

    /// <summary>Canal de entrega correspondente.</summary>
    DeliveryChannel Channel { get; }

    /// <summary>
    /// Envia uma notificação por este canal.
    /// Retorna true se a entrega foi aceite pelo canal, false caso contrário.
    /// </summary>
    Task<bool> DispatchAsync(
        Notification notification,
        string? recipientAddress,
        CancellationToken cancellationToken = default);
}
