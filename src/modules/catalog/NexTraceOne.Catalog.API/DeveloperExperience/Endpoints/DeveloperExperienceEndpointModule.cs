using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ComputeDxScoreFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.ComputeDeveloperExperienceScore.ComputeDeveloperExperienceScore;
using GetDxScoreFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperExperienceScore.GetDeveloperExperienceScore;
using ListDxScoresFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.ListDeveloperExperienceScores.ListDeveloperExperienceScores;
using RecordSnapshotFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordProductivitySnapshot.RecordProductivitySnapshot;

namespace NexTraceOne.Catalog.API.DeveloperExperience.Endpoints;

/// <summary>
/// Registra endpoints do módulo Developer Experience: scores de DX e snapshots de produtividade.
/// </summary>
public sealed class DeveloperExperienceEndpointModule
{
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/developer-experience").RequireRateLimiting("catalog");

        group.MapPost("/scores", async (
            ComputeDxScoreFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:write");

        group.MapGet("/scores", async (
            string? period,
            string? scoreLevel,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new ListDxScoresFeature.Query(period, scoreLevel, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:read");

        group.MapGet("/scores/{teamId}", async (
            string teamId,
            string period,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetDxScoreFeature.Query(teamId, period);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:read");

        group.MapPost("/snapshots", async (
            RecordSnapshotFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:write");
    }
}
