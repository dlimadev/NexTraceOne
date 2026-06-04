using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.ProductAnalytics;
using NexTraceOne.Catalog.Infrastructure.ProductAnalytics;

namespace NexTraceOne.Catalog.API.ProductAnalytics;

/// <summary>
/// Registra serviços do módulo Product Analytics (Application + Infrastructure).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona o módulo Product Analytics ao contêiner de DI.</summary>
    public static IServiceCollection AddProductAnalyticsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddProductAnalyticsApplication(configuration);
        services.AddProductAnalyticsInfrastructure(configuration);
        return services;
    }
}
