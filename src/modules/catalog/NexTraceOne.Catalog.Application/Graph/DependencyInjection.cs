using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Graph.Features.CreateGraphSnapshot;
using NexTraceOne.Catalog.Application.Graph.Features.CreateSavedView;
using NexTraceOne.Catalog.Application.Graph.Features.DecommissionAsset;
using NexTraceOne.Catalog.Application.Graph.Features.GetAssetDetail;
using NexTraceOne.Catalog.Application.Graph.Features.GetImpactPropagation;
using NexTraceOne.Catalog.Application.Graph.Features.GetNodeHealth;
using NexTraceOne.Catalog.Application.Graph.Features.GetSubgraph;
using NexTraceOne.Catalog.Application.Graph.Features.GetTemporalDiff;
using NexTraceOne.Catalog.Application.Graph.Features.InferDependencyFromOtel;
using NexTraceOne.Catalog.Application.Graph.Features.MapConsumerRelationship;
using NexTraceOne.Catalog.Application.Graph.Features.RegisterApiAsset;
using NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset;
using NexTraceOne.Catalog.Application.Graph.Features.SearchAssets;
using NexTraceOne.Catalog.Application.Graph.Features.SyncConsumers;
using NexTraceOne.Catalog.Application.Graph.Features.UpdateAssetMetadata;
using NexTraceOne.Catalog.Application.Graph.Features.ValidateDiscoveredDependency;
using NexTraceOne.Catalog.Application.Graph.Features.AddServiceLink;
using NexTraceOne.Catalog.Application.Graph.Features.ListServiceLinks;
using NexTraceOne.Catalog.Application.Graph.Features.RemoveServiceLink;
using NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceLink;
using NexTraceOne.Catalog.Application.Graph.Features.RunServiceDiscovery;
using NexTraceOne.Catalog.Application.Graph.Features.ListDiscoveredServices;
using NexTraceOne.Catalog.Application.Graph.Features.MatchDiscoveredService;
using NexTraceOne.Catalog.Application.Graph.Features.RegisterFromDiscovery;
using NexTraceOne.Catalog.Application.Graph.Features.IgnoreDiscoveredService;
using NexTraceOne.Catalog.Application.Graph.Features.GetDiscoveryDashboard;

namespace NexTraceOne.Catalog.Application.Graph;

/// <summary>
/// Registra serviços da camada Application do módulo Catalog Graph.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features,
/// incluindo temporalidade, propagação de impacto, overlays e saved views.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogGraphApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Features existentes ──────────────────────────────────────────
        services.AddTransient<IValidator<RegisterServiceAsset.Command>, RegisterServiceAsset.Validator>();
        services.AddTransient<IValidator<RegisterApiAsset.Command>, RegisterApiAsset.Validator>();
        services.AddTransient<IValidator<MapConsumerRelationship.Command>, MapConsumerRelationship.Validator>();
        services.AddTransient<IValidator<GetAssetDetail.Query>, GetAssetDetail.Validator>();
        services.AddTransient<IValidator<SearchAssets.Query>, SearchAssets.Validator>();
        services.AddTransient<IValidator<InferDependencyFromOtel.Command>, InferDependencyFromOtel.Validator>();
        services.AddTransient<IValidator<ValidateDiscoveredDependency.Query>, ValidateDiscoveredDependency.Validator>();
        services.AddTransient<IValidator<DecommissionAsset.Command>, DecommissionAsset.Validator>();
        services.AddTransient<IValidator<UpdateAssetMetadata.Command>, UpdateAssetMetadata.Validator>();

        // ── Subgrafo e navegação contextual ──────────────────────────────
        services.AddTransient<IValidator<GetSubgraph.Query>, GetSubgraph.Validator>();
        services.AddTransient<IValidator<GetImpactPropagation.Query>, GetImpactPropagation.Validator>();

        // ── Temporalidade ────────────────────────────────────────────────
        services.AddTransient<IValidator<CreateGraphSnapshot.Command>, CreateGraphSnapshot.Validator>();
        services.AddTransient<IValidator<GetTemporalDiff.Query>, GetTemporalDiff.Validator>();

        // ── Overlays e saúde ─────────────────────────────────────────────
        services.AddTransient<IValidator<GetNodeHealth.Query>, GetNodeHealth.Validator>();

        // ── Saved Views ──────────────────────────────────────────────────
        services.AddTransient<IValidator<CreateSavedView.Command>, CreateSavedView.Validator>();

        // ── Integração Inbound Externa ───────────────────────────────────
        services.AddTransient<IValidator<SyncConsumers.Command>, SyncConsumers.Validator>();

        // ── Service Links ────────────────────────────────────────────────
        services.AddTransient<IValidator<AddServiceLink.Command>, AddServiceLink.Validator>();
        services.AddTransient<IValidator<RemoveServiceLink.Command>, RemoveServiceLink.Validator>();
        services.AddTransient<IValidator<ListServiceLinks.Query>, ListServiceLinks.Validator>();
        services.AddTransient<IValidator<UpdateServiceLink.Command>, UpdateServiceLink.Validator>();

        // ── Service Discovery ────────────────────────────────────────────
        services.AddTransient<IValidator<RunServiceDiscovery.Command>, RunServiceDiscovery.Validator>();
        services.AddTransient<IValidator<ListDiscoveredServices.Query>, ListDiscoveredServices.Validator>();
        services.AddTransient<IValidator<MatchDiscoveredService.Command>, MatchDiscoveredService.Validator>();
        services.AddTransient<IValidator<RegisterFromDiscovery.Command>, RegisterFromDiscovery.Validator>();
        services.AddTransient<IValidator<IgnoreDiscoveredService.Command>, IgnoreDiscoveredService.Validator>();
        services.AddTransient<IValidator<GetDiscoveryDashboard.Query>, GetDiscoveryDashboard.Validator>();

        return services;
    }
}
