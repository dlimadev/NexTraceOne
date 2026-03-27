namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Tipo de entidade de conhecimento que origina uma relação.
/// Mantém o modelo de KnowledgeRelation explícito e rastreável.
/// </summary>
public enum KnowledgeSourceEntityType
{
    /// <summary>Relação originada por um KnowledgeDocument.</summary>
    KnowledgeDocument,

    /// <summary>Relação originada por uma OperationalNote.</summary>
    OperationalNote
}
