using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IMultiDimensionalPromotionConfidenceReader"/>.
/// Retorna dados neutros (100 em todas as dimensões, sem histórico) até a infraestrutura real ser ligada.
/// Wave BC.3 — GetMultiDimensionalPromotionConfidenceReport.
/// </summary>
internal sealed class NullMultiDimensionalPromotionConfidenceReader
    : IMultiDimensionalPromotionConfidenceReader
{
    public Task<IMultiDimensionalPromotionConfidenceReader.PromotionDimensionData> GetByReleaseAsync(
        string tenantId, string releaseId, CancellationToken ct)
        => Task.FromResult(new IMultiDimensionalPromotionConfidenceReader.PromotionDimensionData(
            ReleaseId: releaseId,
            ServiceId: string.Empty,
            BlastRadiusScore: 100m,
            RollbackScore: 100m,
            EnvBehaviorScore: 100m,
            EvidenceIntegrityScore: 100m,
            ContractComplianceScore: 100m,
            SloHealthScore: 100m,
            ChaosResilienceScore: 100m,
            ChangePatternScore: 100m,
            MissingDimensions: []));

    public Task<IReadOnlyList<IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry>>
        GetHistoricalOutcomesAsync(string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry>>([]);
}
