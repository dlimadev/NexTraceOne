namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que modela a vinculação de um contrato a um Schema Registry (Kafka).
/// Captura informações de subject, versão de schema, modo de compatibilidade e
/// relação entre topic/schema/producer/consumers para governança de contratos event-driven.
/// Preparado para integração com Confluent Schema Registry e equivalentes.
/// </summary>
/// <param name="Subject">Nome do subject no Schema Registry (ex: "orders-value", "users-key").</param>
/// <param name="SchemaVersion">Versão do schema no registry (inteiro sequencial).</param>
/// <param name="SchemaId">ID global do schema no registry.</param>
/// <param name="SchemaFormat">Formato do schema (AVRO, JSON, PROTOBUF).</param>
/// <param name="CompatibilityMode">Modo de compatibilidade configurado (BACKWARD, FORWARD, FULL, NONE, etc.).</param>
/// <param name="TopicName">Nome do tópico Kafka associado.</param>
/// <param name="Producers">Lista de producers conhecidos que publicam neste tópico.</param>
/// <param name="Consumers">Lista de consumers conhecidos que consomem deste tópico.</param>
/// <param name="RegistryUrl">URL do Schema Registry onde este binding está registrado.</param>
public sealed record SchemaRegistryBinding(
    string Subject,
    int SchemaVersion,
    int? SchemaId,
    string SchemaFormat,
    string CompatibilityMode,
    string TopicName,
    IReadOnlyList<string> Producers,
    IReadOnlyList<string> Consumers,
    string? RegistryUrl = null);
