using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using AttachWorkItemContextFeature = NexTraceOne.ChangeIntelligence.Application.Features.AttachWorkItemContext.AttachWorkItemContext;
using CalculateBlastRadiusFeature = NexTraceOne.ChangeIntelligence.Application.Features.CalculateBlastRadius.CalculateBlastRadius;
using ClassifyChangeLevelFeature = NexTraceOne.ChangeIntelligence.Application.Features.ClassifyChangeLevel.ClassifyChangeLevel;
using ComputeChangeScoreFeature = NexTraceOne.ChangeIntelligence.Application.Features.ComputeChangeScore.ComputeChangeScore;
using GetBlastRadiusReportFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using GetChangeScoreFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeScore.GetChangeScore;
using GetReleaseFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetRelease.GetRelease;
using GetReleaseHistoryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetReleaseHistory.GetReleaseHistory;
using ListReleasesFeature = NexTraceOne.ChangeIntelligence.Application.Features.ListReleases.ListReleases;
using NotifyDeploymentFeature = NexTraceOne.ChangeIntelligence.Application.Features.NotifyDeployment.NotifyDeployment;
using RegisterRollbackFeature = NexTraceOne.ChangeIntelligence.Application.Features.RegisterRollback.RegisterRollback;
using UpdateDeploymentStateFeature = NexTraceOne.ChangeIntelligence.Application.Features.UpdateDeploymentState.UpdateDeploymentState;

namespace NexTraceOne.ChangeIntelligence.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo ChangeIntelligence.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class ChangeIntelligenceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/releases");

        group.MapPost("/", async (
            NotifyDeploymentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/releases/{0}", localizer);
        });

        group.MapGet("/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetReleaseFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/", async (
            Guid apiAssetId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListReleasesFeature.Query(apiAssetId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/{apiAssetId:guid}/history", async (
            Guid apiAssetId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetReleaseHistoryFeature.Query(apiAssetId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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

        group.MapPut("/{releaseId:guid}/status", async (
            Guid releaseId,
            UpdateDeploymentStateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/{releaseId:guid}/rollback", async (
            Guid releaseId,
            RegisterRollbackFeature.Command command,
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
