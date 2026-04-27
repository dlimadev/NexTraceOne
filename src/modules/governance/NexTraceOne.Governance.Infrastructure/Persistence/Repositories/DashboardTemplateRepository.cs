using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para DashboardTemplate (V3.8).</summary>
public sealed class DashboardTemplateRepository(GovernanceDbContext db) : IDashboardTemplateRepository
{
    public async Task<DashboardTemplate?> GetByIdAsync(DashboardTemplateId id, string? tenantId, CancellationToken ct = default)
        => await db.DashboardTemplates
            .FirstOrDefaultAsync(t => t.Id == id && (t.IsSystem || t.TenantId == tenantId), ct);

    public async Task<(IReadOnlyList<DashboardTemplate> Items, int Total)> ListAsync(
        string tenantId, string? category, string? persona, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.DashboardTemplates
            .Where(t => t.IsSystem || t.TenantId == tenantId);

        if (category is not null)
            query = query.Where(t => t.Category == category.ToLowerInvariant());

        if (persona is not null)
            query = query.Where(t => t.Persona == persona);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.IsSystem)
            .ThenByDescending(t => t.InstallCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(DashboardTemplate template, CancellationToken ct = default)
        => await db.DashboardTemplates.AddAsync(template, ct);

    public Task UpdateAsync(DashboardTemplate template, CancellationToken ct = default)
    {
        db.DashboardTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(DashboardTemplateId id, string tenantId, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, tenantId, ct);
        if (entity is not null)
            db.DashboardTemplates.Remove(entity);
    }
}
