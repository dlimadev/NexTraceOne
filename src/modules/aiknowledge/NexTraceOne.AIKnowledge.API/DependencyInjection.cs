using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.ExternalAI;
using NexTraceOne.AIKnowledge.Application.Governance;
using NexTraceOne.AIKnowledge.Application.Orchestration;
using NexTraceOne.AIKnowledge.Infrastructure;

namespace NexTraceOne.AIKnowledge.API;

/// <summary>
/// Registra todos os serviços do módulo AIHub (AIKnowledge).
/// Substitui AddAiGovernanceModule + AddExternalAiModule + AddAiOrchestrationModule.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiHubModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiGovernanceApplication(configuration);
        services.AddExternalAiApplication(configuration);
        services.AddAiOrchestrationApplication(configuration);
        services.AddAiHubInfrastructure(configuration);
        return services;
    }
}
