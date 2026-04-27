using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using UploadRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UploadRuleset.UploadRuleset;
using ListRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ListRulesets.ListRulesets;
using ArchiveRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ArchiveRuleset.ArchiveRuleset;
using ActivateRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ActivateRuleset.ActivateRuleset;
using DeleteRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.DeleteRuleset.DeleteRuleset;
using BindRulesetToAssetTypeFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.BindRulesetToAssetType.BindRulesetToAssetType;
using ExecuteLintForReleaseFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ExecuteLintForRelease.ExecuteLintForRelease;
using GetRulesetFindingsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetFindings.GetRulesetFindings;
using GetRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetScore.GetRulesetScore;
using InstallDefaultRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.InstallDefaultRulesets.InstallDefaultRulesets;
using ComputeRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ComputeRulesetScore.ComputeRulesetScore;
using GetSpectralMarketplaceFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetSpectralMarketplace.GetSpectralMarketplace;
using ActivateSpectralPackageFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ActivateSpectralPackage.ActivateSpectralPackage;

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
        var group = app.MapGroup("/api/v1/contracts/spectral/rulesets");

        group.MapPost("/", async (
            UploadRulesetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/contracts/spectral/rulesets/{r.RulesetId}", localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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

        group.MapPut("/{rulesetId:guid}/activate", async (
            Guid rulesetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateRulesetFeature.Command(rulesetId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapDelete("/{rulesetId:guid}", async (
            Guid rulesetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteRulesetFeature.Command(rulesetId), cancellationToken);
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
            return result.ToCreatedResult(r => $"/api/v1/contracts/spectral/rulesets/{r.RulesetId}/bindings", localizer);
        })
        .RequirePermission("rulesets:write");

        group.MapPost("/lint", async (
            ExecuteLintForReleaseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/contracts/spectral/rulesets/findings/{r.ReleaseId}", localizer);
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
            return result.ToCreatedResult(r => $"/api/v1/contracts/spectral/rulesets/{r.RulesetId}", localizer);
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

        // ── CC-08: Contract Linting Marketplace ──────────────────────────
        var marketplaceGroup = app.MapGroup("/api/v1/rulesets/marketplace");

        marketplaceGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSpectralMarketplaceFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:read")
        .WithTags("Contract Linting Marketplace")
        .WithSummary("List available Spectral packages in the marketplace");

        marketplaceGroup.MapPost("/{packageId}/activate", async (
            string packageId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ActivateSpectralPackageFeature.Command(packageId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("rulesets:write")
        .WithTags("Contract Linting Marketplace")
        .WithSummary("Activate a Spectral package from the marketplace");
    }
}
