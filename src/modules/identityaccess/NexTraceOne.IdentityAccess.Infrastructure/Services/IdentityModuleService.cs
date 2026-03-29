using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.DTOs;
using NexTraceOne.IdentityAccess.Contracts.ServiceInterfaces;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação do contrato público IIdentityModule.
/// Serve como ponto de acesso para outros módulos consultarem dados de identidade
/// sem acoplar diretamente ao DbContext ou repositórios internos.
/// Utiliza IPermissionResolver para resolução DB-first com fallback estático.
/// </summary>
internal sealed class IdentityModuleService(
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    IPermissionResolver permissionResolver) : IIdentityModule
{
    /// <inheritdoc />
    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(UserId.From(userId), cancellationToken);
        return user is null
            ? null
            : new UserSummaryDto(user.Id.Value, user.Email.Value, user.FullName.Value, user.IsActive);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByUserAndTenantAsync(
            UserId.From(userId),
            TenantId.From(tenantId),
            cancellationToken);

        if (membership is null || !membership.IsActive)
        {
            return [];
        }

        var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
        if (role is null)
        {
            return [];
        }

        return await permissionResolver.ResolvePermissionsAsync(
            role.Id, role.Name, tenantId: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTenantMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByUserAndTenantAsync(
            UserId.From(userId),
            TenantId.From(tenantId),
            cancellationToken);

        return membership is { IsActive: true };
    }
}
