namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Modo de deployment da plataforma NexTraceOne.
/// Define restrições de funcionalidade, licenciamento e topologia.
/// </summary>
public enum DeploymentMode
{
    SaaS = 0,
    OnPremise = 1,
    Hybrid = 2
}
