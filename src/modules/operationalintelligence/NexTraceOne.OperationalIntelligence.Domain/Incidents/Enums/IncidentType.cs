namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Classificação do tipo de incidente operacional.
/// Reflete a natureza do problema detectado, permitindo correlação e mitigação contextualizada.
/// </summary>
public enum IncidentType
{
    /// <summary>Degradação de desempenho ou qualidade do serviço.</summary>
    ServiceDegradation = 0,

    /// <summary>Problema de disponibilidade total ou parcial.</summary>
    AvailabilityIssue = 1,

    /// <summary>Falha em dependência externa ou interna.</summary>
    DependencyFailure = 2,

    /// <summary>Impacto causado por alteração de contrato, schema ou payload.</summary>
    ContractImpact = 3,

    /// <summary>Problema em mensageria, tópicos ou filas.</summary>
    MessagingIssue = 4,

    /// <summary>Falha em processamento background, jobs ou schedulers.</summary>
    BackgroundProcessingIssue = 5,

    /// <summary>Regressão operacional detectada após mudança recente.</summary>
    OperationalRegression = 6
}
