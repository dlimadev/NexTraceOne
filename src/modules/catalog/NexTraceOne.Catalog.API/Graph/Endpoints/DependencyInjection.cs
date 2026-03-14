using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Catalog.Application.Graph;
using NexTraceOne.Catalog.Infrastructure.Graph;

namespace NexTraceOne.Catalog.API.Graph;

/// <summary>
/// Registra serviços específicos da camada API do módulo Catalog Graph.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogGraphModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCatalogGraphApplication(configuration);
        services.AddCatalogGraphInfrastructure(configuration);
        return services;
    }
}
