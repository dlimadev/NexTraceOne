namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que modela a vinculação de um contrato a um Schema Registry (Kafka).
/// Captura informações de subject, versão de schema, modo de compatibilidade e
/// relação entre topic/schema/producer/consumers para governança de contratos event-driven.
/// Preparado para integração com Confluent Schema Registry e equivalentes.
/// </summary>
public sealed record SchemaRegistryBinding(
    /// <summary>Nome do subject no Schema Registry (ex: "orders-value", "users-key").</summary>
    string Subject,
    /// <summary>Versão do schema no registry (inteiro sequencial).</summary>
    int SchemaVersion,
    /// <summary>ID global do schema no registry.</summary>
    int? SchemaId,
    /// <summary>Formato do schema (AVRO, JSON, PROTOBUF).</summary>
    string SchemaFormat,
    /// <summary>Modo de compatibilidade configurado (BACKWARD, FORWARD, FULL, NONE, etc.).</summary>
    string CompatibilityMode,
    /// <summary>Nome do tópico Kafka associado.</summary>
    string TopicName,
    /// <summary>Lista de producers conhecidos que publicam neste tópico.</summary>
    IReadOnlyList<string> Producers,
    /// <summary>Lista de consumers conhecidos que consomem deste tópico.</summary>
    IReadOnlyList<string> Consumers,
    /// <summary>URL do Schema Registry onde este binding está registrado.</summary>
    string? RegistryUrl = null);
