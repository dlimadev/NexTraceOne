using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using ClassifyBreakingChangeFeature = NexTraceOne.Contracts.Application.Features.ClassifyBreakingChange.ClassifyBreakingChange;
using ComputeSemanticDiffFeature = NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff.ComputeSemanticDiff;
using CreateContractVersionFeature = NexTraceOne.Contracts.Application.Features.CreateContractVersion.CreateContractVersion;
using ExportContractFeature = NexTraceOne.Contracts.Application.Features.ExportContract.ExportContract;
using GetContractHistoryFeature = NexTraceOne.Contracts.Application.Features.GetContractHistory.GetContractHistory;
using ImportContractFeature = NexTraceOne.Contracts.Application.Features.ImportContract.ImportContract;
using LockContractVersionFeature = NexTraceOne.Contracts.Application.Features.LockContractVersion.LockContractVersion;
using SuggestSemanticVersionFeature = NexTraceOne.Contracts.Application.Features.SuggestSemanticVersion.SuggestSemanticVersion;

namespace NexTraceOne.Contracts.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Contracts.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class ContractsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts");

        group.MapPost("/", async (
            ImportContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}", localizer);
        });

        group.MapPost("/versions", async (
            CreateContractVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}", localizer);
        });

        group.MapPost("/diff", async (
            ComputeSemanticDiffFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/{contractVersionId:guid}/classification", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ClassifyBreakingChangeFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/suggest-version", async (
            Guid apiAssetId,
            ChangeLevel changeLevel,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SuggestSemanticVersionFeature.Query(apiAssetId, changeLevel), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/history/{apiAssetId:guid}", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetContractHistoryFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/{contractVersionId:guid}/lock", async (
            Guid contractVersionId,
            LockContractVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ContractVersionId = contractVersionId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/{contractVersionId:guid}/export", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportContractFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
