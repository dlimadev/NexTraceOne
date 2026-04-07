using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using FourEyesFeature = NexTraceOne.Governance.Application.Features.EvaluateFourEyesPrinciple.EvaluateFourEyesPrinciple;
using CabFeature = NexTraceOne.Governance.Application.Features.EvaluateChangeAdvisoryBoard.EvaluateChangeAdvisoryBoard;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de governança avançada — Four Eyes Principle e Change Advisory Board.
/// Implementam gates de governança configuráveis via parametrização.
/// </summary>
public sealed class GovernanceGatesEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/gates");

        // Four Eyes Principle evaluation
        group.MapGet("/four-eyes", async (
            string actionCode,
            string requestedBy,
            string? approvedBy,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new FourEyesFeature.Query(actionCode, requestedBy, approvedBy);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");

        // Change Advisory Board evaluation
        group.MapGet("/cab", async (
            string serviceName,
            string environment,
            string criticality,
            string blastRadius,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new CabFeature.Query(serviceName, environment, criticality, blastRadius);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");
    }
}
