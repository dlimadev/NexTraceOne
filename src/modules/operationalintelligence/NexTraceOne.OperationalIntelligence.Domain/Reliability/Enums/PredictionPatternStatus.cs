namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Estado do padrão preditivo no ciclo de vida.
/// Detected → Confirmed (validado) | Dismissed (falso positivo) | Stale (dados mudaram).
/// </summary>
public enum PredictionPatternStatus
{
    /// <summary>Padrão recém-detectado, aguarda validação.</summary>
    Detected,

    /// <summary>Padrão confirmado como válido para prevenção ativa.</summary>
    Confirmed,

    /// <summary>Padrão descartado como falso positivo ou irrelevante.</summary>
    Dismissed,

    /// <summary>Padrão obsoleto, dados subjacentes mudaram significativamente.</summary>
    Stale
}
