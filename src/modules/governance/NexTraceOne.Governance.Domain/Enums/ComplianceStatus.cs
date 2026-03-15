namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado de compliance técnico-operacional para um indicador específico.
/// </summary>
public enum ComplianceStatus
{
    Compliant = 0,
    PartiallyCompliant = 1,
    NonCompliant = 2,
    NotApplicable = 3
}
