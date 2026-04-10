using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para recomendações de self-healing (HealingRecommendation).
/// </summary>
internal sealed class HealingRecommendationRepository(ReliabilityDbContext context)
    : IHealingRecommendationRepository
{
    public async Task<HealingRecommendation?> GetByIdAsync(HealingRecommendationId id, CancellationToken ct)
        => await context.HealingRecommendations
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<HealingRecommendation>> ListByServiceAsync(
        string serviceName, CancellationToken ct)
        => await context.HealingRecommendations
            .Where(r => r.ServiceName == serviceName)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HealingRecommendation>> ListByStatusAsync(
        HealingRecommendationStatus? status,
        string? serviceName,
        CancellationToken ct)
    {
        var query = context.HealingRecommendations.AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(r => r.ServiceName == serviceName);

        return await query
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public void Add(HealingRecommendation recommendation)
        => context.HealingRecommendations.Add(recommendation);

    public void Update(HealingRecommendation recommendation)
        => context.HealingRecommendations.Update(recommendation);
}
