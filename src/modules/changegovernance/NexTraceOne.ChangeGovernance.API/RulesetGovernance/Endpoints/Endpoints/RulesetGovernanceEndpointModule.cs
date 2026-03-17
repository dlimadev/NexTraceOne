using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using UploadRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UploadRuleset.UploadRuleset;
using ListRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ListRulesets.ListRulesets;
using ArchiveRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ArchiveRuleset.ArchiveRuleset;
using BindRulesetToAssetTypeFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.BindRulesetToAssetType.BindRulesetToAssetType;
using ExecuteLintForReleaseFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ExecuteLintForRelease.ExecuteLintForRelease;
using GetRulesetFindingsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetFindings.GetRulesetFindings;
using GetRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetScore.GetRulesetScore;
using InstallDefaultRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.InstallDefaultRulesets.InstallDefaultRulesets;
using ComputeRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ComputeRulesetScore.ComputeRulesetScore;

namespace NexTraceOne.ChangeGovernance.API.RulesetGovernance.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo RulesetGovernance.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class RulesetGovernanceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rulesets");

        group.MapPost("/", async (
            UploadRulesetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/{0}", localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapGet("/", async (
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListRulesetsFeature.Query(page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:read");

        group.MapPut("/{rulesetId:guid}/archive", async (
            Guid rulesetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ArchiveRulesetFeature.Command(rulesetId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapPost("/{rulesetId:guid}/bindings", async (
            Guid rulesetId,
            BindRulesetToAssetTypeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { RulesetId = rulesetId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/{0}/bindings", localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapPost("/lint", async (
            ExecuteLintForReleaseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/findings/{0}", localizer);
        })
        .RequirePermission("rulesets:execute");

        group.MapGet("/findings/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRulesetFindingsFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:read");

        group.MapGet("/score/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRulesetScoreFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:read");

        group.MapPost("/install-defaults", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new InstallDefaultRulesetsFeature.Command(), cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/{0}", localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapPost("/compute-score", async (
            ComputeRulesetScoreFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:execute");
    }
}
