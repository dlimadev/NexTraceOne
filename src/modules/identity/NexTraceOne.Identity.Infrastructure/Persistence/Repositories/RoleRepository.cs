using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

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
}
