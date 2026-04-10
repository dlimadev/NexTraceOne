using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ContractCompliancePolicy usando EF Core.
/// </summary>
internal sealed class ContractCompliancePolicyRepository(ConfigurationDbContext context)
    : IContractCompliancePolicyRepository
{
    public async Task<ContractCompliancePolicy?> GetByIdAsync(
        ContractCompliancePolicyId id, CancellationToken cancellationToken)
        => await context.ContractCompliancePolicies.SingleOrDefaultAsync(
            p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ContractCompliancePolicy>> ListByTenantAsync(
        string tenantId, CancellationToken cancellationToken)
        => await context.ContractCompliancePolicies
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<ContractCompliancePolicy?> GetByScopeAsync(
        string tenantId, PolicyScope scope, string? scopeId, CancellationToken cancellationToken)
        => await context.ContractCompliancePolicies
            .Where(p => p.TenantId == tenantId && p.Scope == scope && p.ScopeId == scopeId)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ContractCompliancePolicy>> ListActiveByScopeAsync(
        string tenantId, PolicyScope scope, CancellationToken cancellationToken)
        => await context.ContractCompliancePolicies
            .Where(p => p.TenantId == tenantId && p.Scope == scope && p.IsActive)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ContractCompliancePolicy policy, CancellationToken cancellationToken)
        => await context.ContractCompliancePolicies.AddAsync(policy, cancellationToken);

    public async Task DeleteAsync(ContractCompliancePolicyId id, CancellationToken cancellationToken)
    {
        var entity = await context.ContractCompliancePolicies.SingleOrDefaultAsync(
            p => p.Id == id, cancellationToken);
        if (entity is not null) context.ContractCompliancePolicies.Remove(entity);
    }
}
