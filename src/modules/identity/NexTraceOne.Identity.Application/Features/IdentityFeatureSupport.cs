using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Features;

/// <summary>
/// Utilitários internos compartilhados entre features do módulo Identity.
/// </summary>
internal static class IdentityFeatureSupport
{
    /// <summary>Resolve o vínculo ativo do usuário com base no tenant atual ou no primeiro vínculo disponível.</summary>
    public static async Task<TenantMembership?> ResolveMembershipAsync(
        ICurrentTenant currentTenant,
        ITenantMembershipRepository membershipRepository,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (currentTenant.Id != Guid.Empty)
        {
            var currentMembership = await membershipRepository.GetByUserAndTenantAsync(
                userId,
                TenantId.From(currentTenant.Id),
                cancellationToken);

            if (currentMembership is { IsActive: true })
            {
                return currentMembership;
            }
        }

        return (await membershipRepository.ListByUserAsync(userId, cancellationToken))
            .FirstOrDefault(membership => membership.IsActive);
    }

    /// <summary>Cria a resposta padronizada de autenticação para o módulo Identity.</summary>
    public static LocalLoginFeature.LoginResponse CreateLoginResponse(
        User user,
        TenantMembership membership,
        Role role,
        IJwtTokenGenerator jwtTokenGenerator,
        string refreshToken)
    {
        var permissions = Role.GetPermissionsForRole(role.Name);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user, membership, permissions);

        return new LocalLoginFeature.LoginResponse(
            accessToken,
            refreshToken,
            jwtTokenGenerator.AccessTokenLifetimeSeconds,
            new LocalLoginFeature.UserResponse(
                user.Id.Value,
                user.Email.Value,
                user.FullName.Value,
                membership.TenantId.Value,
                role.Name,
                permissions));
    }
}
