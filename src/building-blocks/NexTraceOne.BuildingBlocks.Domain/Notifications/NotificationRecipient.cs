using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Domain.Notifications;

/// <summary>
/// Representa o destinatário de uma notificação e o estado de entrega para esse usuário.
/// Cada destinatário pode ter canal preferido e timestamps independentes de leitura/confirmação.
/// Permite rastrear o ciclo de vida completo da notificação por usuário.
/// </summary>
public sealed class NotificationRecipient : Entity<NotificationRecipientId>
{
    /// <summary>Referência à notificação à qual este destinatário pertence.</summary>
    public NotificationId NotificationRequestId { get; private set; } = default!;

    /// <summary>Identificador do usuário destinatário.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Identificador do tenant do destinatário.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Canal preferido pelo usuário para esta notificação.
    /// Quando nulo, o orquestrador usa a preferência cadastrada ou o canal padrão do template.
    /// </summary>
    public NotificationChannel? PreferredChannel { get; private set; }

    /// <summary>Status atual da entrega para este destinatário.</summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>Data/hora UTC em que a notificação foi enviada ao destinatário.</summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>Data/hora UTC em que a entrega foi confirmada pelo provedor.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>Data/hora UTC em que o destinatário leu a notificação.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>Data/hora UTC em que o destinatário confirmou ciência explícita.</summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    /// <summary>Motivo da falha de entrega, quando aplicável.</summary>
    public string? FailureReason { get; private set; }

    private NotificationRecipient() { }

    /// <summary>
    /// Factory method para criação de um destinatário de notificação.
    /// Inicializa com status Pending e sem timestamps de entrega.
    /// </summary>
    /// <param name="notificationRequestId">Id da notificação associada.</param>
    /// <param name="userId">Id do usuário destinatário.</param>
    /// <param name="tenantId">Id do tenant do destinatário.</param>
    /// <param name="preferredChannel">Canal preferido (opcional).</param>
    /// <returns>Instância de <see cref="NotificationRecipient"/> com status Pending.</returns>
    public static NotificationRecipient Create(
        NotificationId notificationRequestId,
        Guid userId,
        Guid tenantId,
        NotificationChannel? preferredChannel = null)
    {
        return new NotificationRecipient
        {
            Id = new NotificationRecipientId(Guid.NewGuid()),
            NotificationRequestId = notificationRequestId,
            UserId = userId,
            TenantId = tenantId,
            PreferredChannel = preferredChannel,
            Status = NotificationStatus.Pending
        };
    }

    /// <summary>Marca a notificação como enviada para este destinatário.</summary>
    /// <param name="sentAt">Data/hora UTC do envio.</param>
    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
    }

    /// <summary>Marca a notificação como entregue para este destinatário.</summary>
    /// <param name="deliveredAt">Data/hora UTC da confirmação de entrega.</param>
    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = deliveredAt;
    }

    /// <summary>Marca a notificação como lida pelo destinatário.</summary>
    /// <param name="readAt">Data/hora UTC da leitura.</param>
    public void MarkRead(DateTimeOffset readAt)
    {
        Status = NotificationStatus.Read;
        ReadAt = readAt;
    }

    /// <summary>Marca a notificação como confirmada pelo destinatário.</summary>
    /// <param name="acknowledgedAt">Data/hora UTC da confirmação explícita.</param>
    public void MarkAcknowledged(DateTimeOffset acknowledgedAt)
    {
        Status = NotificationStatus.Acknowledged;
        AcknowledgedAt = acknowledgedAt;
    }

    /// <summary>Marca a notificação como falhada para este destinatário.</summary>
    /// <param name="reason">Motivo da falha de entrega.</param>
    public void MarkFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>Marca a notificação como expirada para este destinatário.</summary>
    public void MarkExpired()
    {
        Status = NotificationStatus.Expired;
    }
}
