using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de PolicyDefinitionVersion usando EF Core.
/// Append-only — sem operações de actualização.
/// </summary>
internal sealed class PolicyDefinitionVersionRepository(PlatformGovernanceDbContext context)
    : IPolicyDefinitionVersionRepository
{
    public async Task<IReadOnlyList<PolicyDefinitionVersion>> ListByPolicyIdAsync(
        PolicyAsCodeDefinitionId policyId,
        CancellationToken ct)
        => await context.PolicyDefinitionVersions
            .Where(v => v.PolicyId == policyId)
            .OrderByDescending(v => v.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<PolicyDefinitionVersion?> GetByVersionAsync(
        PolicyAsCodeDefinitionId policyId,
        string version,
        CancellationToken ct)
        => await context.PolicyDefinitionVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.PolicyId == policyId && v.Version == version, ct);

    public async Task AddAsync(PolicyDefinitionVersion version, CancellationToken ct)
        => await context.PolicyDefinitionVersions.AddAsync(version, ct);
}
