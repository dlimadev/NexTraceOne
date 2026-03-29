using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IJitPermissionProvider"/> que delega para o repositório JIT do módulo Identity.
/// Converte o userId string (proveniente do JWT claim) para o tipo forte <see cref="UserId"/>
/// e consulta grants activos via <see cref="IJitAccessRepository"/>.
/// </summary>
internal sealed class JitPermissionProvider(
    IJitAccessRepository jitRepository,
    IDateTimeProvider dateTimeProvider) : IJitPermissionProvider
{
    public async Task<bool> HasActiveJitGrantAsync(
        string userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var guid))
            return false;

        var typedUserId = UserId.From(guid);
        var now = dateTimeProvider.UtcNow;

        return await jitRepository.HasActiveGrantAsync(typedUserId, permissionCode, now, cancellationToken);
    }
}
