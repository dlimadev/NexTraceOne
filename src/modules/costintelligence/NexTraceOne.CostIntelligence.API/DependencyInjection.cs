using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.CostIntelligence.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo CostIntelligence.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCostIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCostIntelligenceApplication(configuration);
        services.AddCostIntelligenceInfrastructure(configuration);
        return services;
    }
}
