namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Tipos de sub-score que compõem o Change Confidence Score 2.0.
/// Cada dimensão mede um aspeto distinto do risco e confiança de uma mudança.
/// </summary>
public enum ConfidenceSubScoreType
{
    /// <summary>Cobertura de testes reportada pelo pipeline de CI.</summary>
    TestCoverage = 0,

    /// <summary>Estabilidade dos contratos afetados pela mudança (breaking changes).</summary>
    ContractStability = 1,

    /// <summary>Taxa histórica de regressões em releases anteriores do mesmo serviço.</summary>
    HistoricalRegression = 2,

    /// <summary>Dimensão do blast radius (serviços, contratos e consumidores impactados).</summary>
    BlastSurface = 3,

    /// <summary>Score agregado de saúde dos serviços dependentes.</summary>
    DependencyHealth = 4,

    /// <summary>Sinal do canary deployment (error rate e latência observados).</summary>
    CanarySignal = 5,

    /// <summary>Delta de comportamento entre staging e baseline de produção.</summary>
    PreProdDelta = 6
}
