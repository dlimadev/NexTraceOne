using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Estratégia de normalização para eventos consumidos de uma fonte específica.
/// Cada fonte de mensageria (Kafka, ServiceBus, SQS, RabbitMQ) possui a sua
/// própria estratégia que conhece o formato do envelope de mensagem.
/// </summary>
public interface IEventNormalizationStrategy
{
    /// <summary>Tipo de fonte que esta estratégia suporta.</summary>
    EventSourceType SourceType { get; }

    /// <summary>Verifica se esta estratégia pode processar o sourceType indicado.</summary>
    bool CanHandle(string sourceType);

    /// <summary>
    /// Normaliza um evento bruto para o formato canónico NexTraceOne.
    /// Retorna null quando o payload é inválido ou não pode ser normalizado — sinaliza dead letter.
    /// </summary>
    Task<NormalizedEvent?> NormalizeAsync(RawConsumerEvent raw, CancellationToken ct);
}

/// <summary>
/// Evento bruto recebido do sistema de mensageria antes da normalização.
/// </summary>
public sealed record RawConsumerEvent(
    string SourceType,
    string Topic,
    string? PartitionKey,
    string Payload,
    DateTimeOffset ReceivedAt);

/// <summary>
/// Evento normalizado para o formato canónico NexTraceOne após processamento pela estratégia.
/// </summary>
public sealed record NormalizedEvent(
    string EventType,
    string ServiceName,
    string? ReleaseId,
    string? EnvironmentName,
    DateTimeOffset OccurredAt,
    string RawPayload);
