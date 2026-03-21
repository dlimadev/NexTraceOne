namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de ownership do agent — determina quem pode gerir o agent.
/// System: plataforma (imutável por tenants). Tenant: organização. User: utilizador individual.
/// </summary>
public enum AgentOwnershipType
{
    System = 0,
    Tenant = 1,
    User = 2,
}
