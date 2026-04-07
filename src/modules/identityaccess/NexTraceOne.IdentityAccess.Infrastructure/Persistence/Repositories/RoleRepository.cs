using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de papéis de autorização persistidos via EF Core.
/// </summary>
internal sealed class RoleRepository(IdentityDbContext context)
    : RepositoryBase<Role, RoleId>(context), IRoleRepository
{
    /// <inheritdoc />
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await context.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken)
        => await context.Roles.Where(x => x.IsSystem).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Roles.OrderBy(x => x.IsSystem).ThenBy(x => x.Name).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<RoleId, Role>> GetByIdsAsync(
        IReadOnlyCollection<RoleId> ids,
        CancellationToken cancellationToken)
    {
        var roles = await context.Roles
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return roles.ToDictionary(r => r.Id, r => r);
    }

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken)
        => await context.Roles.AddAsync(role, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(Role role, CancellationToken cancellationToken)
    {
        context.Roles.Remove(role);
        return Task.CompletedTask;
    }
}
