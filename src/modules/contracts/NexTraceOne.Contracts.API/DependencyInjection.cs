using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Contracts.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Contracts.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddContractsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddContractsApplication(configuration);
        services.AddContractsInfrastructure(configuration);
        return services;
    }
}
