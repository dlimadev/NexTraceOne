using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Promotion.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo Promotion.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPromotionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPromotionApplication(configuration);
        services.AddPromotionInfrastructure(configuration);
        return services;
    }
}
