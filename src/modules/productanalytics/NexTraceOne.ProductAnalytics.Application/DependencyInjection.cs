using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ProductAnalytics.Application.Abstractions;

namespace NexTraceOne.ProductAnalytics.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Product Analytics.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Product Analytics ao contêiner de DI.</summary>
    public static IServiceCollection AddProductAnalyticsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Null readers ─────────────────────────────────────────────────
        services.AddSingleton<IPortalAdoptionReader, NullPortalAdoptionReader>();
        services.AddSingleton<ISelfServiceWorkflowReader, NullSelfServiceWorkflowReader>();

        return services;
    }
}
