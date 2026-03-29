using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence;
using NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets;

/// <summary>
/// Registra serviços de infraestrutura do sub-domínio Legacy Assets do módulo Catalog.
/// Inclui: DbContext, UnitOfWork e Repositórios de ativos mainframe.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogLegacyAssetsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<LegacyAssetsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<ILegacyAssetsUnitOfWork>(sp => sp.GetRequiredService<LegacyAssetsDbContext>());

        // ── Repositórios de ativos legacy ─────────────────────────────────
        services.AddScoped<IMainframeSystemRepository, MainframeSystemRepository>();
        services.AddScoped<ICobolProgramRepository, CobolProgramRepository>();
        services.AddScoped<ICopybookRepository, CopybookRepository>();
        services.AddScoped<ICicsTransactionRepository, CicsTransactionRepository>();
        services.AddScoped<IImsTransactionRepository, ImsTransactionRepository>();
        services.AddScoped<IDb2ArtifactRepository, Db2ArtifactRepository>();
        services.AddScoped<IZosConnectBindingRepository, ZosConnectBindingRepository>();

        return services;
    }
}
