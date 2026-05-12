using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.IdentityAccess.Application.Features.CreateEnvironmentAccessPolicy;
using NexTraceOne.IdentityAccess.Application.Features.DeleteEnvironmentAccessPolicy;
using NexTraceOne.IdentityAccess.Application.Features.GetEnvironmentAccessPolicies;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints Minimal API de políticas de acesso por ambiente.
/// W5-05: Fine-Grained Auth per Environment.
///
/// Endpoints disponíveis:
/// - GET    /admin/environment-policies          → Listar políticas do tenant
/// - POST   /admin/environment-policies          → Criar nova política
/// - DELETE /admin/environment-policies/{id}     → Desactivar política
/// </summary>
internal static class EnvironmentPolicyEndpoints
{
    internal static void Map(IEndpointRouteBuilder group)
    {
        var g = group.MapGroup("/admin/environment-policies");

        g.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEnvironmentAccessPolicies.Query(), ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("iam:admin");

        g.MapPost("/", async (
            CreateEnvironmentAccessPolicy.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/identity/admin/environment-policies/{r.PolicyId}", localizer);
        })
        .RequirePermission("iam:admin");

        g.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteEnvironmentAccessPolicy.Command(id), ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("iam:admin");
    }
}
