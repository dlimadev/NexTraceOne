using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Licensing.Application;
using NexTraceOne.Licensing.Infrastructure;

namespace NexTraceOne.Licensing.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Licensing.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddLicensingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLicensingApplication(configuration);
        services.AddLicensingInfrastructure(configuration);
        return services;
    }
}
