using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.DeveloperPortal.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo DeveloperPortal.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDeveloperPortalModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDeveloperPortalApplication(configuration);
        services.AddDeveloperPortalInfrastructure(configuration);
        return services;
    }
}
