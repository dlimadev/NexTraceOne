namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Tipo de fonte de eventos para o consumer worker.
/// Identifica o sistema de mensageria de origem de um evento consumido.
/// </summary>
public enum EventSourceType
{
    /// <summary>Apache Kafka.</summary>
    Kafka = 0,

    /// <summary>Azure Service Bus.</summary>
    ServiceBus = 1,

    /// <summary>Amazon Simple Queue Service.</summary>
    Sqs = 2,

    /// <summary>RabbitMQ (AMQP).</summary>
    RabbitMq = 3,

    /// <summary>Tipo desconhecido ou não mapeado.</summary>
    Unknown = 99
}
