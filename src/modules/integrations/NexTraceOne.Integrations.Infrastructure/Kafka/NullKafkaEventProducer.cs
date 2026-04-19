using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Kafka;

/// <summary>
/// Implementação nula de <see cref="IKafkaEventProducer"/>.
/// Descarta silenciosamente todos os eventos enquanto nenhum cluster Kafka estiver configurado
/// via <c>Kafka:BootstrapServers</c> e <c>Kafka:Enabled = true</c> em appsettings.
/// Registado como default via DI. Substitua por <c>ConfluentKafkaEventProducer</c>
/// (requer <c>Confluent.Kafka</c> NuGet) quando o cluster estiver disponível.
/// </summary>
internal sealed class NullKafkaEventProducer(ILogger<NullKafkaEventProducer> logger) : IKafkaEventProducer
{
    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task ProduceAsync(
        string topic,
        string key,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "NullKafkaEventProducer: Kafka not configured — discarding event '{EventType}' on topic '{Topic}' (key={Key})",
            eventType, topic, key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ProduceBatchAsync(
        string topic,
        IEnumerable<KafkaMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var count = messages.TryGetNonEnumeratedCount(out var c) ? c : 0;
        logger.LogDebug(
            "NullKafkaEventProducer: Kafka not configured — discarding batch of {Count} message(s) on topic '{Topic}'",
            count, topic);
        return Task.CompletedTask;
    }
}
