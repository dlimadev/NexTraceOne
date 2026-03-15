namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Status da mitigação associada a um incidente.
/// Permite rastrear a progressão das ações corretivas.
/// </summary>
public enum MitigationStatus
{
    /// <summary>Nenhuma mitigação iniciada.</summary>
    NotStarted = 0,

    /// <summary>Mitigação em andamento.</summary>
    InProgress = 1,

    /// <summary>Mitigação aplicada — aguardando validação.</summary>
    Applied = 2,

    /// <summary>Mitigação verificada e eficaz.</summary>
    Verified = 3,

    /// <summary>Mitigação falhou — necessita nova abordagem.</summary>
    Failed = 4
}
