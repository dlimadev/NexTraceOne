using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Configuration.Application;
using NexTraceOne.Configuration.Infrastructure;

namespace NexTraceOne.Configuration.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Configuration.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddConfigurationApplication(configuration);
        services.AddConfigurationInfrastructure(configuration);
        return services;
    }
}
