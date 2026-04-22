namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de cobertura de peer review em mudanças e contratos.
/// Por omissão satisfeita por <c>NullPeerReviewCoverageReader</c> (honest-null).
/// Wave AP.2 — GetPeerReviewCoverageReport.
/// </summary>
public interface IPeerReviewCoverageReader
{
    Task<PeerReviewTenantData> GetByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record PeerReviewTenantData(
        IReadOnlyList<ChangeReviewEntry> Changes,
        IReadOnlyList<ContractChangeEntry> ContractChanges,
        IReadOnlyList<ReviewBacklogEntry> ReviewBacklogs);

    public sealed record ChangeReviewEntry(
        string ChangeId,
        string ServiceName,
        string TeamName,
        bool HasPeerReview,
        int ReviewerCount,
        int BlastRadiusScore,
        int ConfidenceScore,
        IReadOnlyList<string> ReviewerIds);

    public sealed record ContractChangeEntry(
        string ContractId, string ContractName, bool HasReview, bool IsBreaking);

    public sealed record ReviewBacklogEntry(
        string ChangeId, string ServiceName, int PendingHours);
}
