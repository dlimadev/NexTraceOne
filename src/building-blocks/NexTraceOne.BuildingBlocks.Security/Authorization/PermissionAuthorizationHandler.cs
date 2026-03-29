using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Handler de autorização que avalia se o usuário autenticado possui
/// a permissão exigida pela policy do endpoint.
///
/// As permissões são lidas da claim "permissions" do JWT pelo <see cref="ICurrentUser"/>.
/// Quando a permissão não consta no JWT, verifica se existe um grant JIT (Just-In-Time)
/// ativo via <see cref="IJitPermissionProvider"/> (opcional — o sistema funciona sem JIT registado).
///
/// Garante que o sistema rejeita por padrão (deny by default):
/// se o usuário não tiver a permissão, o acesso é negado mesmo que esteja autenticado.
///
/// Segurança: registra em log decisões de autorização negadas para fins de auditoria
/// e detecção de tentativas de acesso não autorizado (SIEM, correlação de eventos).
/// </summary>
public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    ILogger<PermissionAuthorizationHandler> logger,
    IJitPermissionProvider? jitPermissionProvider = null)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
            return;

        if (currentUser.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // Fallback: verificar se existe um grant JIT (Just-In-Time) ativo para esta permissão.
        if (jitPermissionProvider is not null)
        {
            var hasJitGrant = await jitPermissionProvider.HasActiveJitGrantAsync(
                currentUser.Id,
                requirement.Permission,
                context.Resource is Microsoft.AspNetCore.Http.HttpContext httpContext
                    ? httpContext.RequestAborted
                    : CancellationToken.None);

            if (hasJitGrant)
            {
                logger.LogInformation(
                    "Authorization granted via JIT for user {UserId}: permission '{Permission}'",
                    currentUser.Id,
                    requirement.Permission);
                context.Succeed(requirement);
                return;
            }
        }

        // Registrar negação para auditoria de segurança — sem expor dados pessoais no log.
        // O userId é um identificador interno, não dado pessoal sensível.
        logger.LogWarning(
            "Authorization denied for user {UserId}: missing permission '{Permission}'",
            currentUser.Id,
            requirement.Permission);
    }
}
