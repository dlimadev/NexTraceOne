using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetAlertFiringHistoryFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetAlertFiringHistory.GetAlertFiringHistory;
using ResolveAlertFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ResolveAlert.ResolveAlert;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de alertas operacionais disparados.
/// </summary>
public sealed class AlertFiringEndpointModule
{
    /// <summary>Mapeia os endpoints de alertas no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alerts/firing");

        group.MapGet("/", async (
            string? status,
            int days,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAlertFiringHistoryFeature.Query(status, days <= 0 ? 30 : days), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:incidents:read");

        group.MapPost("/{recordId:guid}/resolve", async (
            Guid recordId,
            ResolveAlertFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var cmd = command with { RecordId = recordId };
            var result = await sender.Send(cmd, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:incidents:write");
    }
}
