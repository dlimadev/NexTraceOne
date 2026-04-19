namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para publicação de eventos de domínio num cluster Apache Kafka.
/// A implementação padrão é <c>NullKafkaEventProducer</c> que descarta silenciosamente
/// os eventos até que um cluster Kafka real seja configurado via
/// <c>Kafka:BootstrapServers</c> e <c>Kafka:Enabled = true</c> em appsettings.
/// </summary>
public interface IKafkaEventProducer
{
    /// <summary>
    /// Indica se o producer está configurado e ligado a um cluster Kafka real.
    /// Usado para logging informativo e para mostrar estado de integração na UI.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Publica um evento serializado num tópico Kafka específico.
    /// Quando <see cref="IsConfigured"/> é false, o evento é descartado silenciosamente sem erro.
    /// </summary>
    /// <param name="topic">Nome do tópico Kafka.</param>
    /// <param name="key">Chave de partição (tipicamente o ID do agregado).</param>
    /// <param name="eventType">Nome do tipo de evento (usado como header <c>event-type</c>).</param>
    /// <param name="payloadJson">Payload serializado em JSON.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task ProduceAsync(
        string topic,
        string key,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica um batch de eventos no mesmo tópico, em ordem.
    /// Cada evento é publicado individualmente; a implementação Null descarta todos silenciosamente.
    /// </summary>
    Task ProduceBatchAsync(
        string topic,
        IEnumerable<KafkaMessage> messages,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Representa uma mensagem a publicar no Kafka.
/// </summary>
/// <param name="Key">Chave de partição.</param>
/// <param name="EventType">Tipo de evento (header).</param>
/// <param name="PayloadJson">Payload JSON da mensagem.</param>
public sealed record KafkaMessage(string Key, string EventType, string PayloadJson);
