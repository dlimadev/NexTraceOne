using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Contracts.DTOs;
using NexTraceOne.Identity.Contracts.ServiceInterfaces;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Services;

/// <summary>
/// Implementação do contrato público IIdentityModule.
/// Serve como ponto de acesso para outros módulos consultarem dados de identidade
/// sem acoplar diretamente ao DbContext ou repositórios internos.
/// </summary>
internal sealed class IdentityModuleService(
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository) : IIdentityModule
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
        return role is null ? [] : Role.GetPermissionsForRole(role.Name);
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
