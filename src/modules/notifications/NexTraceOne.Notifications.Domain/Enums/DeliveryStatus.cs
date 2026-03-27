namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Estados de entrega de notificação por canal externo.
/// Rastreabilidade de cada tentativa de entrega.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>Entrega pendente, aguardando processamento.</summary>
    Pending = 0,

    /// <summary>Entrega concluída com sucesso pelo canal.</summary>
    Delivered = 1,

    /// <summary>Entrega falhou após todas as tentativas.</summary>
    Failed = 2,

    /// <summary>Entrega ignorada (e.g., preferência opt-out, canal desabilitado, deduplicação).</summary>
    Skipped = 3,

    /// <summary>
    /// Entrega falhou nesta tentativa mas está agendada para retry.
    /// NextRetryAt indica quando deve ser reprocessada pelo NotificationDeliveryRetryJob.
    /// </summary>
    RetryScheduled = 4
}
