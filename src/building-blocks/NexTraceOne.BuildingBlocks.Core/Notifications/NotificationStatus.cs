namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Status do ciclo de vida de entrega de uma notificação.
/// Rastreia desde a criação até a confirmação ou falha,
/// permitindo retry automático e auditoria completa.
/// </summary>
public enum NotificationStatus
{
    /// <summary>Notificação criada, aguardando processamento pelo orquestrador.</summary>
    Pending = 0,

    /// <summary>Notificação enfileirada para envio pelo channel adapter.</summary>
    Queued = 1,

    /// <summary>Notificação enviada com sucesso pelo canal (sem confirmação de entrega).</summary>
    Sent = 2,

    /// <summary>Entrega confirmada pelo provedor externo (webhook de delivery).</summary>
    Delivered = 3,

    /// <summary>Notificação lida pelo destinatário (rastreável em InApp e email).</summary>
    Read = 4,

    /// <summary>Destinatário confirmou ciência explícita (para notificações que exigem acknowledgement).</summary>
    Acknowledged = 5,

    /// <summary>Falha no envio após todas as tentativas de retry.</summary>
    Failed = 6,

    /// <summary>Notificação expirou antes de ser lida ou confirmada pelo destinatário.</summary>
    Expired = 7
}
