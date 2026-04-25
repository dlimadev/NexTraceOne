using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ClassifyChangeLevelFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel.ClassifyChangeLevel;
using CalculateBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius.CalculateBlastRadius;
using GetBlastRadiusReportFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using ComputeChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore.ComputeChangeScore;
using GetChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeScore.GetChangeScore;
using AttachWorkItemContextFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachWorkItemContext.AttachWorkItemContext;
using GetPreProductionComparisonFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPreProductionComparison.GetPreProductionComparison;
using GetRiskScoreTrendFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRiskScoreTrend.GetRiskScoreTrend;
using EvaluateReleaseTrainFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluateReleaseTrain.EvaluateReleaseTrain;
using GetDeploymentRiskForecastReportFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentRiskForecastReport.GetDeploymentRiskForecastReport;
using GetPredictiveBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPredictiveBlastRadius.GetPredictiveBlastRadius;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de análise de inteligência de mudança.
/// Agrupa as operações de classificação de nível de mudança, cálculo de
/// blast radius, score de risco e associação de work items.
///
/// Estes endpoints representam o core analítico da plataforma NexTraceOne,
/// oferecendo visibilidade sobre o impacto e risco de cada release.
/// </summary>
internal static class AnalysisEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de análise no grupo raiz de releases.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPut("/{releaseId:guid}/classify", async (
            Guid releaseId,
            ClassifyChangeLevelFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapPost("/{releaseId:guid}/blast-radius", async (
            Guid releaseId,
            CalculateBlastRadiusFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapGet("/{releaseId:guid}/blast-radius", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetBlastRadiusReportFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        group.MapPost("/{releaseId:guid}/score", async (
            Guid releaseId,
            ComputeChangeScoreFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapGet("/{releaseId:guid}/score", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetChangeScoreFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        group.MapPut("/{releaseId:guid}/workitem", async (
            Guid releaseId,
            AttachWorkItemContextFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── GET /api/v1/changes/{preProdReleaseId}/pre-prod-comparison — Comparação pré-produção ──
        group.MapGet("/{preProdReleaseId:guid}/pre-prod-comparison", async (
            Guid preProdReleaseId,
            Guid productionReleaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPreProductionComparisonFeature.Query(preProdReleaseId, productionReleaseId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("change-intelligence:read")
        .WithName("GetPreProductionComparison")
        .WithSummary("Compare pre-production baseline metrics against production baseline before promoting a release");

        // ── GET /api/v1/releases/risk-trend — Tendência de risk score por serviço (Gap 12) ───────
        group.MapGet("/risk-trend", async (
            string serviceName,
            string? environment,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRiskScoreTrendFeature.Query(serviceName, environment, limit),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("change-intelligence:read")
        .WithName("GetRiskScoreTrend")
        .WithSummary("Returns the time-series of risk scores for a given service, enabling trend visualisation");

        // ── POST /api/v1/releases/train-evaluation — Avaliação de Release Train (Gap 1) ──────────
        group.MapPost("/train-evaluation", async (
            EvaluateReleaseTrainFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("change-intelligence:read")
        .WithName("EvaluateReleaseTrain")
        .WithSummary("Evaluates a multi-service Release Train: aggregates risk scores, blast radius and readiness signal");

        // ── GET /api/v1/changes/releases/{releaseId}/risk-forecast — Deployment Risk Forecast ─────
        group.MapGet("/{releaseId:guid}/risk-forecast", async (
            Guid releaseId,
            Guid tenantId,
            int? maxTopPendingReleases,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDeploymentRiskForecastReportFeature.Query(
                ReleaseId: releaseId,
                TenantId: tenantId,
                MaxTopPendingReleases: maxTopPendingReleases ?? 10);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("change-intelligence:read")
        .WithName("GetDeploymentRiskForecastReport")
        .WithSummary("Returns predictive risk forecast for a release, combining historical rollback rate, environment instability, service risk profile and change confidence");

        // ── CC-07: Predictive Blast Radius v2 — ProbabilityOfRegression ──
        group.MapGet("/{releaseId:guid}/blast-radius/predictive", async (
            Guid releaseId,
            int? historicalLookbackDays,
            double? minCallFrequency,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPredictiveBlastRadiusFeature.Query(
                    releaseId,
                    historicalLookbackDays ?? 90,
                    minCallFrequency ?? 10.0),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("change-intelligence:read")
        .WithName("GetPredictiveBlastRadius")
        .WithSummary("CC-07: Returns blast radius enriched with ProbabilityOfRegression per consumer");
    }
}
