namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

public enum DlqMessageStatus
{
    Pending,
    Reprocessing,
    Resolved,
    Discarded
}

/// <summary>
/// Registo persistente de uma mensagem Outbox que esgotou todas as tentativas de entrega.
/// Auditável e reprocessável via endpoint Platform Admin.
/// </summary>
public sealed class DeadLetterMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TenantId { get; init; }
    public string MessageType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string? LastException { get; set; }
    public int AttemptCount { get; init; }
    public DateTimeOffset ExhaustedAt { get; init; }
    public DateTimeOffset? ReprocessedAt { get; set; }
    public DlqMessageStatus Status { get; set; } = DlqMessageStatus.Pending;

    /// <summary>
    /// Cria um DeadLetterMessage a partir de uma mensagem Outbox exausta e a última exceção.
    /// </summary>
    public static DeadLetterMessage From(OutboxMessage message, Exception exception, DateTimeOffset exhaustedAt) =>
        new()
        {
            TenantId = message.TenantId,
            MessageType = message.EventType,
            Payload = message.Payload,
            FailureReason = message.LastError ?? $"Exhausted {message.RetryCount} delivery attempts.",
            LastException = exception.Message,
            AttemptCount = message.RetryCount,
            ExhaustedAt = exhaustedAt
        };

    public void MarkReprocessing(DateTimeOffset now)
    {
        Status = DlqMessageStatus.Reprocessing;
        ReprocessedAt = now;
    }

    public void MarkResolved() => Status = DlqMessageStatus.Resolved;

    public void MarkDiscarded(string reason)
    {
        Status = DlqMessageStatus.Discarded;
        FailureReason = reason;
    }
}
