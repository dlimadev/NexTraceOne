using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de atribuições de papéis a usuários em tenants, persistido via EF Core.
///
/// Suporta o modelo multi-role: um usuário pode ter N papéis no mesmo tenant.
/// Consultas de atribuições ativas consideram:
/// - Flag IsActive
/// - Vigência temporal (ValidFrom/ValidUntil) quando informada.
/// </summary>
internal sealed class UserRoleAssignmentRepository(IdentityDbContext context)
    : RepositoryBase<UserRoleAssignment, UserRoleAssignmentId>(context), IUserRoleAssignmentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRoleAssignment>> GetActiveAssignmentsAsync(
        UserId userId,
        TenantId tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await context.UserRoleAssignments
            .Where(x => x.UserId == userId
                && x.TenantId == tenantId
                && x.IsActive
                && (!x.ValidFrom.HasValue || x.ValidFrom.Value <= now)
                && (!x.ValidUntil.HasValue || x.ValidUntil.Value > now))
            .OrderBy(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRoleAssignment>> ListByUserAsync(
        UserId userId,
        CancellationToken cancellationToken)
        => await context.UserRoleAssignments
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<UserRoleAssignment> Items, int TotalCount)> ListByTenantAsync(
        TenantId tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.UserRoleAssignments.Where(x => x.TenantId == tenantId);

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

            query = query.Where(x => userIds.Contains(x.UserId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        UserId userId,
        TenantId tenantId,
        RoleId roleId,
        CancellationToken cancellationToken)
        => await context.UserRoleAssignments
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.RoleId == roleId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<UserRoleAssignment?> GetByIdAsync(
        UserRoleAssignmentId id,
        CancellationToken cancellationToken)
        => await context.UserRoleAssignments
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRoleAssignment>> ListExpiredAssignmentsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await context.UserRoleAssignments
            .Where(x => x.IsActive && x.ValidUntil.HasValue && x.ValidUntil.Value <= now)
            .ToListAsync(cancellationToken);
}
