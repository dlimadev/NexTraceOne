using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Graph.Services;

namespace NexTraceOne.Catalog.Infrastructure.Graph;

/// <summary>
/// Registra serviços de infraestrutura do módulo Catalog Graph.
/// Inclui: DbContext, Repositórios (ativos, snapshots, health, saved views),
/// Adapters externos e integração cross-module via Contracts.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogGraphInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("CatalogDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<CatalogGraphDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogGraphDbContext>());

        // ── Repositórios de ativos (existentes) ──────────────────────────
        services.AddScoped<IApiAssetRepository, ApiAssetRepository>();
        services.AddScoped<IServiceAssetRepository, ServiceAssetRepository>();

        // ── Repositórios de temporalidade, overlays e saved views ────────
        services.AddScoped<IGraphSnapshotRepository, GraphSnapshotRepository>();
        services.AddScoped<INodeHealthRepository, NodeHealthRepository>();
        services.AddScoped<ISavedGraphViewRepository, SavedGraphViewRepository>();

        // ── Integração cross-module via Contracts ────────────────────────
        services.AddScoped<ICatalogGraphModule, CatalogGraphModuleService>();

        return services;
    }
}
