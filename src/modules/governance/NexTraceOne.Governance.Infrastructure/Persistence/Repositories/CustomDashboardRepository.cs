using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de Custom Dashboards usando EF Core.
/// </summary>
internal sealed class CustomDashboardRepository(GovernanceDbContext context) : ICustomDashboardRepository
{
    public async Task<IReadOnlyList<CustomDashboard>> ListAsync(string? persona, CancellationToken ct)
    {
        var query = context.CustomDashboards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(d => d.Persona == persona);

        return await query.OrderByDescending(d => d.UpdatedAt).AsNoTracking().ToListAsync(ct);
    }

    public async Task<CustomDashboard?> GetByIdAsync(CustomDashboardId id, CancellationToken ct)
        => await context.CustomDashboards.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> CountAsync(string? persona, CancellationToken ct)
    {
        var query = context.CustomDashboards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(d => d.Persona == persona);

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(CustomDashboard dashboard, CancellationToken ct)
        => await context.CustomDashboards.AddAsync(dashboard, ct);

    public Task UpdateAsync(CustomDashboard dashboard, CancellationToken ct)
    {
        context.CustomDashboards.Update(dashboard);
        return Task.CompletedTask;
    }
}
