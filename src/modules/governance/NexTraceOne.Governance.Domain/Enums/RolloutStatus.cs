namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado de um rollout de governance pack.
/// </summary>
public enum RolloutStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    RolledBack = 3,
    Failed = 4
}
