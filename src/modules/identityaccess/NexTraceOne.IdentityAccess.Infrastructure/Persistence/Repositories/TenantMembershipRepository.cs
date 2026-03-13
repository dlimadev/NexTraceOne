using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de vínculos de usuário com tenants persistidos via EF Core.
/// Suporta paginação e busca para listagem administrativa.
/// </summary>
internal sealed class TenantMembershipRepository(IdentityDbContext context)
    : RepositoryBase<TenantMembership, TenantMembershipId>(context), ITenantMembershipRepository
{
    /// <inheritdoc />
    public async Task<TenantMembership?> GetByUserAndTenantAsync(UserId userId, TenantId tenantId, CancellationToken cancellationToken)
        => await context.TenantMemberships.SingleOrDefaultAsync(
            x => x.UserId == userId && x.TenantId == tenantId,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken)
        => await context.TenantMemberships
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<TenantMembership> Items, int TotalCount)> ListByTenantAsync(
        TenantId tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var membershipQuery = context.TenantMemberships.Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            var userIds = await context.Users
                .AsNoTracking()
                .Where(x => x.Email.Value.Contains(normalizedSearch)
                    || x.FullName.FirstName.ToLower().Contains(normalizedSearch)
                    || x.FullName.LastName.ToLower().Contains(normalizedSearch))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            membershipQuery = membershipQuery.Where(x => userIds.Contains(x.UserId));
        }

        var totalCount = await membershipQuery.CountAsync(cancellationToken);
        var items = await membershipQuery
            .OrderBy(x => x.JoinedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantMembership>> ListAllActiveByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await context.TenantMemberships
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.UserId)
            .ToListAsync(cancellationToken);
}
