using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Interface do repositório de HealingRecommendation.
/// Define operações para recomendações de self-healing.
/// </summary>
public interface IHealingRecommendationRepository
{
    Task<HealingRecommendation?> GetByIdAsync(HealingRecommendationId id, CancellationToken ct);

    Task<IReadOnlyList<HealingRecommendation>> ListByServiceAsync(
        string serviceName, CancellationToken ct);

    Task<IReadOnlyList<HealingRecommendation>> ListByStatusAsync(
        HealingRecommendationStatus? status,
        string? serviceName,
        CancellationToken ct);

    void Add(HealingRecommendation recommendation);

    void Update(HealingRecommendation recommendation);
}
