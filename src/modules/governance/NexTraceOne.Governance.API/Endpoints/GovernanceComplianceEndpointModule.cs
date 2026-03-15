using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetComplianceSummaryFeature = NexTraceOne.Governance.Application.Features.GetComplianceSummary.GetComplianceSummary;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Compliance do módulo Governance.
/// Disponibiliza visão de compliance técnico-operacional.
/// </summary>
public sealed class GovernanceComplianceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/compliance");

        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetComplianceSummaryFeature.Query(teamId, domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:compliance:read");
    }
}
