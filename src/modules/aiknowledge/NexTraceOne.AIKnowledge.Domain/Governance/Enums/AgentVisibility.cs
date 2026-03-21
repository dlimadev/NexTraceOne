namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Visibilidade do agent — controla quem pode ver e usar o agent.
/// Private: apenas o criador. Team: equipa do criador. Tenant: toda a organização.
/// </summary>
public enum AgentVisibility
{
    Private = 0,
    Team = 1,
    Tenant = 2,
}
