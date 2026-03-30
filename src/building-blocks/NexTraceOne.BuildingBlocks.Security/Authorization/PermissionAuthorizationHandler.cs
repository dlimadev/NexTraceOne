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
/// 2. Mapeamentos planos papel→permissão persistidos no banco — via <see cref="IDatabasePermissionProvider"/>
///    (permite personalização por tenant sem redeploy).
/// 3. Políticas de acesso módulo/página/ação persistidas no banco — via <see cref="IModuleAccessPermissionProvider"/>
///    (modelo enterprise granular com wildcard e deny explícito).
/// 4. Grants JIT (Just-In-Time) — via <see cref="IJitPermissionProvider"/>
///    (permissões temporárias aprovadas em workflow).
///
/// Garante deny-by-default: se nenhuma fonte conceder a permissão, o acesso é negado.
/// Deny explícito: se o step 3 retornar <c>false</c>, o acesso é imediatamente negado
/// (override de qualquer concessão anterior que não tenha sido avaliada).
///
/// Segurança: registra em log decisões de autorização negadas para fins de auditoria
/// e detecção de tentativas de acesso não autorizado (SIEM, correlação de eventos).
/// </summary>
public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    ILogger<PermissionAuthorizationHandler> logger,
    IDatabasePermissionProvider? databasePermissionProvider = null,
    IModuleAccessPermissionProvider? moduleAccessPermissionProvider = null,
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

        var tenantId = httpContext?.User?.FindFirst("tenant_id")?.Value ?? string.Empty;

        // Multi-role: iterar sobre todos os role_ids emitidos no JWT.
        var roleIdClaims = (httpContext?.User?.FindAll("role_ids")
            ?.Select(c => c.Value)
            .ToList()) ?? [];

        // Backward-compatible: se não houver role_ids, usar role_id (legado).
        if (roleIdClaims.Count == 0)
        {
            var legacyRoleId = httpContext?.User?.FindFirst("role_id")?.Value ?? string.Empty;
            roleIdClaims = [legacyRoleId];
        }

        // 2. Verificação secundária: mapeamentos planos papel→permissão persistidos no banco.
        // Permite customização por tenant sem necessidade de re-emitir JWT.
        // Suporta multi-role: verifica todos os role_ids do JWT (v1.4).
        if (databasePermissionProvider is not null)
        {
            foreach (var roleId in roleIdClaims)
            {
                var hasDbPermission = await databasePermissionProvider.HasPermissionAsync(
                    currentUser.Id,
                    roleId,
                    tenantId,
                    requirement.Permission,
                    cancellationToken);

                if (hasDbPermission)
                {
                    logger.LogInformation(
                        "Authorization granted via database permission for user {UserId} role {RoleId}: permission '{Permission}'",
                        currentUser.Id,
                        roleId,
                        requirement.Permission);
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        // 3. Verificação terciária: políticas de acesso módulo/página/ação persistidas.
        // O provider traduz o permission code plano para o triplo (Module, Page, Action)
        // e consulta a tabela iam_module_access_policies com prioridade tenant > sistema.
        // Suporta deny explícito: IsAllowed=false bloqueia acesso imediatamente.
        if (moduleAccessPermissionProvider is not null)
        {
            foreach (var roleId in roleIdClaims)
            {
                var moduleAccessResult = await moduleAccessPermissionProvider.HasModuleAccessAsync(
                    currentUser.Id,
                    roleId,
                    tenantId,
                    requirement.Permission,
                    cancellationToken);

                if (moduleAccessResult == true)
                {
                    logger.LogInformation(
                        "Authorization granted via module access policy for user {UserId} role {RoleId}: permission '{Permission}'",
                        currentUser.Id,
                        roleId,
                        requirement.Permission);
                    context.Succeed(requirement);
                    return;
                }

                // Deny explícito: uma política activa nega acesso. Bloquear imediatamente.
                if (moduleAccessResult == false)
                {
                    logger.LogWarning(
                        "Authorization explicitly denied via module access policy for user {UserId} role {RoleId}: permission '{Permission}'",
                        currentUser.Id,
                        roleId,
                        requirement.Permission);
                    return;
                }
            }
        }

        // 4. Fallback: verificar se existe um grant JIT (Just-In-Time) ativo para esta permissão.
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
