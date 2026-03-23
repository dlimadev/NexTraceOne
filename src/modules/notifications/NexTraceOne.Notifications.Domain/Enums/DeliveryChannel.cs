namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Canais de entrega de notificação suportados pela plataforma.
/// Cada canal é uma extensão do núcleo (central interna).
/// </summary>
public enum DeliveryChannel
{
    /// <summary>Central interna de notificações (inbox do produto).</summary>
    InApp = 0,

    /// <summary>Email via SMTP.</summary>
    Email = 1,

    /// <summary>Microsoft Teams via webhook ou Adaptive Cards.</summary>
    MicrosoftTeams = 2
}
