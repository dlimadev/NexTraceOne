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
using TransitionServiceLifecycleFeature = NexTraceOne.Catalog.Application.Graph.Features.TransitionServiceLifecycle.TransitionServiceLifecycle;
using AddServiceLinkFeature = NexTraceOne.Catalog.Application.Graph.Features.AddServiceLink.AddServiceLink;
using ListServiceLinksFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServiceLinks.ListServiceLinks;
using RemoveServiceLinkFeature = NexTraceOne.Catalog.Application.Graph.Features.RemoveServiceLink.RemoveServiceLink;
using UpdateServiceLinkFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceLink.UpdateServiceLink;
using RunServiceDiscoveryFeature = NexTraceOne.Catalog.Application.Graph.Features.RunServiceDiscovery.RunServiceDiscovery;
using ListDiscoveredServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListDiscoveredServices.ListDiscoveredServices;
using MatchDiscoveredServiceFeature = NexTraceOne.Catalog.Application.Graph.Features.MatchDiscoveredService.MatchDiscoveredService;
using RegisterFromDiscoveryFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterFromDiscovery.RegisterFromDiscovery;
using IgnoreDiscoveredServiceFeature = NexTraceOne.Catalog.Application.Graph.Features.IgnoreDiscoveredService.IgnoreDiscoveredService;
using GetDiscoveryDashboardFeature = NexTraceOne.Catalog.Application.Graph.Features.GetDiscoveryDashboard.GetDiscoveryDashboard;
using ComputeServiceMaturityFeature = NexTraceOne.Catalog.Application.Graph.Features.ComputeServiceMaturity.ComputeServiceMaturity;
using GetServiceMaturityDashboardFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityDashboard.GetServiceMaturityDashboard;
using DetectCircularDependenciesFeature = NexTraceOne.Catalog.Application.Graph.Features.DetectCircularDependencies.DetectCircularDependencies;
using PropagateHealthStatusFeature = NexTraceOne.Catalog.Application.Graph.Features.PropagateHealthStatus.PropagateHealthStatus;
using GetOwnershipAuditFeature = NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipAudit.GetOwnershipAudit;
using RegisterFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterFrameworkDetail.RegisterFrameworkDetail;
using UpdateFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateFrameworkDetail.UpdateFrameworkDetail;
using GetFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.GetFrameworkDetail.GetFrameworkDetail;
using PublishFrameworkVersionFeature = NexTraceOne.Catalog.Application.Graph.Features.PublishFrameworkVersion.PublishFrameworkVersion;
using GetServiceMaturityBenchmarkFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityBenchmark.GetServiceMaturityBenchmark;
using CreateServiceInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.CreateServiceInterface.CreateServiceInterface;
using ListServiceInterfacesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServiceInterfaces.ListServiceInterfaces;
using GetServiceInterfaceByIdFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceInterfaceById.GetServiceInterfaceById;
using DeprecateServiceInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.DeprecateServiceInterface.DeprecateServiceInterface;
using BindContractToInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.BindContractToInterface.BindContractToInterface;
using ListContractBindingsByInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.ListContractBindingsByInterface.ListContractBindingsByInterface;
using DeactivateContractBindingFeature = NexTraceOne.Catalog.Application.Graph.Features.DeactivateContractBinding.DeactivateContractBinding;
using SetServiceTierFeature = NexTraceOne.Catalog.Application.Graph.Features.SetServiceTier.SetServiceTier;
using GetServiceTierPolicyFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceTierPolicy.GetServiceTierPolicy;
using DetectOwnershipDriftFeature = NexTraceOne.Catalog.Application.Graph.Features.DetectOwnershipDrift.DetectOwnershipDrift;
using GetOwnershipDriftReportFeature = NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipDriftReport.GetOwnershipDriftReport;
using ReviewServiceOwnershipFeature = NexTraceOne.Catalog.Application.Graph.Features.ReviewServiceOwnership.ReviewServiceOwnership;
using ExportToBackstageFeature = NexTraceOne.Catalog.Application.Graph.Features.ExportToBackstage.ExportToBackstage;

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
/// <summary>
    /// Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog").RequireRateLimiting("data-intensive");

        // ── Ativos: Registro e Consulta ──────────────────────────────────

        group.MapPost("/services", async (
            RegisterServiceAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/services/{r.ServiceAssetId}", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPost("/apis", async (
            RegisterApiAssetFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/apis/{r.ApiAssetId}", localizer);
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
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListServicesFeature.Query(
                teamName, domain, serviceType, criticality,
                lifecycleStatus, exposureType, search,
                page ?? 1, pageSize ?? 50);
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

        group.MapPatch("/services/{serviceId:guid}/lifecycle", async (
            Guid serviceId,
            TransitionServiceLifecycleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

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

        // ── Service Links ────────────────────────────────────────────────

        group.MapGet("/services/{serviceId:guid}/links", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListServiceLinksFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPost("/services/{serviceId:guid}/links", async (
            Guid serviceId,
            AddServiceLinkFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/services/{r.ServiceAssetId}/links", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPut("/services/{serviceId:guid}/links/{linkId:guid}", async (
            Guid serviceId,
            Guid linkId,
            UpdateServiceLinkFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId, LinkId = linkId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapDelete("/services/{serviceId:guid}/links/{linkId:guid}", async (
            Guid serviceId,
            Guid linkId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RemoveServiceLinkFeature.Command(serviceId, linkId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

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
            return result.ToCreatedResult(r => $"/api/v1/catalog/snapshots/{r.SnapshotId}", localizer);
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

        // ── Propagação de Saúde ──────────────────────────────────────────

        group.MapGet("/health-propagation/{rootServiceName}", async (
            string rootServiceName,
            int? maxDepth,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new PropagateHealthStatusFeature.Query(rootServiceName, maxDepth ?? 4);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Dependências Circulares ──────────────────────────────────────

        group.MapGet("/circular-dependencies", async (
            string? serviceName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new DetectCircularDependenciesFeature.Query(serviceName);
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
            return result.ToCreatedResult(r => $"/api/v1/catalog/views/{r.ViewId}", localizer);
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

        // ── Service Discovery ────────────────────────────────────────────

        group.MapPost("/discovery/run", async (
            RunServiceDiscoveryFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/discovery/services", async (
            string? status,
            string? environment,
            string? search,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListDiscoveredServicesFeature.Query(status, environment, search);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/discovery/dashboard", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDiscoveryDashboardFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPost("/discovery/services/{discoveredServiceId:guid}/match", async (
            Guid discoveredServiceId,
            MatchDiscoveredServiceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DiscoveredServiceId = discoveredServiceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPost("/discovery/services/{discoveredServiceId:guid}/register", async (
            Guid discoveredServiceId,
            RegisterFromDiscoveryFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DiscoveredServiceId = discoveredServiceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/services/{r.ServiceAssetId}", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPost("/discovery/services/{discoveredServiceId:guid}/ignore", async (
            Guid discoveredServiceId,
            IgnoreDiscoveredServiceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DiscoveredServiceId = discoveredServiceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        // ── Maturity & Ownership Audit ────────────────────────────────────

        group.MapGet("/services/{serviceId:guid}/maturity", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ComputeServiceMaturityFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/maturity/dashboard", async (
            string? teamName,
            string? domain,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServiceMaturityDashboardFeature.Query(teamName, domain);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/ownership/audit", async (
            string? teamName,
            string? domain,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOwnershipAuditFeature.Query(teamName, domain);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Framework / SDK Details ──────────────────────────────────────

        group.MapPost("/services/{serviceId:guid}/framework", async (
            Guid serviceId,
            RegisterFrameworkDetailFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/services/{r.ServiceAssetId}/framework", localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapPut("/services/{serviceId:guid}/framework", async (
            Guid serviceId,
            UpdateFrameworkDetailFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/services/{serviceId:guid}/framework", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetFrameworkDetailFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPost("/services/{serviceId:guid}/framework/versions", async (
            Guid serviceId,
            PublishFrameworkVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        // ── Maturity Benchmark ───────────────────────────────────────────

        group.MapGet("/maturity/benchmark", async (
            string? domain,
            string? teamName,
            int? topN,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServiceMaturityBenchmarkFeature.Query(domain, teamName, topN ?? 10);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Service Interfaces ───────────────────────────────────────────

        group.MapGet("/services/{serviceId:guid}/interfaces", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListServiceInterfacesFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:interfaces:read");

        group.MapPost("/services/{serviceId:guid}/interfaces", async (
            Guid serviceId,
            CreateServiceInterfaceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceAssetId = serviceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/interfaces/{r.InterfaceId}", localizer);
        }).RequirePermission("catalog:interfaces:write");

        group.MapGet("/interfaces/{interfaceId:guid}", async (
            Guid interfaceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetServiceInterfaceByIdFeature.Query(interfaceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:interfaces:read");

        group.MapPatch("/interfaces/{interfaceId:guid}/deprecate", async (
            Guid interfaceId,
            DeprecateServiceInterfaceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { InterfaceId = interfaceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:interfaces:write");

        // ── Contract Bindings ─────────────────────────────────────────────

        group.MapGet("/interfaces/{interfaceId:guid}/bindings", async (
            Guid interfaceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListContractBindingsByInterfaceFeature.Query(interfaceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contract-bindings:read");

        group.MapPost("/interfaces/{interfaceId:guid}/bindings", async (
            Guid interfaceId,
            BindContractToInterfaceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ServiceInterfaceId = interfaceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/bindings/{r.BindingId}", localizer);
        }).RequirePermission("catalog:contract-bindings:write");

        group.MapDelete("/bindings/{bindingId:guid}", async (
            Guid bindingId,
            string deactivatedBy,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateContractBindingFeature.Command(bindingId, deactivatedBy), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contract-bindings:write");

        // ── Service Tier & Ownership Drift (Wave A.3) ─────────────────────

        group.MapPut("/services/{serviceId:guid}/tier", async (
            Guid serviceId,
            SetServiceTierFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { ServiceId = serviceId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/services/{serviceId:guid}/tier-policy", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetServiceTierPolicyFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapGet("/services/{serviceId:guid}/ownership-drift", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DetectOwnershipDriftFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        group.MapPost("/services/{serviceId:guid}/ownership-review", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReviewServiceOwnershipFeature.Command(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:write");

        group.MapGet("/ownership/drift-report", async (
            string? teamName,
            string? domain,
            string? tier,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOwnershipDriftReportFeature.Query(teamName, domain, tier);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Backstage Bridge Export ──────────────────────────────────────

        group.MapGet("/services/export/backstage", async (
            string? @namespace,
            string? lifecycle,
            string? teamName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ExportToBackstageFeature.Query(@namespace, lifecycle, teamName);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");
    }
}
