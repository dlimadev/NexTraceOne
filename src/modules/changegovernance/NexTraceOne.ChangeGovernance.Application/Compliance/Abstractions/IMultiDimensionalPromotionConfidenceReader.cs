namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de confiança multi-dimensional para promoção.
/// Por omissão satisfeita por <c>NullMultiDimensionalPromotionConfidenceReader</c> (honest-null).
/// Wave BC.3 — GetMultiDimensionalPromotionConfidenceReport.
/// </summary>
public interface IMultiDimensionalPromotionConfidenceReader
{
    Task<PromotionDimensionData> GetByReleaseAsync(
        string tenantId, string releaseId, CancellationToken ct);

    Task<IReadOnlyList<HistoricalOutcomeEntry>> GetHistoricalOutcomesAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record PromotionDimensionData(
        string ReleaseId,
        string ServiceId,
        decimal BlastRadiusScore,
        decimal RollbackScore,
        decimal EnvBehaviorScore,
        decimal EvidenceIntegrityScore,
        decimal ContractComplianceScore,
        decimal SloHealthScore,
        decimal ChaosResilienceScore,
        decimal ChangePatternScore,
        IReadOnlyList<string> MissingDimensions);

    public sealed record HistoricalOutcomeEntry(
        string ReleaseId,
        decimal ConfidenceScoreAtPromotion,
        bool SuccessfulPromotion,
        DateTimeOffset PromotedAt);
}
