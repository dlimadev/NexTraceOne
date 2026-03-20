using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Authorization;

/// <summary>
/// Handler de autorização que valida se o usuário tem acesso ao ambiente ativo da requisição.
///
/// Avalia EnvironmentAccessRequirement verificando:
/// - O usuário está autenticado
/// - O ambiente está resolvido na requisição (via EnvironmentContextAccessor)
/// - O usuário tem acesso ao ambiente (via IEnvironmentRepository)
///
/// Segurança deny-by-default: se qualquer verificação falhar, o acesso é negado.
/// </summary>
internal sealed class EnvironmentAccessAuthorizationHandler(
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IEnvironmentContextAccessor environmentContextAccessor,
    IEnvironmentAccessValidator environmentAccessValidator,
    IDateTimeProvider dateTimeProvider,
    ILogger<EnvironmentAccessAuthorizationHandler> logger)
    : AuthorizationHandler<EnvironmentAccessRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EnvironmentAccessRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
            return;

        if (!environmentContextAccessor.IsResolved)
        {
            logger.LogWarning(
                "EnvironmentAccess denied for user {UserId}: no environment context resolved",
                currentUser.Id);
            return;
        }

        if (currentTenant.Id == Guid.Empty)
        {
            logger.LogWarning(
                "EnvironmentAccess denied for user {UserId}: no tenant context",
                currentUser.Id);
            return;
        }

        var userId = new UserId(Guid.Parse(currentUser.Id));
        var tenantId = new TenantId(currentTenant.Id);
        var environmentId = environmentContextAccessor.EnvironmentId;

        var hasAccess = await environmentAccessValidator.HasAccessAsync(
            userId, tenantId, environmentId, dateTimeProvider.UtcNow);

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning(
                "EnvironmentAccess denied for user {UserId} to environment {EnvironmentId} in tenant {TenantId}",
                currentUser.Id,
                environmentId.Value,
                currentTenant.Id);
        }
    }
}
