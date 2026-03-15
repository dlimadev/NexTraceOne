using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetControlsSummaryFeature = NexTraceOne.Governance.Application.Features.GetControlsSummary.GetControlsSummary;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Enterprise Controls — resumo de controlos enterprise por dimensão.
/// Agrega indicadores de cobertura, maturidade e gaps por área de controle.
/// </summary>
public sealed class EnterpriseControlsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/controls");

        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            string? serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetControlsSummaryFeature.Query(teamId, domainId, serviceId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:controls:read");
    }
}
