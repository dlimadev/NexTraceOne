using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de mapeamentos SSO Group → Role.
/// </summary>
internal sealed class SsoGroupMappingRepository(IdentityDbContext dbContext) : ISsoGroupMappingRepository
{
    public async Task<SsoGroupMapping?> FindActiveByGroupsAsync(
        TenantId tenantId,
        string provider,
        IReadOnlyCollection<string> externalGroupIds,
        CancellationToken cancellationToken)
    {
        if (externalGroupIds.Count == 0)
            return null;

        return await dbContext.SsoGroupMappings
            .Where(m => m.TenantId == tenantId
                && m.Provider == provider
                && m.IsActive
                && externalGroupIds.Contains(m.ExternalGroupId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
