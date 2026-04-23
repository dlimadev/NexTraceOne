namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para avaliar o impacto de mudanças regulatórias sobre os serviços do tenant.
/// Por omissão satisfeita por <c>NullRegulatoryChangeImpactReader</c> (honest-null).
/// Wave BB.3 — GetRegulatoryChangeImpactReport.
/// </summary>
public interface IRegulatoryChangeImpactReader
{
    Task<IReadOnlyList<ServiceRegulatoryImpactEntry>> ListImpactedServicesAsync(
        string tenantId,
        string standardId,
        string newControlId,
        string scope,
        CancellationToken ct);

    Task<decimal> GetTenantRegulatoryReadinessScoreAsync(string tenantId, CancellationToken ct);

    public sealed record ServiceRegulatoryImpactEntry(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        string TeamId,
        bool HasExistingControl,
        string? MitigationPath,
        string EstimatedEffortLevel,
        int EstimatedEffortDays);
}
