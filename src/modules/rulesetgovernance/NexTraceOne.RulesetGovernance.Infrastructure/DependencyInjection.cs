using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.RulesetGovernance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo RulesetGovernance.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRulesetGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
