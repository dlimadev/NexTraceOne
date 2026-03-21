using System.Text.Json;

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
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public Guid TenantId { get; init; }

    /// <summary>
    /// Chave de idempotência para garantir processamento único.
    /// Formato: {EventType}:{AggregateId}:{Timestamp}.
    /// Handlers devem verificar esta chave antes de executar side-effects.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>Cria uma mensagem de outbox a partir de um evento de domínio serializado.</summary>
    public static OutboxMessage Create(object domainEvent, Guid tenantId, DateTimeOffset createdAt)
        => new()
        {
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            TenantId = tenantId,
            CreatedAt = createdAt,
            IdempotencyKey = $"{domainEvent.GetType().AssemblyQualifiedName}:{Guid.NewGuid():N}:{createdAt:O}"
        };
}
