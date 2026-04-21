using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para definições de políticas do Policy Studio.
/// </summary>
internal sealed class PolicyDefinitionRepository(IdentityDbContext dbContext) : IPolicyDefinitionRepository
{
    /// <summary>Obtém uma definição de política pelo identificador.</summary>
    public async Task<PolicyDefinition?> GetByIdAsync(PolicyDefinitionId id, CancellationToken ct = default)
        => await dbContext.PolicyDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    /// <summary>Lista definições de políticas de um tenant, com filtro opcional por tipo.</summary>
    public async Task<IReadOnlyList<PolicyDefinition>> ListByTenantAsync(string tenantId, PolicyDefinitionType? type = null, CancellationToken ct = default)
    {
        var query = dbContext.PolicyDefinitions
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.PolicyType == type.Value);

        return await query.OrderBy(x => x.Name).ToListAsync(ct);
    }

    /// <summary>Lista definições de políticas activas por tipo.</summary>
    public async Task<IReadOnlyList<PolicyDefinition>> ListEnabledByTypeAsync(PolicyDefinitionType type, CancellationToken ct = default)
        => await dbContext.PolicyDefinitions
            .Where(x => x.PolicyType == type && x.IsEnabled && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    /// <summary>Adiciona uma nova definição de política.</summary>
    public void Add(PolicyDefinition policy)
        => dbContext.PolicyDefinitions.Add(policy);

    /// <summary>Marca a definição como modificada no change tracker.</summary>
    public void Update(PolicyDefinition policy)
        => dbContext.PolicyDefinitions.Update(policy);
}
