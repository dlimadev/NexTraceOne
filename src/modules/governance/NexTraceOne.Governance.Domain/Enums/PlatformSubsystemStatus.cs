namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado operacional de um subsistema da plataforma NexTraceOne.
/// Utilizado para monitorização de saúde e dashboards de status.
/// </summary>
public enum PlatformSubsystemStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2,
    Unknown = 3
}
