using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.EngineeringGraph.Application.Features.DecommissionAsset;
using NexTraceOne.EngineeringGraph.Application.Features.GetAssetDetail;
using NexTraceOne.EngineeringGraph.Application.Features.InferDependencyFromOtel;
using NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset;
using NexTraceOne.EngineeringGraph.Application.Features.SearchAssets;
using NexTraceOne.EngineeringGraph.Application.Features.UpdateAssetMetadata;
using NexTraceOne.EngineeringGraph.Application.Features.ValidateDiscoveredDependency;

namespace NexTraceOne.EngineeringGraph.Application;

/// <summary>
/// Registra serviços da camada Application do módulo EngineeringGraph.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringGraphApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<RegisterServiceAsset.Command>, RegisterServiceAsset.Validator>();
        services.AddTransient<IValidator<RegisterApiAsset.Command>, RegisterApiAsset.Validator>();
        services.AddTransient<IValidator<MapConsumerRelationship.Command>, MapConsumerRelationship.Validator>();
        services.AddTransient<IValidator<GetAssetDetail.Query>, GetAssetDetail.Validator>();
        services.AddTransient<IValidator<SearchAssets.Query>, SearchAssets.Validator>();
        services.AddTransient<IValidator<InferDependencyFromOtel.Command>, InferDependencyFromOtel.Validator>();
        services.AddTransient<IValidator<ValidateDiscoveredDependency.Query>, ValidateDiscoveredDependency.Validator>();
        services.AddTransient<IValidator<DecommissionAsset.Command>, DecommissionAsset.Validator>();
        services.AddTransient<IValidator<UpdateAssetMetadata.Command>, UpdateAssetMetadata.Validator>();

        return services;
    }
}
