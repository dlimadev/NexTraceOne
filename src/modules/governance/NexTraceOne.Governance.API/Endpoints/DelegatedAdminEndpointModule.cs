using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListDelegatedAdministrationsFeature = NexTraceOne.Governance.Application.Features.ListDelegatedAdministrations.ListDelegatedAdministrations;
using CreateDelegatedAdministrationFeature = NexTraceOne.Governance.Application.Features.CreateDelegatedAdministration.CreateDelegatedAdministration;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de administração delegada no módulo Governance.
/// Permite listar e criar delegações de administração para equipas e domínios.
/// Suporta governança descentralizada com controlo de expiração e auditoria.
/// </summary>
public sealed class DelegatedAdminEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/delegations");

        // ── Listar delegações de administração ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListDelegatedAdministrationsFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");

        // ── Criar nova delegação de administração ──
        group.MapPost("/", async (
            CreateDelegatedAdministrationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            if (result.IsSuccess)
                return Results.Created($"/api/v1/admin/delegations/{result.Value.DelegationId}", result.Value);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:write");
    }
}
