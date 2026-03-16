namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Tipo de escopo para aplicação de governança.
/// </summary>
public enum GovernanceScopeType
{
    Global = 0,
    Domain = 1,
    Team = 2,
    Environment = 3,
    ServiceCriticality = 4,
    ContractType = 5,
    AIUsageScope = 6
}
