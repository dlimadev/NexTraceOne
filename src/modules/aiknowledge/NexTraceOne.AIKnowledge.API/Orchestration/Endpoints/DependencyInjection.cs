using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Orchestration;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration;

namespace NexTraceOne.AIKnowledge.API.Orchestration.Endpoints;

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
