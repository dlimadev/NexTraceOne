using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetRiskSummaryFeature = NexTraceOne.Governance.Application.Features.GetRiskSummary.GetRiskSummary;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Risk do módulo Governance.
/// Disponibiliza resumo de risco operacional contextualizado.
/// </summary>
public sealed class GovernanceRiskEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/risk");

        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRiskSummaryFeature.Query(teamId, domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:risk:read");
    }
}
