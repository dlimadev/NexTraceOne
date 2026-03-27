using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Integrations.Application;
using NexTraceOne.Integrations.Infrastructure;

namespace NexTraceOne.Integrations.API.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo Integrations.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIntegrationsApplication(configuration);
        services.AddIntegrationsInfrastructure(configuration);
        return services;
    }
}
