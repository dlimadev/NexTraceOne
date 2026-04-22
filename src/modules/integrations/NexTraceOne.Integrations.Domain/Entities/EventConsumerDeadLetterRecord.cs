using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para EventConsumerDeadLetterRecord.
/// </summary>
public sealed record EventConsumerDeadLetterRecordId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo de dead letter para eventos que falharam o processamento no consumer worker.
/// Armazena o payload original, metadados da fonte e historial de tentativas para
/// permitir re-processamento manual ou análise de falha.
/// </summary>
public sealed class EventConsumerDeadLetterRecord : Entity<EventConsumerDeadLetterRecordId>
{
    /// <summary>Identificador do tenant proprietário do registo.</summary>
    public Guid TenantId { get; private init; }

    /// <summary>Tipo da fonte de eventos (ex: "Kafka", "ServiceBus", "SQS", "RabbitMQ").</summary>
    public string SourceType { get; private init; } = string.Empty;

    /// <summary>Nome do tópico ou fila de onde o evento foi consumido.</summary>
    public string Topic { get; private init; } = string.Empty;

    /// <summary>Chave de partição opcional (relevante para Kafka e Service Bus).</summary>
    public string? PartitionKey { get; private init; }

    /// <summary>Payload original em formato JSON.</summary>
    public string Payload { get; private init; } = string.Empty;

    /// <summary>Número de tentativas de processamento realizadas.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Mensagem do último erro ocorrido no processamento.</summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da primeira tentativa de processamento.</summary>
    public DateTimeOffset FirstAttemptAt { get; private init; }

    /// <summary>Data/hora UTC da última tentativa de processamento.</summary>
    public DateTimeOffset LastAttemptAt { get; private set; }

    /// <summary>Indica se o registo foi resolvido manualmente ou re-processado com sucesso.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Data/hora UTC da resolução, quando aplicável.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    private EventConsumerDeadLetterRecord() { }

    /// <summary>
    /// Cria um novo registo de dead letter para um evento falhado.
    /// </summary>
    public static EventConsumerDeadLetterRecord Record(
        Guid tenantId,
        string sourceType,
        string topic,
        string? partitionKey,
        string payload,
        string lastError)
    {
        Guard.Against.Default(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(sourceType, nameof(sourceType));
        Guard.Against.NullOrWhiteSpace(topic, nameof(topic));
        Guard.Against.NullOrWhiteSpace(payload, nameof(payload));
        Guard.Against.NullOrWhiteSpace(lastError, nameof(lastError));

        var now = DateTimeOffset.UtcNow;
        return new EventConsumerDeadLetterRecord
        {
            Id = new EventConsumerDeadLetterRecordId(Guid.NewGuid()),
            TenantId = tenantId,
            SourceType = sourceType.Trim(),
            Topic = topic.Trim(),
            PartitionKey = partitionKey?.Trim(),
            Payload = payload,
            AttemptCount = 1,
            LastError = lastError.Trim(),
            FirstAttemptAt = now,
            LastAttemptAt = now,
            IsResolved = false
        };
    }

    /// <summary>Regista uma nova tentativa falhada.</summary>
    public void RecordFailedAttempt(string error)
    {
        Guard.Against.NullOrWhiteSpace(error, nameof(error));
        AttemptCount++;
        LastError = error.Trim();
        LastAttemptAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marca o registo como resolvido.</summary>
    public void MarkResolved()
    {
        IsResolved = true;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
