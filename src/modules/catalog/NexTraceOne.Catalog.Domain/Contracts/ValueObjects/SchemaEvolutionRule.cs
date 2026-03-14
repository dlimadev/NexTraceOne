namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que define uma regra de evolução de schema para contratos event-driven.
/// Encapsula a política de compatibilidade aplicável a um subject/tópico no Schema Registry,
/// incluindo o modo de compatibilidade, ações permitidas e restrições.
/// Utilizado pelo scorecard e pela avaliação de compatibilidade para contratos Kafka/AsyncAPI.
/// </summary>
public sealed record SchemaEvolutionRule(
    /// <summary>Nome da regra (ex: "backward-compatibility", "field-addition-only").</summary>
    string RuleName,
    /// <summary>Descrição da regra em linguagem natural.</summary>
    string Description,
    /// <summary>Tipo de mudança que a regra governa (ex: "FieldAdded", "FieldRemoved", "TypeChanged").</summary>
    string ChangeType,
    /// <summary>Indica se a mudança é permitida pela regra.</summary>
    bool IsAllowed,
    /// <summary>Severidade quando a regra é violada (Error, Warning, Info).</summary>
    string Severity,
    /// <summary>Sugestão de correção quando a regra é violada.</summary>
    string? SuggestedFix = null);
