namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Categoria de uma regra dentro de um governance pack.
/// </summary>
public enum GovernanceRuleCategory
{
    Contracts = 0,
    SourceOfTruth = 1,
    Changes = 2,
    Incidents = 3,
    AIGovernance = 4,
    Reliability = 5,
    Operations = 6,
    ContractQuality = 7
}
