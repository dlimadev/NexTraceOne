using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.RuntimeIntelligence.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo RuntimeIntelligence.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRuntimeIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRuntimeIntelligenceApplication(configuration);
        services.AddRuntimeIntelligenceInfrastructure(configuration);
        return services;
    }
}
