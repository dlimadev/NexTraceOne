namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado de um waiver/exceção de governança.
/// </summary>
public enum WaiverStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3,
    Revoked = 4
}
