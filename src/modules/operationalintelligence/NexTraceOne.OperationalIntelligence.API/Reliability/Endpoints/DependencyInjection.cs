using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Reliability;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

namespace NexTraceOne.OperationalIntelligence.API.Reliability.Endpoints;

/// <summary>
/// Registra serviços do módulo Reliability (Application + Infrastructure).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddReliabilityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReliabilityApplication(configuration);
        services.AddReliabilityInfrastructure(configuration);
        return services;
    }
}
