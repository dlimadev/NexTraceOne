using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.RulesetGovernance.Application;
using NexTraceOne.RulesetGovernance.Infrastructure;

namespace NexTraceOne.RulesetGovernance.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo RulesetGovernance.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRulesetGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRulesetGovernanceApplication(configuration);
        services.AddRulesetGovernanceInfrastructure(configuration);
        return services;
    }
}
