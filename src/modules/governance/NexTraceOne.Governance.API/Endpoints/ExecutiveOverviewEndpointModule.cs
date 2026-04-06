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
using ComputeDoraMetricsFeature = NexTraceOne.Governance.Application.Features.ComputeDoraMetrics.ComputeDoraMetrics;
using GetDoraMetricsTrendFeature = NexTraceOne.Governance.Application.Features.GetDoraMetricsTrend.GetDoraMetricsTrend;
using ComputeServiceScorecardFeature = NexTraceOne.Governance.Application.Features.ComputeServiceScorecard.ComputeServiceScorecard;
using ListServiceScorecardsFeature = NexTraceOne.Governance.Application.Features.ListServiceScorecards.ListServiceScorecards;

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

        // ── DORA Metrics ──────────────────────────────────────────────────────
        group.MapGet("/dora-metrics", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceName,
            string? teamName,
            int periodDays = 30,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new ComputeDoraMetricsFeature.Query(serviceName, teamName, periodDays), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:reports:read")
        .WithName("ComputeDoraMetrics")
        .WithSummary("Compute DORA metrics (Deployment Frequency, Lead Time, Change Failure Rate, MTTR)");

        group.MapGet("/dora-metrics/trend", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceName,
            int periodDays = 90,
            int bucketDays = 7,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new GetDoraMetricsTrendFeature.Query(periodDays, bucketDays, serviceName), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:reports:read")
        .WithName("GetDoraMetricsTrend")
        .WithSummary("Get DORA metrics trend over time as time-bucketed data points");

        // ── Service Scorecards ────────────────────────────────────────────────
        group.MapGet("/service-scorecards/{serviceName}", async (
            string serviceName,
            ISender sender,
            IErrorLocalizer localizer,
            int periodDays = 30,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new ComputeServiceScorecardFeature.Query(serviceName, periodDays), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:reports:read")
        .WithName("ComputeServiceScorecard")
        .WithSummary("Compute service scorecard across 8 maturity dimensions");

        group.MapGet("/service-scorecards", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? teamName,
            string? domain,
            string? maturityLevel,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new ListServiceScorecardsFeature.Query(teamName, domain, maturityLevel, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("governance:reports:read")
        .WithName("ListServiceScorecards")
        .WithSummary("List service scorecards for a team or domain, ordered by score");
    }
}
