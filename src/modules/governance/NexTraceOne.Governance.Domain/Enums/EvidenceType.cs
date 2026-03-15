namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Tipo de evidência num pacote de evidência.
/// </summary>
public enum EvidenceType
{
    Approval = 0,
    ChangeHistory = 1,
    ContractPublication = 2,
    MitigationRecord = 3,
    AiUsageRecord = 4,
    PolicyDecision = 5,
    ComplianceResult = 6,
    AuditReference = 7
}
