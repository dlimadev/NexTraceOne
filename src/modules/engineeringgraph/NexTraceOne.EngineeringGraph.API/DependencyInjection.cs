using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
