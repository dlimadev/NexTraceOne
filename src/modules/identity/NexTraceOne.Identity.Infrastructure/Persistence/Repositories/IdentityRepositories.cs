using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext context)
    : RepositoryBase<User, UserId>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
        // Usa .Value para garantir tradução correta para SQL via value conversion do EF Core
        => await context.Users.SingleOrDefaultAsync(x => x.Email.Value == email.Value, cancellationToken);

    public async Task<User?> GetByFederatedIdentityAsync(string provider, string externalId, CancellationToken cancellationToken)
        => await context.Users.SingleOrDefaultAsync(
            x => x.FederationProvider == provider && x.ExternalId == externalId,
            cancellationToken);

    public Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken)
        // Usa .Value para garantir tradução correta para SQL via value conversion do EF Core
        => context.Users.AnyAsync(x => x.Email.Value == email.Value, cancellationToken);

    public async Task<IReadOnlyDictionary<UserId, User>> GetByIdsAsync(IReadOnlyCollection<UserId> ids, CancellationToken cancellationToken)
        => await context.Users
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
}

internal sealed class SessionRepository(IdentityDbContext context)
    : RepositoryBase<Session, SessionId>(context), ISessionRepository
{
    public async Task<Session?> GetByRefreshTokenHashAsync(RefreshTokenHash refreshTokenHash, CancellationToken cancellationToken)
        // Usa .Value para garantir tradução correta para SQL via value conversion do EF Core
        => await context.Sessions.SingleOrDefaultAsync(x => x.RefreshToken.Value == refreshTokenHash.Value, cancellationToken);

    public async Task<Session?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken)
        => await context.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
}

internal sealed class RoleRepository(IdentityDbContext context)
    : RepositoryBase<Role, RoleId>(context), IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await context.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken)
        => await context.Roles.Where(x => x.IsSystem).OrderBy(x => x.Name).ToListAsync(cancellationToken);
}

internal sealed class TenantMembershipRepository(IdentityDbContext context)
    : RepositoryBase<TenantMembership, TenantMembershipId>(context), ITenantMembershipRepository
{
    public async Task<TenantMembership?> GetByUserAndTenantAsync(UserId userId, TenantId tenantId, CancellationToken cancellationToken)
        => await context.TenantMemberships.SingleOrDefaultAsync(
            x => x.UserId == userId && x.TenantId == tenantId,
            cancellationToken);

    public async Task<IReadOnlyList<TenantMembership>> ListByUserAsync(UserId userId, CancellationToken cancellationToken)
        => await context.TenantMemberships
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);

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
            // FullName é OwnsOne: usa as propriedades mapeadas diretamente (FirstName e LastName)
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
}
