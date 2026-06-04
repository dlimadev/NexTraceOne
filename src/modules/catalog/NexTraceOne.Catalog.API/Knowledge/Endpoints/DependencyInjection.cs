using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.Knowledge;
using NexTraceOne.Catalog.Infrastructure.Knowledge;

namespace NexTraceOne.Catalog.API.Knowledge.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo Knowledge.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKnowledgeApplication(configuration);
        services.AddKnowledgeInfrastructure(configuration);
        return services;
    }
}
