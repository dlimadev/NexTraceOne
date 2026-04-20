namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Tipo de origem dos dados de um dataset de avaliação.</summary>
public enum EvaluationDatasetSourceType
{
    /// <summary>Casos estáticos validados manualmente por especialistas.</summary>
    Curated = 0,

    /// <summary>Casos capturados a partir de trajectórias reais do Agent Lightning.</summary>
    Generated = 1,

    /// <summary>Casos gerados por outro modelo de IA (identificado explicitamente).</summary>
    Synthetic = 2
}
