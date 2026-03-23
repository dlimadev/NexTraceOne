namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Estados do ciclo de vida de uma notificação na central interna.
/// Transições permitidas:
///   Unread → Read → Acknowledged → Archived
///   Unread → Read → Archived
///   Unread → Dismissed
///   Read → Dismissed
/// </summary>
public enum NotificationStatus
{
    /// <summary>Notificação criada mas ainda não visualizada pelo destinatário.</summary>
    Unread = 0,

    /// <summary>Notificação visualizada/aberta pelo destinatário.</summary>
    Read = 1,

    /// <summary>Notificação explicitamente confirmada pelo destinatário (para notificações que exigem ação).</summary>
    Acknowledged = 2,

    /// <summary>Notificação arquivada pelo destinatário ou automaticamente.</summary>
    Archived = 3,

    /// <summary>Notificação descartada pelo destinatário sem ação.</summary>
    Dismissed = 4
}
