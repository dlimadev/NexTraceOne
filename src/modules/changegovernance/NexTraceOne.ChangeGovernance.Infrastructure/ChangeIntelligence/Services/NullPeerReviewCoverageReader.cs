using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IPeerReviewCoverageReader"/>.
/// Retorna <see cref="IPeerReviewCoverageReader.PeerReviewTenantData"/> com listas vazias
/// quando o bridge com dados de revisão entre pares não está configurado.
///
/// Wave AP.2 — GetPeerReviewCoverageReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullPeerReviewCoverageReader : IPeerReviewCoverageReader
{
    /// <inheritdoc/>
    public Task<IPeerReviewCoverageReader.PeerReviewTenantData> GetByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult(new IPeerReviewCoverageReader.PeerReviewTenantData(
            Changes: [],
            ContractChanges: [],
            ReviewBacklogs: []));
}
