using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.EngineeringGraph.Application.Features.GetAssetDetail;
using NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset;
using NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset;
using NexTraceOne.EngineeringGraph.Application.Features.SearchAssets;

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

        return services;
    }
}
