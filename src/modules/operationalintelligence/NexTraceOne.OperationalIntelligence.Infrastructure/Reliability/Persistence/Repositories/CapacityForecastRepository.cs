using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para CapacityForecast.</summary>
internal sealed class CapacityForecastRepository(ReliabilityDbContext context)
    : ICapacityForecastRepository
{
    public async Task<CapacityForecast?> GetByServiceAndResourceAsync(
        string serviceId, string environment, string resourceType, CancellationToken ct)
        => await context.CapacityForecasts
            .AsNoTracking()
            .Where(f => f.ServiceId == serviceId && f.Environment == environment && f.ResourceType == resourceType)
            .OrderByDescending(f => f.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CapacityForecast>> ListAsync(
        string? environment, string? saturationRisk, CancellationToken ct)
    {
        var query = context.CapacityForecasts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(f => f.Environment == environment);
        if (!string.IsNullOrWhiteSpace(saturationRisk))
            query = query.Where(f => f.SaturationRisk == saturationRisk);
        return await query.OrderByDescending(f => f.ComputedAt).ToListAsync(ct);
    }

    public void Add(CapacityForecast forecast)
        => context.CapacityForecasts.Add(forecast);
}
