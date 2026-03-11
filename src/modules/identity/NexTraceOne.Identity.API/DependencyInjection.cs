using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Identity.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Identity.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityApplication(configuration);
        services.AddIdentityInfrastructure(configuration);
        return services;
    }
}
