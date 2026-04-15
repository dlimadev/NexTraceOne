namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de padrão de deteção utilizado pelo guardrail.
/// </summary>
public enum GuardrailPatternType
{
    /// <summary>Expressão regular (regex).</summary>
    Regex,

    /// <summary>Correspondência por palavra-chave.</summary>
    Keyword,

    /// <summary>Classificador de ML.</summary>
    Classifier,

    /// <summary>Similaridade semântica.</summary>
    Semantic
}
