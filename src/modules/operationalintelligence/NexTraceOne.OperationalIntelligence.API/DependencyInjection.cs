using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.OperationalIntelligence.Application.Cost;
using NexTraceOne.OperationalIntelligence.Application.Runtime;
using NexTraceOne.OperationalIntelligence.Infrastructure;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost;

namespace NexTraceOne.OperationalIntelligence.API;

/// <summary>
/// Registra todos os serviços do módulo OperationalIntelligence.
/// Substitui AddRuntimeIntelligenceModule + AddReliabilityModule + AddCostIntelligenceModule.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIncidentResponseModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRuntimeIntelligenceApplication(configuration);
        services.AddIncidentResponseInfrastructure(configuration);
        return services;
    }

    public static IServiceCollection AddCostIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCostIntelligenceApplication(configuration);
        services.AddCostIntelligenceInfrastructure(configuration);
        return services;
    }
}
