using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.ChangeIntelligence.Application;
using NexTraceOne.ChangeIntelligence.Infrastructure;

namespace NexTraceOne.ChangeIntelligence.API;

/// <summary>
/// Extensões de registo do módulo ChangeIntelligence no contentor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona todos os serviços do módulo ChangeIntelligence ao contentor DI.</summary>
    public static IServiceCollection AddChangeIntelligenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChangeIntelligenceApplication(configuration);
        services.AddChangeIntelligenceInfrastructure(configuration);
        return services;
    }
}
