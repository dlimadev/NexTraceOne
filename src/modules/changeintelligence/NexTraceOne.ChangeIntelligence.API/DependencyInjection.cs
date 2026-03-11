using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.ChangeIntelligence.Application;
using NexTraceOne.ChangeIntelligence.Infrastructure;

namespace NexTraceOne.ChangeIntelligence.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo ChangeIntelligence.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChangeIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChangeIntelligenceApplication(configuration);
        services.AddChangeIntelligenceInfrastructure(configuration);
        return services;
    }
}
