using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints;

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
