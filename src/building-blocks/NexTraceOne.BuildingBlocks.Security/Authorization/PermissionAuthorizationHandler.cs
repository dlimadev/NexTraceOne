using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Handler de autorização que avalia se o usuário autenticado possui
/// a permissão exigida pela policy do endpoint.
///
/// Estratégia de resolução (cascata, primeira correspondência ganha):
/// 1. Claims JWT ("permissions") — via <see cref="ICurrentUser.HasPermission"/>.
/// 2. Mapeamentos persistidos em base de dados — via <see cref="IDatabasePermissionProvider"/>
///    (permite personalização por tenant sem redeploy).
/// 3. Grants JIT (Just-In-Time) — via <see cref="IJitPermissionProvider"/>
///    (permissões temporárias aprovadas em workflow).
///
/// Garante deny-by-default: se nenhuma fonte conceder a permissão, o acesso é negado.
///
/// Segurança: registra em log decisões de autorização negadas para fins de auditoria
/// e detecção de tentativas de acesso não autorizado (SIEM, correlação de eventos).
/// </summary>
public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    ILogger<PermissionAuthorizationHandler> logger,
    IDatabasePermissionProvider? databasePermissionProvider = null,
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

        // 1. Verificação primária: claims JWT (permissões embebidas no token).
        if (currentUser.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;

        // 2. Verificação secundária: mapeamentos papel→permissão persistidos no banco de dados.
        // Permite customização por tenant sem necessidade de re-emitir JWT.
        if (databasePermissionProvider is not null)
        {
            var roleId = httpContext?.User?.FindFirst("role_id")?.Value ?? string.Empty;
            var tenantId = httpContext?.User?.FindFirst("tenant_id")?.Value ?? string.Empty;

            var hasDbPermission = await databasePermissionProvider.HasPermissionAsync(
                currentUser.Id,
                roleId,
                tenantId,
                requirement.Permission,
                cancellationToken);

            if (hasDbPermission)
            {
                logger.LogInformation(
                    "Authorization granted via database permission for user {UserId}: permission '{Permission}'",
                    currentUser.Id,
                    requirement.Permission);
                context.Succeed(requirement);
                return;
            }
        }

        // 3. Fallback: verificar se existe um grant JIT (Just-In-Time) ativo para esta permissão.
        if (jitPermissionProvider is not null)
        {
            var hasJitGrant = await jitPermissionProvider.HasActiveJitGrantAsync(
                currentUser.Id,
                requirement.Permission,
                cancellationToken);

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

        // Registrar negação com contexto de requisição para auditoria de segurança e investigação SOC.
        if (httpContext is not null)
        {
            logger.LogWarning(
                "Authorization denied for user {UserId}: " +
                "missing permission '{Permission}' for {HttpMethod} {RequestPath}",
                currentUser.Id,
                requirement.Permission,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                "Authorization denied for user {UserId}: missing permission '{Permission}'",
                currentUser.Id,
                requirement.Permission);
        }
    }
}
