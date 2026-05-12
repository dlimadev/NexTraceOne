using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Application.Features;

/// <summary>
/// Implementação injetável da construção de respostas padronizadas de login
/// e resolução de memberships no módulo Identity.
///
/// Responsabilidade única: centralizar operações compartilhadas entre múltiplos
/// handlers de autenticação (LocalLogin, OidcCallback, FederatedLogin, RefreshToken),
/// eliminando duplicação sem acoplar handlers entre si.
///
/// SaaS-01: popula capabilities do plano do tenant no JWT para que HasCapability()
/// funcione correctamente em toda a plataforma.
/// </summary>
internal sealed class LoginResponseBuilder(
    ICurrentTenant currentTenant,
    ITenantMembershipRepository membershipRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPermissionResolver permissionResolver,
    ITenantLicenseRepository licenseRepository) : ILoginResponseBuilder
{
    /// <inheritdoc />
    public Guid CurrentTenantId => currentTenant.Id;

    /// <inheritdoc />
    public async Task<TenantMembership?> ResolveMembershipAsync(
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

    /// <inheritdoc />
    public async Task<LocalLoginFeature.LoginResponse> CreateLoginResponseAsync(
        User user,
        TenantMembership membership,
        Role role,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.Id != Guid.Empty
            ? TenantId.From(currentTenant.Id)
            : membership.TenantId;

        var permissions = await permissionResolver.ResolvePermissionsAsync(
            role.Id, role.Name, tenantId, cancellationToken);

        // SaaS-01: resolve capabilities from tenant license plan.
        // Falls back to Enterprise (all capabilities) when no license is provisioned,
        // preserving backward-compatibility for self-hosted deployments.
        var license = await licenseRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);
        var capabilities = license?.GetCapabilities() ?? TenantCapabilities.ForPlan(TenantPlan.Enterprise);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(
            user, tenantId, [membership.RoleId], [role.Name], capabilities);

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
