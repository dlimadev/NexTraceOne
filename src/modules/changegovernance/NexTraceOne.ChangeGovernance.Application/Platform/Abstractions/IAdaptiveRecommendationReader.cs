using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetAdaptiveRecommendationReport;

namespace NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

/// <summary>
/// Abstracção para leitura de sinais de recomendação adaptativa do tenant.
/// Por omissão satisfeita por <c>NullAdaptiveRecommendationReader</c> (honest-null).
/// Wave AU.3 — GetAdaptiveRecommendationReport.
/// </summary>
public interface IAdaptiveRecommendationReader
{
    /// <summary>Retorna sinais de recomendação para o tenant.</summary>
    Task<IReadOnlyList<RecommendationSignal>> GetSignalsAsync(string tenantId, CancellationToken ct);

    /// <summary>Sinal bruto de recomendação adaptativa.</summary>
    public sealed record RecommendationSignal(
        Guid RecommendationId,
        GetAdaptiveRecommendationReport.RecommendationCategory Category,
        string Title,
        string Description,
        int ImpactScore,
        GetAdaptiveRecommendationReport.EffortEstimate EffortEstimate,
        IReadOnlyList<string> AffectedServices,
        IReadOnlyList<string> AffectedTeams,
        string RecommendationSource,
        IReadOnlyList<string> EvidenceLinks);
}
