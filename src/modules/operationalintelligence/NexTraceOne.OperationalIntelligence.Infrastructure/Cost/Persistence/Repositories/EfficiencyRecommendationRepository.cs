using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

internal sealed class EfficiencyRecommendationRepository(CostIntelligenceDbContext context)
    : RepositoryBase<EfficiencyRecommendation, EfficiencyRecommendationId>(context), IEfficiencyRecommendationRepository
{
    public async Task<IReadOnlyList<EfficiencyRecommendation>> ListByServiceAsync(
        string serviceId,
        string environment,
        CancellationToken ct = default)
        => await context.EfficiencyRecommendations
            .Where(r => r.ServiceId == serviceId && r.Environment == environment)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EfficiencyRecommendation>> ListUnacknowledgedAsync(
        CancellationToken ct = default)
        => await context.EfficiencyRecommendations
            .Where(r => !r.IsAcknowledged)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    public void AddRange(IEnumerable<EfficiencyRecommendation> recommendations)
        => context.EfficiencyRecommendations.AddRange(recommendations);
}
