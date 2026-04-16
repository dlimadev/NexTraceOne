using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de PolicyAsCodeDefinition usando EF Core.
/// </summary>
internal sealed class PolicyAsCodeRepository(GovernanceDbContext context) : IPolicyAsCodeRepository
{
    public async Task<IReadOnlyList<PolicyAsCodeDefinition>> ListAsync(
        PolicyDefinitionStatus? status,
        PolicyEnforcementMode? enforcementMode,
        CancellationToken ct)
    {
        var query = context.PolicyAsCodeDefinitions.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (enforcementMode.HasValue)
            query = query.Where(p => p.EnforcementMode == enforcementMode.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<PolicyAsCodeDefinition?> GetByIdAsync(PolicyAsCodeDefinitionId id, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PolicyAsCodeDefinition?> GetByNameAsync(string name, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.SingleOrDefaultAsync(p => p.Name == name, ct);

    public async Task AddAsync(PolicyAsCodeDefinition definition, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.AddAsync(definition, ct);

    public Task UpdateAsync(PolicyAsCodeDefinition definition, CancellationToken ct)
    {
        context.PolicyAsCodeDefinitions.Update(definition);
        return Task.CompletedTask;
    }
}
