using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Registo de entrega de notificação por canal externo.
/// Cada tentativa de entrega (email, Teams) gera uma instância de NotificationDelivery,
/// permitindo rastreabilidade completa e retry independente por canal.
/// </summary>
public sealed class NotificationDelivery : Entity<NotificationDeliveryId>
{
    private NotificationDelivery() { } // EF Core

    private NotificationDelivery(
        NotificationDeliveryId id,
        NotificationId notificationId,
        DeliveryChannel channel,
        string? recipientAddress)
    {
        Id = id;
        NotificationId = notificationId;
        Channel = channel;
        RecipientAddress = recipientAddress;
        Status = DeliveryStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        RetryCount = 0;
    }

    /// <summary>Id da notificação associada.</summary>
    public NotificationId NotificationId { get; private set; } = default!;

    /// <summary>Canal de entrega utilizado.</summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>Endereço do destinatário no canal (email, Teams channel id, etc.).</summary>
    public string? RecipientAddress { get; private set; }

    /// <summary>Estado atual da entrega.</summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>Data/hora UTC da criação do registo de entrega.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última tentativa de entrega (sucesso ou falha).</summary>
    public DateTimeOffset? LastAttemptAt { get; private set; }

    /// <summary>Data/hora UTC da entrega com sucesso.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>Data/hora UTC da última falha.</summary>
    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>
    /// Data/hora UTC agendada para a próxima tentativa de retry.
    /// Apenas relevante quando Status = RetryScheduled.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; private set; }

    /// <summary>Mensagem de erro da última tentativa falhada.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Número de tentativas de entrega realizadas.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo registo de entrega para um canal específico.
    /// </summary>
    public static NotificationDelivery Create(
        NotificationId notificationId,
        DeliveryChannel channel,
        string? recipientAddress = null)
    {
        return new NotificationDelivery(
            new NotificationDeliveryId(Guid.NewGuid()),
            notificationId,
            channel,
            recipientAddress);
    }

    /// <summary>Marca a entrega como concluída com sucesso.</summary>
    public void MarkDelivered()
    {
        Status = DeliveryStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        LastAttemptAt = DeliveredAt;
        NextRetryAt = null;
    }

    /// <summary>Marca a entrega como falhada definitivamente (sem mais retries).</summary>
    public void MarkFailed(string? errorMessage)
    {
        Status = DeliveryStatus.Failed;
        FailedAt = DateTimeOffset.UtcNow;
        LastAttemptAt = FailedAt;
        ErrorMessage = errorMessage;
        NextRetryAt = null;
    }

    /// <summary>Marca a entrega como ignorada (opt-out, canal desabilitado, etc.).</summary>
    public void MarkSkipped()
    {
        Status = DeliveryStatus.Skipped;
        LastAttemptAt = DateTimeOffset.UtcNow;
        NextRetryAt = null;
    }

    /// <summary>
    /// Agenda um retry: regista o erro da tentativa atual e define quando a próxima
    /// tentativa deve ocorrer. O NotificationDeliveryRetryJob recolherá este registo
    /// quando NextRetryAt for atingido.
    /// Nota: FailedAt só é definido em MarkFailed(); ScheduleRetry indica falha transitória.
    /// </summary>
    public void ScheduleRetry(DateTimeOffset nextRetryAt, string? errorMessage = null)
    {
        Status = DeliveryStatus.RetryScheduled;
        NextRetryAt = nextRetryAt;
        LastAttemptAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }

    /// <summary>Incrementa o contador de retry e limpa o agendamento, tornando pendente.</summary>
    public void IncrementRetry()
    {
        RetryCount++;
        Status = DeliveryStatus.Pending;
        NextRetryAt = null;
        LastAttemptAt = DateTimeOffset.UtcNow;
    }
}
