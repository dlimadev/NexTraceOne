using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.AiOrchestration.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo AiOrchestration.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiOrchestrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiOrchestrationApplication(configuration);
        services.AddAiOrchestrationInfrastructure(configuration);
        return services;
    }
}
