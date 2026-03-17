using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AiGovernance.Application;
using NexTraceOne.AiGovernance.Infrastructure;

namespace NexTraceOne.AiGovernance.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo AiGovernance.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiGovernanceApplication(configuration);
        services.AddAiGovernanceInfrastructure(configuration);
        return services;
    }
}
