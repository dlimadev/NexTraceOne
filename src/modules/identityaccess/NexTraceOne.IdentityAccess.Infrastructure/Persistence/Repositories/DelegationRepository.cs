using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de delegações formais.
/// </summary>
internal sealed class DelegationRepository(IdentityDbContext dbContext) : IDelegationRepository
{
    public async Task<Delegation?> GetByIdAsync(DelegationId id, CancellationToken cancellationToken)
        => await dbContext.Delegations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Delegation>> ListActiveByDelegateeAsync(UserId delegateeId, DateTimeOffset now, CancellationToken cancellationToken)
        => await dbContext.Delegations
            .Where(x => x.DelegateeId == delegateeId
                && x.Status == DelegationStatus.Active
                && x.ValidFrom <= now
                && x.ValidUntil > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Delegation>> ListByGrantorAsync(UserId grantorId, CancellationToken cancellationToken)
        => await dbContext.Delegations
            .Where(x => x.GrantorId == grantorId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Delegation>> ListActiveByTenantAsync(TenantId tenantId, DateTimeOffset now, CancellationToken cancellationToken)
        => await dbContext.Delegations
            .Where(x => x.TenantId == tenantId
                && x.Status == DelegationStatus.Active
                && x.ValidFrom <= now
                && x.ValidUntil > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(Delegation delegation)
        => dbContext.Delegations.Add(delegation);
}
