using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core de EnvironmentAccessPolicy.</summary>
internal sealed class EnvironmentAccessPolicyRepository(IdentityDbContext db)
    : IEnvironmentAccessPolicyRepository
{
    /// <inheritdoc />
    public async Task<EnvironmentAccessPolicy?> GetByIdAsync(
        EnvironmentAccessPolicyId id, CancellationToken cancellationToken)
        => await db.EnvironmentAccessPolicies
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnvironmentAccessPolicy>> ListByTenantAsync(
        Guid tenantId, CancellationToken cancellationToken)
        => await db.EnvironmentAccessPolicies
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.PolicyName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(EnvironmentAccessPolicy policy, CancellationToken cancellationToken)
        => await db.EnvironmentAccessPolicies.AddAsync(policy, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(EnvironmentAccessPolicy policy, CancellationToken cancellationToken)
    {
        db.EnvironmentAccessPolicies.Update(policy);
        return Task.CompletedTask;
    }
}
