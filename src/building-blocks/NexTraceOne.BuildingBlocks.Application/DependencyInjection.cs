using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Application;

/// <summary>
/// Registra serviços do BuildingBlocks.Application no DI.
/// Inclui: Pipeline Behaviors MediatR, DateTimeProvider, validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar MediatR behaviors, DateTimeProvider, validators
        return services;
    }
}
