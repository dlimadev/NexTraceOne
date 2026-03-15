using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.AiGovernance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo AiGovernance.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
