using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.EngineeringGraph.Application.Features.CreateGraphSnapshot;
using NexTraceOne.EngineeringGraph.Application.Features.CreateSavedView;
using NexTraceOne.EngineeringGraph.Application.Features.DecommissionAsset;
using NexTraceOne.EngineeringGraph.Application.Features.GetAssetDetail;
using NexTraceOne.EngineeringGraph.Application.Features.GetImpactPropagation;
using NexTraceOne.EngineeringGraph.Application.Features.GetNodeHealth;
using NexTraceOne.EngineeringGraph.Application.Features.GetSubgraph;
using NexTraceOne.EngineeringGraph.Application.Features.GetTemporalDiff;
using NexTraceOne.EngineeringGraph.Application.Features.InferDependencyFromOtel;
using NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset;
using NexTraceOne.EngineeringGraph.Application.Features.SearchAssets;
using NexTraceOne.EngineeringGraph.Application.Features.SyncConsumers;
using NexTraceOne.EngineeringGraph.Application.Features.UpdateAssetMetadata;
using NexTraceOne.EngineeringGraph.Application.Features.ValidateDiscoveredDependency;

namespace NexTraceOne.EngineeringGraph.Application;

/// <summary>
/// Registra serviços da camada Application do módulo EngineeringGraph.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features,
/// incluindo temporalidade, propagação de impacto, overlays e saved views.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringGraphApplication(
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

        return services;
    }
}
