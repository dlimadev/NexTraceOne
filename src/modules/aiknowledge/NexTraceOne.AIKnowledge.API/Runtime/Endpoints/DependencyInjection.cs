using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Runtime;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime;

namespace NexTraceOne.AIKnowledge.API.Runtime.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo AiRuntime.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiRuntimeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiRuntimeApplication(configuration);
        services.AddAiRuntimeInfrastructure(configuration);
        return services;
    }
}
