namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Classificação do resultado da review pós-release.
/// Determinada progressivamente por janelas de observação.
/// </summary>
public enum ReviewOutcome
{
    /// <summary>A mudança teve impacto positivo nos indicadores observados.</summary>
    Positive = 0,
    /// <summary>A mudança não causou impacto relevante — neutra.</summary>
    Neutral = 1,
    /// <summary>Indicadores requerem atenção, mas não há degradação grave.</summary>
    NeedsAttention = 2,
    /// <summary>A mudança causou degradação significativa nos indicadores.</summary>
    Negative = 3,
    /// <summary>Dados insuficientes ou tempo de observação curto demais para classificar.</summary>
    Inconclusive = 4
}
