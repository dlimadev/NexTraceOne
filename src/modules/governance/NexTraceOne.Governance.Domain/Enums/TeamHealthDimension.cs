namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Dimensões de saúde de uma equipa avaliadas no Team Health Dashboard.
/// Cada dimensão contribui com um score de 0 a 100 para o cálculo do score composto.
/// </summary>
public enum TeamHealthDimension
{
    /// <summary>Quantidade de serviços geridos pela equipa.</summary>
    ServiceCount = 1,

    /// <summary>Saúde dos contratos (API, eventos, SOAP) publicados pela equipa.</summary>
    ContractHealth = 2,

    /// <summary>Frequência de incidentes associados aos serviços da equipa.</summary>
    IncidentFrequency = 3,

    /// <summary>Tempo médio de resolução de incidentes (Mean Time To Resolve).</summary>
    MeanTimeToResolve = 4,

    /// <summary>Nível de dívida técnica acumulada nos serviços da equipa.</summary>
    TechDebt = 5,

    /// <summary>Cobertura de documentação operacional e técnica.</summary>
    DocumentationCoverage = 6,

    /// <summary>Nível de conformidade com políticas de governança.</summary>
    PolicyCompliance = 7
}
