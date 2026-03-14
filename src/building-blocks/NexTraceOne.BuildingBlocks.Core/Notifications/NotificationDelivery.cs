using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Rastreia cada tentativa de entrega de uma notificação por canal específico.
/// Permite monitorar retries, falhas e status de entrega por provedor externo.
/// Cada delivery é independente: uma mesma notificação pode ter múltiplas
/// tentativas de entrega por canais diferentes (fallback) ou retries no mesmo canal.
/// </summary>
public sealed class NotificationDelivery : Entity<NotificationDeliveryId>
{
    /// <summary>Referência à notificação sendo entregue.</summary>
    public NotificationId NotificationRequestId { get; private set; } = default!;

    /// <summary>Identificador do usuário destinatário desta entrega.</summary>
    public Guid RecipientUserId { get; private set; }

    /// <summary>Canal utilizado nesta tentativa de entrega.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Status atual desta tentativa de entrega.</summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>Número de tentativas de envio realizadas neste canal.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Data/hora UTC da última tentativa de envio.</summary>
    public DateTimeOffset? LastAttemptAt { get; private set; }

    /// <summary>Data/hora UTC da confirmação de entrega pelo provedor.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>Mensagem de erro da última tentativa falhada.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Identificador externo da mensagem no provedor (ex: message-id do email, webhook id).
    /// Usado para correlação com logs do provedor externo.
    /// </summary>
    public string? ExternalMessageId { get; private set; }

    private NotificationDelivery() { }

    /// <summary>
    /// Factory method para criação de um registro de entrega.
    /// Inicializa com status Pending e sem tentativas.
    /// </summary>
    /// <param name="notificationRequestId">Id da notificação.</param>
    /// <param name="recipientUserId">Id do usuário destinatário.</param>
    /// <param name="channel">Canal de entrega.</param>
    /// <returns>Instância de <see cref="NotificationDelivery"/> com status Pending.</returns>
    public static NotificationDelivery Create(
        NotificationId notificationRequestId,
        Guid recipientUserId,
        NotificationChannel channel)
    {
        return new NotificationDelivery
        {
            Id = new NotificationDeliveryId(Guid.NewGuid()),
            NotificationRequestId = notificationRequestId,
            RecipientUserId = recipientUserId,
            Channel = channel,
            Status = NotificationStatus.Pending,
            AttemptCount = 0
        };
    }

    /// <summary>
    /// Registra uma nova tentativa de envio, incrementando o contador.
    /// Só permite transição para Queued a partir de Pending ou Failed (retry).
    /// </summary>
    /// <param name="attemptAt">Data/hora UTC da tentativa.</param>
    public void RecordAttempt(DateTimeOffset attemptAt)
    {
        if (Status is not (NotificationStatus.Pending or NotificationStatus.Failed))
            return;

        AttemptCount++;
        LastAttemptAt = attemptAt;
        Status = NotificationStatus.Queued;
    }

    /// <summary>Marca a entrega como enviada com sucesso pelo canal.</summary>
    /// <param name="externalMessageId">Id externo da mensagem no provedor (opcional).</param>
    public void MarkSent(string? externalMessageId = null)
    {
        Status = NotificationStatus.Sent;
        ExternalMessageId = externalMessageId;
    }

    /// <summary>Marca a entrega como confirmada pelo provedor externo.</summary>
    /// <param name="deliveredAt">Data/hora UTC da confirmação.</param>
    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = deliveredAt;
    }

    /// <summary>Marca a entrega como falhada com motivo.</summary>
    /// <param name="errorMessage">Mensagem técnica do erro.</param>
    public void MarkFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
