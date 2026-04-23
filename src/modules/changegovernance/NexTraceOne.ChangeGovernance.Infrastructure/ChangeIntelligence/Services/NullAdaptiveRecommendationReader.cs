using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IAdaptiveRecommendationReader"/>.
/// Retorna lista vazia quando o bridge com sinais de recomendação não está configurado.
///
/// Wave AU.3 — GetAdaptiveRecommendationReport (ChangeGovernance Platform).
/// </summary>
internal sealed class NullAdaptiveRecommendationReader : IAdaptiveRecommendationReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IAdaptiveRecommendationReader.RecommendationSignal>> GetSignalsAsync(
        string tenantId,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IAdaptiveRecommendationReader.RecommendationSignal>>([]);
}
