using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using CreateGraphSnapshotFeature = NexTraceOne.Catalog.Application.Graph.Features.CreateGraphSnapshot.CreateGraphSnapshot;
using CreateSavedViewFeature = NexTraceOne.Catalog.Application.Graph.Features.CreateSavedView.CreateSavedView;
using GetAssetDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.GetAssetDetail.GetAssetDetail;
using GetAssetGraphFeature = NexTraceOne.Catalog.Application.Graph.Features.GetAssetGraph.GetAssetGraph;
using GetImpactPropagationFeature = NexTraceOne.Catalog.Application.Graph.Features.GetImpactPropagation.GetImpactPropagation;
using GetNodeHealthFeature = NexTraceOne.Catalog.Application.Graph.Features.GetNodeHealth.GetNodeHealth;
using GetServiceDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceDetail.GetServiceDetail;
using GetServicesSummaryFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServicesSummary.GetServicesSummary;
using GetSubgraphFeature = NexTraceOne.Catalog.Application.Graph.Features.GetSubgraph.GetSubgraph;
using GetTemporalDiffFeature = NexTraceOne.Catalog.Application.Graph.Features.GetTemporalDiff.GetTemporalDiff;
using ListSavedViewsFeature = NexTraceOne.Catalog.Application.Graph.Features.ListSavedViews.ListSavedViews;
using ListServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServices.ListServices;
using ListSnapshotsFeature = NexTraceOne.Catalog.Application.Graph.Features.ListSnapshots.ListSnapshots;
using MapConsumerRelationshipFeature = NexTraceOne.Catalog.Application.Graph.Features.MapConsumerRelationship.MapConsumerRelationship;
using RegisterApiAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterApiAsset.RegisterApiAsset;
using RegisterServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset.RegisterServiceAsset;
using SearchAssetsFeature = NexTraceOne.Catalog.Application.Graph.Features.SearchAssets.SearchAssets;
using SearchServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.SearchServices.SearchServices;
using SyncConsumersFeature = NexTraceOne.Catalog.Application.Graph.Features.SyncConsumers.SyncConsumers;
using UpdateServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceAsset.UpdateServiceAsset;
using UpdateServiceOwnershipFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceOwnership.UpdateServiceOwnership;

namespace NexTraceOne.Catalog.API.Graph.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Catalog Graph.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Inclui endpoints para ativos, subgrafo contextual, temporalidade,
/// propagação de impacto, overlays e saved views.
///
/// Política de autorização:
/// - Endpoints de leitura exigem "catalog:assets:read".
/// - Endpoints de escrita exigem "catalog:assets:write".
/// - Endpoint de integração externa exige "catalog:assets:write"
///   pois modifica o grafo a partir de fontes externas.
/// </summary>
public sealed class ServiceCatalogEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog");

        // ── Ativos: Registro e Consulta ──────────────────────────────────

        group.MapPost("/services", async (
            RegisterServiceAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/catalog/services/{0}", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPost("/apis", async (
            RegisterApiAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/catalog/apis/{0}", localizer);
        }).RequirePermission("catalog:assets:write");

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
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/graph", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAssetGraphFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/apis/{apiAssetId:guid}", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAssetDetailFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/apis/search", async (
            string searchTerm,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchAssetsFeature.Query(searchTerm), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Catálogo de Serviços: Listagem, Detalhe e Gestão ────────────

        group.MapGet("/services", async (
            string? teamName,
            string? domain,
            ServiceType? serviceType,
            Criticality? criticality,
            LifecycleStatus? lifecycleStatus,
            ExposureType? exposureType,
            string? search,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListServicesFeature.Query(
                teamName, domain, serviceType, criticality,
                lifecycleStatus, exposureType, search);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/services/{serviceId:guid}", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetServiceDetailFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPut("/services/{serviceId:guid}", async (
            Guid serviceId,
            UpdateServiceAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/services/{serviceId:guid}/ownership", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetServiceDetailFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPatch("/services/{serviceId:guid}/ownership", async (
            Guid serviceId,
            UpdateServiceOwnershipFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/services/summary", async (
            string? teamName,
            string? domain,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServicesSummaryFeature.Query(teamName, domain);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/services/search", async (
            string q,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchServicesFeature.Query(q), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

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
        }).RequirePermission("catalog:assets:read");

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
        }).RequirePermission("catalog:assets:read");

        // ── Temporalidade (Snapshots e Diff) ─────────────────────────────

        group.MapPost("/snapshots", async (
            CreateGraphSnapshotFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/catalog/snapshots/{0}", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/snapshots", async (
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSnapshotsFeature.Query(limit ?? 50);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

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
        }).RequirePermission("catalog:assets:read");

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
        }).RequirePermission("catalog:assets:read");

        // ── Saved Views ──────────────────────────────────────────────────

        group.MapPost("/views", async (
            CreateSavedViewFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/catalog/views/{0}", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/views", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListSavedViewsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Integração Inbound Externa ───────────────────────────────────
        // Endpoint para sincronização de consumidores vindos de sistemas externos.
        // Suporta autenticação sistema-a-sistema e upsert em lote.
        // Exige permissão de escrita pois modifica o grafo a partir de fontes externas.

        group.MapPost("/integration/v1/consumers/sync", async (
            SyncConsumersFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");
    }
}
