using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using UploadRulesetFeature = NexTraceOne.RulesetGovernance.Application.Features.UploadRuleset.UploadRuleset;
using ListRulesetsFeature = NexTraceOne.RulesetGovernance.Application.Features.ListRulesets.ListRulesets;
using ArchiveRulesetFeature = NexTraceOne.RulesetGovernance.Application.Features.ArchiveRuleset.ArchiveRuleset;
using BindRulesetToAssetTypeFeature = NexTraceOne.RulesetGovernance.Application.Features.BindRulesetToAssetType.BindRulesetToAssetType;
using ExecuteLintForReleaseFeature = NexTraceOne.RulesetGovernance.Application.Features.ExecuteLintForRelease.ExecuteLintForRelease;
using GetRulesetFindingsFeature = NexTraceOne.RulesetGovernance.Application.Features.GetRulesetFindings.GetRulesetFindings;
using GetRulesetScoreFeature = NexTraceOne.RulesetGovernance.Application.Features.GetRulesetScore.GetRulesetScore;
using InstallDefaultRulesetsFeature = NexTraceOne.RulesetGovernance.Application.Features.InstallDefaultRulesets.InstallDefaultRulesets;
using ComputeRulesetScoreFeature = NexTraceOne.RulesetGovernance.Application.Features.ComputeRulesetScore.ComputeRulesetScore;

namespace NexTraceOne.RulesetGovernance.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do modulo RulesetGovernance.
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
        });

        group.MapGet("/", async (
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListRulesetsFeature.Query(page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPut("/{rulesetId:guid}/archive", async (
            Guid rulesetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ArchiveRulesetFeature.Command(rulesetId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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
        });

        group.MapPost("/lint", async (
            ExecuteLintForReleaseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/findings/{0}", localizer);
        });

        group.MapGet("/findings/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRulesetFindingsFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/score/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRulesetScoreFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/install-defaults", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new InstallDefaultRulesetsFeature.Command(), cancellationToken);
            return result.ToCreatedResult("/api/v1/rulesets/{0}", localizer);
        });

        group.MapPost("/compute-score", async (
            ComputeRulesetScoreFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
