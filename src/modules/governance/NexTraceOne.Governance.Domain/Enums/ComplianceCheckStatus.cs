namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Resultado de um check de compliance.
/// </summary>
public enum ComplianceCheckStatus
{
    Passed = 0,
    Failed = 1,
    Warning = 2,
    Skipped = 3,
    Error = 4
}
