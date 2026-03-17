using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateDelegationFeature = NexTraceOne.IdentityAccess.Application.Features.CreateDelegation.CreateDelegation;
using RevokeDelegationFeature = NexTraceOne.IdentityAccess.Application.Features.RevokeDelegation.RevokeDelegation;
using ListDelegationsFeature = NexTraceOne.IdentityAccess.Application.Features.ListDelegations.ListDelegations;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de delegação formal de permissões — funcionalidade enterprise v1.1.
/// Permite criar delegações, revogar delegações existentes e listar
/// todas as delegações ativas para auditoria e gestão administrativa.
/// </summary>
internal static class DelegationEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de delegação no subgrupo <c>/delegations</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var delGroup = group.MapGroup("/delegations");

        // Qualquer usuário autenticado pode criar delegação — o handler valida que o
        // delegante possui as permissões que deseja delegar (ScopeExceedsGrantor).
        delGroup.MapPost("/", async (
            CreateDelegationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        delGroup.MapPost("/{delegationId:guid}/revoke", async (
            Guid delegationId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RevokeDelegationFeature.Command(delegationId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:revoke");

        delGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListDelegationsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");
    }
}
