using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.OperationalIntelligence.Application.Cost;
using NexTraceOne.OperationalIntelligence.Application.FinOps;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost;

namespace NexTraceOne.OperationalIntelligence.API.Cost.Endpoints;

/// <summary>
/// Registra serviços do módulo FinOps (CostIntelligence + FinOps Contextual).
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCostIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCostIntelligenceApplication(configuration);
        services.AddFinOpsApplication(configuration);
        services.AddCostIntelligenceInfrastructure(configuration);
        return services;
    }
}
