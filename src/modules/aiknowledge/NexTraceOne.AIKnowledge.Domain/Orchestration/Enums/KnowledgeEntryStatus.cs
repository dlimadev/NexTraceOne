namespace NexTraceOne.AiOrchestration.Domain.Enums;

/// <summary>
/// Estado de validação de uma entrada sugerida para a base de conhecimento
/// a partir da orquestração de IA.
/// Fluxo: Suggested → (Validated | Discarded).
/// </summary>
public enum KnowledgeEntryStatus
{
    /// <summary>Entrada sugerida pela IA, aguardando validação humana.</summary>
    Suggested = 0,

    /// <summary>Entrada validada — incorporada à base de conhecimento organizacional.</summary>
    Validated = 1,

    /// <summary>Entrada descartada — considerada irrelevante ou imprecisa.</summary>
    Discarded = 2
}
