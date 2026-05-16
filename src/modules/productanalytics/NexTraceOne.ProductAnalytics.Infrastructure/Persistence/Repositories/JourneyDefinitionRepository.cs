using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de JourneyDefinitions.
/// Suporta scope global (TenantId null) e por tenant.
/// Aplica filtro de TenantId em defense-in-depth.
/// </summary>
internal sealed class JourneyDefinitionRepository(
    ProductAnalyticsDbContext context,
    ICurrentTenant currentTenant) : IJourneyDefinitionRepository
{
    public async Task<IReadOnlyList<JourneyDefinition>> ListActiveAsync(Guid? tenantId, CancellationToken ct)
    {
        // Defense-in-depth: ensure caller only sees definitions for the current tenant or global
        var effectiveTenantId = tenantId ?? currentTenant.Id;

        // Load global definitions + tenant-specific definitions
        var definitions = await context.JourneyDefinitions
            .Where(d => d.IsActive && (d.TenantId == null || d.TenantId == effectiveTenantId))
            .OrderBy(d => d.TenantId == null ? 0 : 1)  // globals first
            .ThenBy(d => d.Key)
            .AsNoTracking()
            .ToListAsync(ct);

        // Tenant definitions override global ones with the same key
        var result = new Dictionary<string, JourneyDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in definitions)
        {
            result[def.Key] = def; // tenant-specific (loaded last) overrides global
        }

        return result.Values.OrderBy(d => d.Key).ToList();
    }

    public async Task<JourneyDefinition?> GetByIdAsync(JourneyDefinitionId id, CancellationToken ct)
        => await context.JourneyDefinitions
            .FirstOrDefaultAsync(d => d.Id == id && (d.TenantId == null || d.TenantId == currentTenant.Id), ct);

    public async Task<JourneyDefinition?> GetByKeyAsync(string key, Guid? tenantId, CancellationToken ct)
    {
        var effectiveTenantId = tenantId ?? currentTenant.Id;
        return await context.JourneyDefinitions
            .FirstOrDefaultAsync(d => d.Key == key.ToLowerInvariant() && d.TenantId == effectiveTenantId, ct);
    }

    public async Task<bool> ExistsAsync(string key, Guid? tenantId, CancellationToken ct)
    {
        var effectiveTenantId = tenantId ?? currentTenant.Id;
        return await context.JourneyDefinitions
            .AnyAsync(d => d.Key == key.ToLowerInvariant() && d.TenantId == effectiveTenantId, ct);
    }

    public async Task AddAsync(JourneyDefinition definition, CancellationToken ct)
        => await context.JourneyDefinitions.AddAsync(definition, ct);

    public void Remove(JourneyDefinition definition)
        => context.JourneyDefinitions.Remove(definition);
}
