using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Contracts;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;
using NexTraceOne.ProductAnalytics.Infrastructure.Services;

namespace NexTraceOne.ProductAnalytics.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Product Analytics.
/// Inclui: DbContext, Repositórios.
///
/// P2.3: AnalyticsEvent extraído de GovernanceDbContext para cá.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Product Analytics.</summary>
    public static IServiceCollection AddProductAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("ProductAnalyticsDatabase", "NexTraceOne");

        services.AddDbContext<ProductAnalyticsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // Repositories — P2.3
        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();

        // Cross-module contract — consumed by Governance for adoption metrics
        services.AddScoped<IProductAnalyticsModule, ProductAnalyticsModuleService>();

        return services;
    }
}
