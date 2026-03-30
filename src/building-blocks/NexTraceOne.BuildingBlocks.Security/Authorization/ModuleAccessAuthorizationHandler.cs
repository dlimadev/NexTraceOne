using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Handler de autorização para o modelo granular módulo/página/ação.
///
/// Avalia <see cref="ModuleAccessRequirement"/> consultando:
/// 1. <see cref="IModuleAccessPermissionProvider"/> — políticas persistidas na BD
///    com suporte a personalização por tenant e wildcard.
/// 2. Deny-by-default: se nenhuma fonte conceder acesso, o request é negado.
///
/// Usado pelos endpoints que adoptam <c>RequireModuleAccess("AI", "Runtime", "Write")</c>.
///
/// Segurança: decisões de negação são registadas em log para auditoria SOC/SIEM.
/// </summary>
public sealed class ModuleAccessAuthorizationHandler(
    ICurrentUser currentUser,
    ILogger<ModuleAccessAuthorizationHandler> logger,
    IModuleAccessPermissionProvider? moduleAccessProvider = null)
    : AuthorizationHandler<ModuleAccessRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAccessRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
            return;

        if (moduleAccessProvider is null)
        {
            logger.LogWarning(
                "ModuleAccessAuthorizationHandler: IModuleAccessPermissionProvider not registered. " +
                "Denying access for user {UserId} to {Module}/{Page}/{Action}.",
                currentUser.Id,
                requirement.Module,
                requirement.Page,
                requirement.Action);
            return;
        }

        var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;
        var tenantId = httpContext?.User?.FindFirst("tenant_id")?.Value ?? string.Empty;

        // Multi-role: verificar todos os role_ids do JWT.
        var roleIdClaims = (httpContext?.User?.FindAll("role_ids")
            ?.Select(c => c.Value)
            .ToList()) ?? [];

        // Backward-compatible: usar role_id legado se role_ids não presente.
        if (roleIdClaims.Count == 0)
        {
            var legacyRoleId = httpContext?.User?.FindFirst("role_id")?.Value ?? string.Empty;
            roleIdClaims = [legacyRoleId];
        }

        foreach (var roleId in roleIdClaims)
        {
            var result = await moduleAccessProvider.HasModuleAccessDirectAsync(
                currentUser.Id,
                roleId,
                tenantId,
                requirement.Module,
                requirement.Page,
                requirement.Action,
                cancellationToken);

            if (result == true)
            {
                logger.LogInformation(
                    "ModuleAccess granted for user {UserId} role {RoleId}: {Module}/{Page}/{Action}",
                    currentUser.Id,
                    roleId,
                    requirement.Module,
                    requirement.Page,
                    requirement.Action);
                context.Succeed(requirement);
                return;
            }

            // Deny explícito: se uma política nega, bloquear imediatamente.
            if (result == false)
            {
                logger.LogWarning(
                    "ModuleAccess explicitly denied for user {UserId} role {RoleId}: {Module}/{Page}/{Action}",
                    currentUser.Id,
                    roleId,
                    requirement.Module,
                    requirement.Page,
                    requirement.Action);
                return;
            }
        }

        // Nenhuma política concedeu acesso.
        if (httpContext is not null)
        {
            logger.LogWarning(
                "ModuleAccess denied for user {UserId}: no policy for {Module}/{Page}/{Action} on {HttpMethod} {RequestPath}",
                currentUser.Id,
                requirement.Module,
                requirement.Page,
                requirement.Action,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
    }
}
