using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade EfficiencyRecommendation.
/// </summary>
public interface IEfficiencyRecommendationRepository
{
    Task<IReadOnlyList<EfficiencyRecommendation>> ListByServiceAsync(string serviceId, string environment, CancellationToken ct = default);
    Task<IReadOnlyList<EfficiencyRecommendation>> ListUnacknowledgedAsync(CancellationToken ct = default);
    void Add(EfficiencyRecommendation recommendation);
    void AddRange(IEnumerable<EfficiencyRecommendation> recommendations);
}
