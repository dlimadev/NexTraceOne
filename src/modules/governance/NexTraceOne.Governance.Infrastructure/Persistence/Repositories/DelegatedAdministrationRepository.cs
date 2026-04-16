using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de DelegatedAdministrations usando EF Core.
/// </summary>
internal sealed class DelegatedAdministrationRepository(GovernanceDbContext context) : IDelegatedAdministrationRepository
{
    public async Task<IReadOnlyList<DelegatedAdministration>> ListAsync(
        DelegationScope? scope,
        bool? isActive,
        CancellationToken ct)
    {
        var query = context.DelegatedAdministrations.AsQueryable();

        if (scope.HasValue)
            query = query.Where(d => d.Scope == scope.Value);

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        return await query.OrderByDescending(d => d.GrantedAt).ToListAsync(ct);
    }

    public async Task<DelegatedAdministration?> GetByIdAsync(DelegatedAdministrationId id, CancellationToken ct)
        => await context.DelegatedAdministrations.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<DelegatedAdministration>> ListByGranteeAsync(
        string granteeUserId,
        CancellationToken ct)
        => await context.DelegatedAdministrations
            .Where(d => d.GranteeUserId == granteeUserId && d.IsActive)
            .OrderByDescending(d => d.GrantedAt)
            .ToListAsync(ct);

    public async Task AddAsync(DelegatedAdministration delegation, CancellationToken ct)
        => await context.DelegatedAdministrations.AddAsync(delegation, ct);

    public Task UpdateAsync(DelegatedAdministration delegation, CancellationToken ct)
    {
        context.DelegatedAdministrations.Update(delegation);
        return Task.CompletedTask;
    }
}
