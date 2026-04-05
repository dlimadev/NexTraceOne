using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Templates.Persistence;
using NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Repositories;

namespace NexTraceOne.Catalog.Infrastructure.Templates;

/// <summary>
/// Registra serviços de infraestrutura do módulo Templates (Phase 3.1 — Service Templates &amp; Scaffolding).
/// Inclui: DbContext, UnitOfWork e Repositório de templates governados.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Templates ao container DI.</summary>
    public static IServiceCollection AddCatalogTemplatesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<TemplatesDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TemplatesDbContext>());
        services.AddScoped<ITemplatesUnitOfWork>(sp => sp.GetRequiredService<TemplatesDbContext>());
        services.AddScoped<IServiceTemplateRepository, EfServiceTemplateRepository>();

        return services;
    }
}
