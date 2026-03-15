using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AiGovernance.Application;

namespace NexTraceOne.AiGovernance.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo AiGovernance.
/// Compõe Application layer. Infrastructure será adicionada quando implementada.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiGovernanceApplication(configuration);
        return services;
    }
}
