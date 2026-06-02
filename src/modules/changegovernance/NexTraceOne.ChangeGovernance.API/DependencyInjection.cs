using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;
using NexTraceOne.ChangeGovernance.Application.Promotion;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance;
using NexTraceOne.ChangeGovernance.Application.Workflow;
using NexTraceOne.ChangeGovernance.Infrastructure;

namespace NexTraceOne.ChangeGovernance.API;

/// <summary>
/// Registra todos os serviços do módulo ChangeGovernance.
/// Substitui AddChangeIntelligenceModule + AddWorkflowModule + AddPromotionModule + AddRulesetGovernanceModule.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChangeGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChangeIntelligenceApplication(configuration);
        services.AddWorkflowApplication(configuration);
        services.AddPromotionApplication(configuration);
        services.AddRulesetGovernanceApplication(configuration);
        services.AddChangeGovernanceInfrastructure(configuration);
        return services;
    }
}
