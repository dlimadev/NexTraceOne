namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Status do ciclo de vida de um incidente.
/// Representa a progressão desde a detecção até o encerramento.
/// </summary>
public enum IncidentStatus
{
    /// <summary>Incidente registado — aguarda investigação.</summary>
    Open = 0,

    /// <summary>Investigação em andamento — causa sendo apurada.</summary>
    Investigating = 1,

    /// <summary>Mitigação em andamento — ações corretivas sendo aplicadas.</summary>
    Mitigating = 2,

    /// <summary>Monitorização pós-mitigação — aguardando confirmação de estabilidade.</summary>
    Monitoring = 3,

    /// <summary>Incidente resolvido — serviço restaurado.</summary>
    Resolved = 4,

    /// <summary>Incidente encerrado — análise final concluída.</summary>
    Closed = 5
}
