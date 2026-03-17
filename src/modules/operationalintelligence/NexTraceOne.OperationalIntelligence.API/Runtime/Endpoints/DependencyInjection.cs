using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.OperationalIntelligence.Application.Runtime;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime;

namespace NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo RuntimeIntelligence.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRuntimeIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRuntimeIntelligenceApplication(configuration);
        services.AddRuntimeIntelligenceInfrastructure(configuration);
        return services;
    }
}
