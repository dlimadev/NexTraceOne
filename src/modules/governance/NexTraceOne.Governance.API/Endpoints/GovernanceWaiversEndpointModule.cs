using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListWaiversFeature = NexTraceOne.Governance.Application.Features.ListGovernanceWaivers.ListGovernanceWaivers;
using CreateWaiverFeature = NexTraceOne.Governance.Application.Features.CreateGovernanceWaiver.CreateGovernanceWaiver;
using ApproveWaiverFeature = NexTraceOne.Governance.Application.Features.ApproveGovernanceWaiver.ApproveGovernanceWaiver;
using RejectWaiverFeature = NexTraceOne.Governance.Application.Features.RejectGovernanceWaiver.RejectGovernanceWaiver;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Governance Waivers — gestão de exceções (waivers) a regras de governança.
/// Disponibiliza criação, listagem, aprovação e rejeição de waivers.
/// </summary>
public sealed class GovernanceWaiversEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/waivers");

        // ── Listar governance waivers ──
        group.MapGet("/", async (
            string? packId,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListWaiversFeature.Query(packId, status);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:waivers:read");

        // ── Criar novo waiver de governança ──
        group.MapPost("/", async (
            CreateWaiverFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/governance/waivers/{0}", localizer);
        }).RequirePermission("governance:waivers:write");

        // ── Aprovar waiver de governança ──
        group.MapPost("/{waiverId}/approve", async (
            string waiverId,
            ApproveWaiverFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { WaiverId = waiverId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:waivers:write");

        // ── Rejeitar waiver de governança ──
        group.MapPost("/{waiverId}/reject", async (
            string waiverId,
            RejectWaiverFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { WaiverId = waiverId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:waivers:write");
    }
}
