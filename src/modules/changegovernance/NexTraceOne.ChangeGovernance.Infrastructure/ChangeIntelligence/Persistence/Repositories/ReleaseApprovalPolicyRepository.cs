using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de ReleaseApprovalPolicy — políticas de aprovação configuráveis.
/// </summary>
internal sealed class ReleaseApprovalPolicyRepository(ChangeIntelligenceDbContext context)
    : IReleaseApprovalPolicyRepository
{
    /// <inheritdoc />
    public async Task<ReleaseApprovalPolicy?> GetByIdAsync(
        ReleaseApprovalPolicyId id, CancellationToken cancellationToken = default)
        => await context.ApprovalPolicies
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseApprovalPolicy>> ListActiveAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
        => await context.ApprovalPolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseApprovalPolicy>> ListByEnvironmentAndServiceAsync(
        Guid tenantId,
        string? environmentId,
        Guid? serviceId,
        CancellationToken cancellationToken = default)
        => await context.ApprovalPolicies
            .Where(p => p.TenantId == tenantId
                && p.IsActive
                && (p.EnvironmentId == null || p.EnvironmentId == environmentId)
                && (p.ServiceId == null || p.ServiceId == serviceId))
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(ReleaseApprovalPolicy policy)
        => context.ApprovalPolicies.Add(policy);

    /// <inheritdoc />
    public void Remove(ReleaseApprovalPolicy policy)
        => context.ApprovalPolicies.Remove(policy);
}
