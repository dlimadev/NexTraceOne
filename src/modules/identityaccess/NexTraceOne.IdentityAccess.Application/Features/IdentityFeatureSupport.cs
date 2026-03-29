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
/// Decisão de design:
/// - Classe injetável via DI (Scoped) — compartilha contexto do request (ICurrentTenant).
/// - Dependências recebidas por construtor — respeita DIP, permite mock em testes.
///
/// Refatoração: migrado da classe estática IdentityFeatureSupport para serviço injetável,
/// aderindo ao Dependency Inversion Principle e facilitando testes unitários.
/// Utiliza IPermissionResolver para resolução DB-first com fallback estático.
/// </summary>
internal sealed class LoginResponseBuilder(
    ICurrentTenant currentTenant,
    ITenantMembershipRepository membershipRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPermissionResolver permissionResolver) : ILoginResponseBuilder
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
