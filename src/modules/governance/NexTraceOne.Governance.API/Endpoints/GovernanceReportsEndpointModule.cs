using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetReportsSummaryFeature = NexTraceOne.Governance.Application.Features.GetReportsSummary.GetReportsSummary;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Reports do módulo Governance.
/// Disponibiliza resumos executivos e relatórios por persona.
/// </summary>
public sealed class GovernanceReportsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports");

        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            string? persona,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReportsSummaryFeature.Query(teamId, domainId, persona);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }
}
