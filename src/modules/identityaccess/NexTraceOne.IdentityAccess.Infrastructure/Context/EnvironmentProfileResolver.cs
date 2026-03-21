using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Implementação de IEnvironmentProfileResolver usando IEnvironmentRepository.
/// Resolve o perfil operacional de um ambiente validando o isolamento por tenant.
/// </summary>
internal sealed class EnvironmentProfileResolver(
    IEnvironmentRepository environmentRepository) : IEnvironmentProfileResolver
{
    /// <inheritdoc />
    public async Task<EnvironmentProfile?> ResolveProfileAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);

        if (environment is null || environment.TenantId != tenantId || !environment.IsActive)
            return null;

        return environment.Profile;
    }

    /// <inheritdoc />
    public async Task<bool> IsProductionLikeAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);

        if (environment is null || environment.TenantId != tenantId || !environment.IsActive)
            return false;

        return environment.IsProductionLike;
    }
}
