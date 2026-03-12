using Microsoft.AspNetCore.Authorization;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Handler de autorização que avalia se o usuário autenticado possui
/// a permissão exigida pela policy do endpoint.
///
/// As permissões são lidas da claim "permissions" do JWT pelo <see cref="ICurrentUser"/>.
/// Garante que o sistema rejeita por padrão (deny by default):
/// se o usuário não tiver a permissão, o acesso é negado mesmo que esteja autenticado.
/// </summary>
public sealed class PermissionAuthorizationHandler(ICurrentUser currentUser)
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

        return Task.CompletedTask;
    }
}
