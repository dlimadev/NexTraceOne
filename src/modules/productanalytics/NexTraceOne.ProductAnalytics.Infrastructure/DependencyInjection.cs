using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Contracts;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;
using NexTraceOne.ProductAnalytics.Infrastructure.Services;

// Readers concretos sobrescrevem os Null registrados na Application layer

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
        // Provider-aware: ClickHouse for high-cardinality analytic reads, PostgreSQL as default, Elastic as primary.
        services.AddScoped<AnalyticsEventRepository>();

        var analyticsProvider = configuration["Telemetry:ObservabilityProvider:Provider"] ?? "Elastic";
        if (string.Equals(analyticsProvider, "ClickHouse", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<ClickHouseAnalyticsOptions>(
                configuration.GetSection(ClickHouseAnalyticsOptions.SectionName));
            services.AddHttpClient<ClickHouseAnalyticsEventRepository>()
                .AddStandardResilienceHandler();
            services.AddScoped<IAnalyticsEventRepository>(
                sp => sp.GetRequiredService<ClickHouseAnalyticsEventRepository>());
        }
        else if (string.Equals(analyticsProvider, "Elastic", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<AnalyticsOptions>(
                configuration.GetSection(AnalyticsOptions.SectionName));
            services.AddHttpClient<ElasticsearchAnalyticsEventRepository>()
                .AddStandardResilienceHandler();
            services.AddScoped<IAnalyticsEventRepository>(
                sp => sp.GetRequiredService<ElasticsearchAnalyticsEventRepository>());
        }
        else
        {
            services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();
        }
        services.AddScoped<IJourneyDefinitionRepository, JourneyDefinitionRepository>();

        // Readers concretos — sobrescrevem NullPortalAdoptionReader e NullSelfServiceWorkflowReader
        // registrados na Application layer. O último registro vence no .NET DI container.
        services.AddScoped<IPortalAdoptionReader, PortalAdoptionReader>();
        services.AddScoped<ISelfServiceWorkflowReader, SelfServiceWorkflowReader>();

        // Analytics forwarder — propaga eventos do PostgreSQL para o analytics store (Elastic/ClickHouse)
        services.AddScoped<IAnalyticsEventForwarder, AnalyticsEventForwarder>();

        // Cross-module contract — consumed by Governance for adoption metrics
        services.AddScoped<IProductAnalyticsModule, ProductAnalyticsModuleService>();

        return services;
    }
}
