namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Direção da tendência de confiabilidade de um serviço ao longo do tempo.
/// Utilizada para indicar se a saúde operacional está melhorando, estável ou em declínio.
/// </summary>
public enum TrendDirection
{
    /// <summary>Confiabilidade a melhorar — indicadores positivos nas últimas avaliações.</summary>
    Improving = 0,

    /// <summary>Confiabilidade estável — sem alterações significativas recentes.</summary>
    Stable = 1,

    /// <summary>Confiabilidade em declínio — tendência negativa nos indicadores operacionais.</summary>
    Declining = 2
}
