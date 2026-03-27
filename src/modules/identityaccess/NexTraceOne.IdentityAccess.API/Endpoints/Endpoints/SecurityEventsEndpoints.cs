using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListSecurityEventsFeature = NexTraceOne.IdentityAccess.Application.Features.ListSecurityEvents.ListSecurityEvents;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de consulta de eventos críticos de segurança do módulo Identity.
/// </summary>
internal static class SecurityEventsEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/security-events", async (
            string? eventType,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListSecurityEventsFeature.Query(eventType, page ?? 1, pageSize ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }
}
