namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para leitura de dados comparativos de comportamento entre ambientes (Pre-Prod vs Prod).
/// Por omissão satisfeita por <c>NullEnvironmentBehaviorComparisonReader</c> (honest-null).
/// Wave BC.1 — GetEnvironmentBehaviorComparisonReport.
/// </summary>
public interface IEnvironmentBehaviorComparisonReader
{
    Task<IReadOnlyList<ServiceBehaviorEntry>> ListByTenantAsync(
        string tenantId, string sourceEnvironment, string targetEnvironment, CancellationToken ct);

    Task<IReadOnlyList<PromotionOutcomeEntry>> GetHistoricalPromotionOutcomesAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record ServiceBehaviorEntry(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        decimal SourceP99Ms,
        decimal TargetP99Ms,
        decimal SourceErrorRatePct,
        decimal TargetErrorRatePct,
        decimal SourceAvailabilityPct,
        decimal TargetAvailabilityPct,
        bool ConfigDriftDetected,
        int ConfigDriftKeyCount);

    public sealed record PromotionOutcomeEntry(
        DateTimeOffset PromotedAt,
        string ServiceId,
        bool SuccessfulPromotion,
        decimal SimilarityScoreAtPromotion);
}
