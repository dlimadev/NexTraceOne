namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Canais disponíveis para envio de notificações na plataforma.
/// Cada canal possui adapter próprio e pode ser habilitado/desabilitado
/// por tenant ou por preferência individual do usuário.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Notificação interna na plataforma (inbox).</summary>
    InApp = 0,

    /// <summary>Envio por email via SMTP ou provedor configurado.</summary>
    Email = 1,

    /// <summary>Integração com Microsoft Teams via webhook ou API.</summary>
    MicrosoftTeams = 2,

    /// <summary>Integração com WhatsApp Business API.</summary>
    WhatsApp = 3,

    /// <summary>Envio de SMS como fallback para alertas críticos.</summary>
    Sms = 4,

    /// <summary>Notificação push para dispositivos móveis (futuro).</summary>
    PushNotification = 5
}
