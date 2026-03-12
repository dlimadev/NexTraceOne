using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using GetAssetDetailFeature = NexTraceOne.EngineeringGraph.Application.Features.GetAssetDetail.GetAssetDetail;
using GetAssetGraphFeature = NexTraceOne.EngineeringGraph.Application.Features.GetAssetGraph.GetAssetGraph;
using MapConsumerRelationshipFeature = NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship.MapConsumerRelationship;
using RegisterApiAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset.RegisterApiAsset;
using RegisterServiceAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset.RegisterServiceAsset;
using SearchAssetsFeature = NexTraceOne.EngineeringGraph.Application.Features.SearchAssets.SearchAssets;

namespace NexTraceOne.EngineeringGraph.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo EngineeringGraph.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class EngineeringGraphEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/engineeringgraph");

        group.MapPost("/services", async (
            RegisterServiceAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/engineeringgraph/services/{0}", localizer);
        });

        group.MapPost("/apis", async (
            RegisterApiAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/engineeringgraph/apis/{0}", localizer);
        });

        group.MapPost("/apis/{apiAssetId:guid}/consumers", async (
            Guid apiAssetId,
            MapConsumerRelationshipFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ApiAssetId = apiAssetId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/graph", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAssetGraphFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/apis/{apiAssetId:guid}", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAssetDetailFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/apis/search", async (
            string searchTerm,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchAssetsFeature.Query(searchTerm), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
