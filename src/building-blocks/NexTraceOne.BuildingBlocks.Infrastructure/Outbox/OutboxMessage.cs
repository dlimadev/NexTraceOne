namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Mensagem Outbox para publicação garantida de Integration Events.
/// Salva na mesma transação do aggregate — garantia de consistência.
/// O OutboxProcessorJob entrega de forma assíncrona após commit.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public Guid TenantId { get; init; }
}
