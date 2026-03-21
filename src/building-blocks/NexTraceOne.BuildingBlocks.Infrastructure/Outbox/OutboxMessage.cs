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
    /// Formato: {EventType}:{ContentHash}:{Timestamp}.
    /// Deterministic — the same logical event always produces the same key.
    /// Handlers devem verificar esta chave antes de executar side-effects.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>Cria uma mensagem de outbox a partir de um evento de domínio serializado.</summary>
    public static OutboxMessage Create(object domainEvent, Guid tenantId, DateTimeOffset createdAt)
    {
        var eventTypeName = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name;
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        // Deterministic idempotency key: hash of payload ensures same event = same key.
        var contentHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(payload)))[..16];

        return new()
        {
            EventType = eventTypeName,
            Payload = payload,
            TenantId = tenantId,
            CreatedAt = createdAt,
            IdempotencyKey = $"{eventTypeName}:{contentHash}:{createdAt:O}"
        };
    }
}
