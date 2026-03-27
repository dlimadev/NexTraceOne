using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.ProductAnalytics.Application;
using NexTraceOne.ProductAnalytics.Infrastructure;

namespace NexTraceOne.ProductAnalytics.API;

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
