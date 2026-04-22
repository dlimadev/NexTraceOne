namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de escalações de governança (Break Glass, JIT, delegações).
/// Por omissão satisfeita por <c>NullGovernanceEscalationReader</c> (honest-null).
/// Wave AP.3 — GetGovernanceEscalationReport.
/// </summary>
public interface IGovernanceEscalationReader
{
    Task<GovernanceEscalationData> GetByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record GovernanceEscalationData(
        IReadOnlyList<BreakGlassEventEntry> BreakGlassEvents,
        IReadOnlyList<JitAccessEntry> JitAccessRequests,
        decimal? PreviousPeriodBreakGlassCount);

    public sealed record BreakGlassEventEntry(
        string EventId, string UserId, string UserName,
        string Environment, DateTimeOffset OccurredAt,
        DateTimeOffset? ResolvedAt, bool IsProduction);

    public sealed record JitAccessEntry(
        string RequestId, string UserId, string UserName,
        bool IsApproved, bool IsRejected, bool IsAutoApproved,
        DateTimeOffset GrantedAt, DateTimeOffset? ExpiresAt,
        DateTimeOffset? LastUsedAt);
}
