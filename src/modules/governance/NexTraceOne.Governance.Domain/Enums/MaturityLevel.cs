namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Nível de maturidade operacional de um serviço, equipa ou domínio.
/// </summary>
public enum MaturityLevel
{
    Initial = 0,
    Developing = 1,
    Defined = 2,
    Managed = 3,
    Optimizing = 4
}
