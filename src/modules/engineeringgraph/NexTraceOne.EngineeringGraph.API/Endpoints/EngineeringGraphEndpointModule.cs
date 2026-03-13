using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.EngineeringGraph.Domain.Enums;
using CreateGraphSnapshotFeature = NexTraceOne.EngineeringGraph.Application.Features.CreateGraphSnapshot.CreateGraphSnapshot;
using CreateSavedViewFeature = NexTraceOne.EngineeringGraph.Application.Features.CreateSavedView.CreateSavedView;
using GetAssetDetailFeature = NexTraceOne.EngineeringGraph.Application.Features.GetAssetDetail.GetAssetDetail;
using GetAssetGraphFeature = NexTraceOne.EngineeringGraph.Application.Features.GetAssetGraph.GetAssetGraph;
using GetImpactPropagationFeature = NexTraceOne.EngineeringGraph.Application.Features.GetImpactPropagation.GetImpactPropagation;
using GetNodeHealthFeature = NexTraceOne.EngineeringGraph.Application.Features.GetNodeHealth.GetNodeHealth;
using GetSubgraphFeature = NexTraceOne.EngineeringGraph.Application.Features.GetSubgraph.GetSubgraph;
using GetTemporalDiffFeature = NexTraceOne.EngineeringGraph.Application.Features.GetTemporalDiff.GetTemporalDiff;
using ListSavedViewsFeature = NexTraceOne.EngineeringGraph.Application.Features.ListSavedViews.ListSavedViews;
using ListSnapshotsFeature = NexTraceOne.EngineeringGraph.Application.Features.ListSnapshots.ListSnapshots;
using MapConsumerRelationshipFeature = NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship.MapConsumerRelationship;
using RegisterApiAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset.RegisterApiAsset;
using RegisterServiceAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset.RegisterServiceAsset;
using SearchAssetsFeature = NexTraceOne.EngineeringGraph.Application.Features.SearchAssets.SearchAssets;
using SyncConsumersFeature = NexTraceOne.EngineeringGraph.Application.Features.SyncConsumers.SyncConsumers;

namespace NexTraceOne.EngineeringGraph.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo EngineeringGraph.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Inclui endpoints para ativos, subgrafo contextual, temporalidade,
/// propagação de impacto, overlays e saved views.
/// </summary>
public sealed class EngineeringGraphEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/engineeringgraph");

        // ── Ativos: Registro e Consulta ──────────────────────────────────

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

        // ── Subgrafo Contextual (mini-grafos) ────────────────────────────

        group.MapGet("/subgraph/{rootNodeId:guid}", async (
            Guid rootNodeId,
            int? maxDepth,
            int? maxNodes,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSubgraphFeature.Query(rootNodeId, maxDepth ?? 2, maxNodes ?? 50);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ── Propagação de Impacto ────────────────────────────────────────

        group.MapGet("/impact/{rootNodeId:guid}", async (
            Guid rootNodeId,
            int? maxDepth,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetImpactPropagationFeature.Query(rootNodeId, maxDepth ?? 3);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ── Temporalidade (Snapshots e Diff) ─────────────────────────────

        group.MapPost("/snapshots", async (
            CreateGraphSnapshotFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/engineeringgraph/snapshots/{0}", localizer);
        });

        group.MapGet("/snapshots", async (
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSnapshotsFeature.Query(limit ?? 50);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/snapshots/diff", async (
            Guid fromSnapshotId,
            Guid toSnapshotId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTemporalDiffFeature.Query(fromSnapshotId, toSnapshotId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ── Overlays e Saúde ─────────────────────────────────────────────

        group.MapGet("/health", async (
            OverlayMode overlayMode,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNodeHealthFeature.Query(overlayMode);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ── Saved Views ──────────────────────────────────────────────────

        group.MapPost("/views", async (
            CreateSavedViewFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/engineeringgraph/views/{0}", localizer);
        });

        group.MapGet("/views", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListSavedViewsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ── Integração Inbound Externa ───────────────────────────────────
        // Endpoint para sincronização de consumidores vindos de sistemas externos.
        // Suporta autenticação sistema-a-sistema e upsert em lote.

        group.MapPost("/integration/v1/consumers/sync", async (
            SyncConsumersFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
