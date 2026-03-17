using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ClassifyChangeLevelFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel.ClassifyChangeLevel;
using CalculateBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius.CalculateBlastRadius;
using GetBlastRadiusReportFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using ComputeChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore.ComputeChangeScore;
using GetChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeScore.GetChangeScore;
using AttachWorkItemContextFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachWorkItemContext.AttachWorkItemContext;

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
        });

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
        });

        group.MapGet("/{releaseId:guid}/blast-radius", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetBlastRadiusReportFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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
        });

        group.MapGet("/{releaseId:guid}/score", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetChangeScoreFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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
        });
    }
}
