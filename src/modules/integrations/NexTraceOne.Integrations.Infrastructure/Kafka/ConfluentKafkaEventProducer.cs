using Confluent.Kafka;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Kafka;

/// <summary>
/// Implementação de IKafkaEventProducer usando o Confluent.Kafka client.
/// Activa quando Kafka:Enabled = true e Kafka:BootstrapServers estão configurados.
/// Usa Acks.All para garantia de entrega máxima.
/// </summary>
internal sealed class ConfluentKafkaEventProducer(
    IConfiguration configuration,
    ILogger<ConfluentKafkaEventProducer> logger) : IKafkaEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All
        }).Build();

    /// <inheritdoc />
    public bool IsConfigured => true;

    /// <inheritdoc />
    public async Task ProduceAsync(
        string topic,
        string key,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = payloadJson,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) }
            }
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            logger.LogDebug(
                "ConfluentKafkaEventProducer: Produced event '{EventType}' to topic '{Topic}' partition {Partition} offset {Offset}",
                eventType, topic, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogError(
                ex,
                "ConfluentKafkaEventProducer: Failed to produce event '{EventType}' to topic '{Topic}'",
                eventType, topic);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ProduceBatchAsync(
        string topic,
        IEnumerable<KafkaMessage> messages,
        CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            await ProduceAsync(topic, msg.Key, msg.EventType, msg.PayloadJson, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
