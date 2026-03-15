using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Governance.Application;
using NexTraceOne.Governance.Infrastructure;

namespace NexTraceOne.Governance.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Governance.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGovernanceApplication(configuration);
        services.AddGovernanceInfrastructure(configuration);
        return services;
    }
}
