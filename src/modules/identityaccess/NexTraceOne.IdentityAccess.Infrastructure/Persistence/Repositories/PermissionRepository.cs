using Microsoft.EntityFrameworkCore;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de permissões granulares persistidas via EF Core.
/// </summary>
internal sealed class PermissionRepository(IdentityDbContext context) : IPermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Permissions.OrderBy(x => x.Module).ThenBy(x => x.Code).ToListAsync(cancellationToken);
}
