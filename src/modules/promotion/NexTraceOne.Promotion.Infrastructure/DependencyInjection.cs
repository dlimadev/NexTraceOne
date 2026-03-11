using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Promotion.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Promotion.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPromotionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
