namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Estado de confiabilidade operacional de um serviço.
/// Classifica a condição atual do serviço combinando saúde, anomalias, incidentes
/// e impacto de mudanças recentes. Utilizado pela camada de Team-owned Service Reliability
/// para apresentar uma visão contextualizada da confiabilidade.
/// </summary>
public enum ReliabilityStatus
{
    /// <summary>Serviço operando dentro dos parâmetros normais — sem alertas ou anomalias ativas.</summary>
    Healthy = 0,

    /// <summary>Serviço apresenta degradação parcial — métricas abaixo do esperado ou anomalias detectadas.</summary>
    Degraded = 1,

    /// <summary>Serviço indisponível — falhas graves ou incidentes ativos impedem operação normal.</summary>
    Unavailable = 2,

    /// <summary>Serviço exige atenção — risco operacional identificado ou cobertura insuficiente.</summary>
    NeedsAttention = 3
}
