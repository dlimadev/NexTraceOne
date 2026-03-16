namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado de execução de um background job da plataforma.
/// Utilizado para monitorização de jobs e alertas de stale processing.
/// </summary>
public enum BackgroundJobStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Stale = 3,
    Paused = 4
}
