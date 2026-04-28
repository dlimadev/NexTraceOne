using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetAlertFiringHistoryFeature = NexTraceOne.IdentityAccess.Application.Features.GetAlertFiringHistory.GetAlertFiringHistory;
using ResolveAlertFeature = NexTraceOne.IdentityAccess.Application.Features.ResolveAlert.ResolveAlert;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// SaaS-08: Endpoints de gestão de alertas disparados.
/// </summary>
internal static class AlertsEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var alertsGroup = group.MapGroup("/alerts/firing");

        alertsGroup.MapGet("/", async (
            string? status,
            int days,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAlertFiringHistoryFeature.Query(status, days <= 0 ? 30 : days), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        alertsGroup.MapPost("/{recordId:guid}/resolve", async (
            Guid recordId,
            ResolveAlertFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var cmd = command with { RecordId = recordId };
            var result = await sender.Send(cmd, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");
    }
}
