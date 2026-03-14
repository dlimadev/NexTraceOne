using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Workflow.Application;
using NexTraceOne.Workflow.Infrastructure;

namespace NexTraceOne.Workflow.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Workflow.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWorkflowApplication(configuration);
        services.AddWorkflowInfrastructure(configuration);
        return services;
    }
}
