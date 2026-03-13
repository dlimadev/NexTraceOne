using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.RuntimeIntelligence.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo RuntimeIntelligence.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRuntimeIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
