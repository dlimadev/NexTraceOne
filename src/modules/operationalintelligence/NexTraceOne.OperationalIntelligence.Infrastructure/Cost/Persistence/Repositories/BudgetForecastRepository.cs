using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

internal sealed class BudgetForecastRepository(CostIntelligenceDbContext context)
    : RepositoryBase<BudgetForecast, BudgetForecastId>(context), IBudgetForecastRepository
{
    public async Task<BudgetForecast?> GetLatestByServiceAsync(
        string serviceId,
        string environment,
        CancellationToken ct = default)
        => await context.BudgetForecasts
            .Where(f => f.ServiceId == serviceId && f.Environment == environment)
            .OrderByDescending(f => f.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BudgetForecast>> ListByServiceAsync(
        string serviceId,
        string environment,
        CancellationToken ct = default)
        => await context.BudgetForecasts
            .Where(f => f.ServiceId == serviceId && f.Environment == environment)
            .OrderByDescending(f => f.ComputedAt)
            .ToListAsync(ct);
}
