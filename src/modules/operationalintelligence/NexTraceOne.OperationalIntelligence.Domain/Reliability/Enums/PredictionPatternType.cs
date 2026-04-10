namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Tipo de padrão preditivo identificado pela análise de dados históricos.
/// </summary>
public enum PredictionPatternType
{
    /// <summary>Padrão relacionado com timing de deploy (ex: sexta-feira à tarde).</summary>
    DeployTiming,

    /// <summary>Padrão relacionado com mudanças em contratos sem testes adequados.</summary>
    ContractChange,

    /// <summary>Padrão de correlação entre serviços (ex: falha em cascata).</summary>
    ServiceCorrelation,

    /// <summary>Padrão relacionado com volume ou frequência de deploys.</summary>
    DeployFrequency,

    /// <summary>Padrão de regressão após tipo específico de mudança.</summary>
    ChangeRegression,

    /// <summary>Padrão de degradação de métricas pré-incidente.</summary>
    MetricDegradation
}
