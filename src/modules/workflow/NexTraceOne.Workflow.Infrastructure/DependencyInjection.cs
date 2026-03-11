using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Workflow.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Workflow.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
