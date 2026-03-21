using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Implementação de IEnvironmentAccessValidator usando IEnvironmentRepository.
/// Valida que um ambiente pertence ao tenant e que o usuário tem acesso ativo.
/// </summary>
internal sealed class EnvironmentAccessValidator(
    IEnvironmentRepository environmentRepository) : IEnvironmentAccessValidator
{
    /// <inheritdoc />
    public async Task<Result<Unit>> ValidateAsync(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);

        if (environment is null)
            return Error.NotFound("Environment.NotFound", "Environment '{0}' not found.", environmentId.Value.ToString());

        if (environment.TenantId != tenantId)
            return Error.Forbidden("Environment.WrongTenant", "Environment does not belong to the active tenant.");

        if (!environment.IsActive)
            return Error.Forbidden("Environment.Inactive", "Environment '{0}' is not active.", environment.Name);

        var access = await environmentRepository.GetAccessAsync(userId, tenantId, environmentId, cancellationToken);

        if (access is null)
            return Error.Forbidden("Environment.AccessDenied", "User does not have access to environment '{0}'.", environment.Name);

        if (!access.IsActiveAt(now))
            return Error.Forbidden("Environment.AccessExpired", "Access to environment '{0}' has expired.", environment.Name);

        return Unit.Value;
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);

        if (environment is null || environment.TenantId != tenantId || !environment.IsActive)
            return false;

        var access = await environmentRepository.GetAccessAsync(userId, tenantId, environmentId, cancellationToken);
        return access is not null && access.IsActiveAt(now);
    }
}
