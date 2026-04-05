using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para ServiceFailurePrediction.</summary>
internal sealed class ServiceFailurePredictionRepository(ReliabilityDbContext context)
    : IServiceFailurePredictionRepository
{
    public async Task<ServiceFailurePrediction?> GetByServiceAsync(
        string serviceId, string environment, string horizon, CancellationToken ct)
        => await context.ServiceFailurePredictions
            .AsNoTracking()
            .Where(p => p.ServiceId == serviceId && p.Environment == environment && p.PredictionHorizon == horizon)
            .OrderByDescending(p => p.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ServiceFailurePrediction>> ListAsync(
        string? environment, string? riskLevel, CancellationToken ct)
    {
        var query = context.ServiceFailurePredictions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(p => p.Environment == environment);
        if (!string.IsNullOrWhiteSpace(riskLevel))
            query = query.Where(p => p.RiskLevel == riskLevel);
        return await query.OrderByDescending(p => p.ComputedAt).ToListAsync(ct);
    }

    public void Add(ServiceFailurePrediction prediction)
        => context.ServiceFailurePredictions.Add(prediction);
}
