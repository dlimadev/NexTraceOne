namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Severidade de um evento operacional da plataforma.
/// Utilizado para filtragem e priorização de eventos no painel de operações.
/// </summary>
public enum PlatformEventSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
