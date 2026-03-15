using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetFinOpsSummaryFeature = NexTraceOne.Governance.Application.Features.GetFinOpsSummary.GetFinOpsSummary;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de FinOps do módulo Governance.
/// Disponibiliza resumo de custo operacional contextual.
/// </summary>
public sealed class GovernanceFinOpsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/finops");

        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            string? serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFinOpsSummaryFeature.Query(teamId, domainId, serviceId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");
    }
}
