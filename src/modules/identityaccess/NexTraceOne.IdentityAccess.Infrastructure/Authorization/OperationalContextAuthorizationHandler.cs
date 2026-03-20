using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Authorization;

/// <summary>
/// Handler de autorização que valida se o contexto operacional completo está disponível.
/// Exige que tenant + ambiente + usuário autenticado estejam todos resolvidos.
///
/// Usado em endpoints que precisam garantir que toda operação tem contexto completo,
/// como endpoints de observabilidade, incidentes, IA e análise de mudanças.
/// </summary>
internal sealed class OperationalContextAuthorizationHandler(
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IEnvironmentContextAccessor environmentContextAccessor,
    ILogger<OperationalContextAuthorizationHandler> logger)
    : AuthorizationHandler<OperationalContextRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationalContextRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
            return Task.CompletedTask;

        if (currentTenant.Id == Guid.Empty || !currentTenant.IsActive)
        {
            logger.LogWarning(
                "OperationalContext denied for user {UserId}: invalid or inactive tenant",
                currentUser.Id);
            return Task.CompletedTask;
        }

        if (!environmentContextAccessor.IsResolved)
        {
            logger.LogWarning(
                "OperationalContext denied for user {UserId}: environment context not resolved. " +
                "Ensure X-Environment-Id header is provided.",
                currentUser.Id);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
