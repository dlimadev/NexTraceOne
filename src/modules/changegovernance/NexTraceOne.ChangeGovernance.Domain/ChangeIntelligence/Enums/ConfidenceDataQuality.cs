namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Nível de qualidade dos dados que alimentam um sub-score do Change Confidence Score 2.0.
/// Reflete se a fonte está disponível e confiável, ou se dados simulados foram utilizados.
/// </summary>
public enum ConfidenceDataQuality
{
    /// <summary>Dados indisponíveis ou simulados — sub-score usa valor conservador.</summary>
    Low = 0,

    /// <summary>Dados parcialmente disponíveis ou com incerteza moderada.</summary>
    Medium = 1,

    /// <summary>Dados reais disponíveis com alta confiabilidade.</summary>
    High = 2
}
