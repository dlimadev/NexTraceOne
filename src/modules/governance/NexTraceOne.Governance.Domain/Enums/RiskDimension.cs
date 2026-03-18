namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Dimensões de risco operacional avaliadas pelo NexTraceOne.
/// Cada dimensão representa um eixo de fragilidade contextual.
/// </summary>
public enum RiskDimension
{
    Operational = 0,
    Change = 1,
    Contract = 2,
    Dependency = 3,
    Ownership = 4,
    Documentation = 5,
    IncidentRecurrence = 6,
    AiGovernance = 7,

    // Enterprise Governance (Stage 3C)
    Waivers = 8,
    Rollouts = 9,
    Lifecycle = 10
}
