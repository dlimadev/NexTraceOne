namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Nível de prontidão de deployment da plataforma NexTraceOne.
/// Utilizado para avaliação de readiness em self-hosted e on-prem.
/// </summary>
public enum DeploymentReadinessLevel
{
    Ready = 0,
    PartiallyReady = 1,
    NotReady = 2,
    Unknown = 3
}
