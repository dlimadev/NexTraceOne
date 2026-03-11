using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.ExternalAi.Application;
using NexTraceOne.ExternalAi.Infrastructure;

namespace NexTraceOne.ExternalAi.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo ExternalAi.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddExternalAiModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddExternalAiApplication(configuration);
        services.AddExternalAiInfrastructure(configuration);
        return services;
    }
}
