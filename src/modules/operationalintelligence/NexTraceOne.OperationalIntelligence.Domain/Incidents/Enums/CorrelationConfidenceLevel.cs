namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Nível de confiança usado no motor dinâmico de correlação incidente↔mudança.
/// Calculado com base no critério de correspondência aplicado.
/// </summary>
public enum CorrelationConfidenceLevel
{
    /// <summary>Correspondência exata de serviço — confiança alta.</summary>
    High = 0,

    /// <summary>Correspondência por dependência ou correspondência parcial — confiança média.</summary>
    Medium = 1,

    /// <summary>Correspondência por proximidade temporal apenas — confiança baixa.</summary>
    Low = 2
}
