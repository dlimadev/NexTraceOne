using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetExecutiveOverviewFeature = NexTraceOne.Governance.Application.Features.GetExecutiveOverview.GetExecutiveOverview;
using GetRiskHeatmapFeature = NexTraceOne.Governance.Application.Features.GetRiskHeatmap.GetRiskHeatmap;
using GetMaturityScorecardsFeature = NexTraceOne.Governance.Application.Features.GetMaturityScorecards.GetMaturityScorecards;
using GetBenchmarkingFeature = NexTraceOne.Governance.Application.Features.GetBenchmarking.GetBenchmarking;
using GetExecutiveTrendsFeature = NexTraceOne.Governance.Application.Features.GetExecutiveTrends.GetExecutiveTrends;
using GetExecutiveDrillDownFeature = NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown.GetExecutiveDrillDown;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Executive Governance do módulo Governance.
/// Disponibiliza visão executiva expandida, heatmaps de risco, scorecards de maturidade,
/// benchmarking, tendências e drill-downs.
/// </summary>
public sealed class ExecutiveOverviewEndpointModule
{
    /// <summary>Registra endpoints de governança executiva no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/executive");

        group.MapGet("/overview", async (
            string? domainId,
            string? teamId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExecutiveOverviewFeature.Query(domainId, teamId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        group.MapGet("/risk/heatmap", async (
            string? dimension,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRiskHeatmapFeature.Query(dimension);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:risk:read");

        group.MapGet("/maturity/scorecards", async (
            string? dimension,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMaturityScorecardsFeature.Query(dimension);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        group.MapGet("/benchmarking/{dimension}", async (
            string dimension,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBenchmarkingFeature.Query(dimension);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        group.MapGet("/trends/{category}", async (
            string category,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExecutiveTrendsFeature.Query(category);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        group.MapGet("/{entityType}/{entityId}", async (
            string entityType,
            string entityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExecutiveDrillDownFeature.Query(entityType, entityId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }
}
