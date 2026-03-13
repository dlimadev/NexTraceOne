using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Handler de autorização que avalia se o usuário autenticado possui
/// a permissão exigida pela policy do endpoint.
///
/// As permissões são lidas da claim "permissions" do JWT pelo <see cref="ICurrentUser"/>.
/// Garante que o sistema rejeita por padrão (deny by default):
/// se o usuário não tiver a permissão, o acesso é negado mesmo que esteja autenticado.
///
/// Segurança: registra em log decisões de autorização negadas para fins de auditoria
/// e detecção de tentativas de acesso não autorizado (SIEM, correlação de eventos).
/// </summary>
public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (currentUser.IsAuthenticated && currentUser.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else if (currentUser.IsAuthenticated)
        {
            // Registrar negação para auditoria de segurança — sem expor dados pessoais no log.
            // O userId é um identificador interno, não dado pessoal sensível.
            logger.LogWarning(
                "Authorization denied for user {UserId}: missing permission '{Permission}'",
                currentUser.Id,
                requirement.Permission);
        }

        return Task.CompletedTask;
    }
}
