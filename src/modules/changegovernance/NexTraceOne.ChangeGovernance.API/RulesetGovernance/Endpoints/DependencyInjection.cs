using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.ChangeGovernance.Application.RulesetGovernance;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance;

namespace NexTraceOne.ChangeGovernance.API.RulesetGovernance.Endpoints;

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
