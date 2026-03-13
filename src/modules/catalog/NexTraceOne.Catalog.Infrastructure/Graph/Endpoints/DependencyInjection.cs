using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.EngineeringGraph.Application;
using NexTraceOne.EngineeringGraph.Infrastructure;

namespace NexTraceOne.EngineeringGraph.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo EngineeringGraph.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringGraphModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEngineeringGraphApplication(configuration);
        services.AddEngineeringGraphInfrastructure(configuration);
        return services;
    }
}
