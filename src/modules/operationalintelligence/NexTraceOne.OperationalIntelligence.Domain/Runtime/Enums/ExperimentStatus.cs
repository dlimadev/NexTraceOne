namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado do ciclo de vida de um experimento de chaos engineering.
/// </summary>
public enum ExperimentStatus
{
    /// <summary>Experimento planeado mas ainda não executado.</summary>
    Planned = 0,

    /// <summary>Experimento em execução ativa.</summary>
    Running = 1,

    /// <summary>Experimento concluído com sucesso.</summary>
    Completed = 2,

    /// <summary>Experimento falhou durante execução.</summary>
    Failed = 3,

    /// <summary>Experimento cancelado antes da conclusão.</summary>
    Cancelled = 4,
}
