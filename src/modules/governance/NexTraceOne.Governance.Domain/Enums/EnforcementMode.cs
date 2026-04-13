namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Modo de enforcement para regras de um governance pack.
/// </summary>
public enum EnforcementMode
{
    Advisory = 0,
    Required = 1,
    Blocking = 2,
    Warning = 3
}
