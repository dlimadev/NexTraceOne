using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Graph.EventHandlers;
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

        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<CatalogGraphDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogGraphDbContext>());
        services.AddScoped<ICatalogGraphUnitOfWork>(sp => sp.GetRequiredService<CatalogGraphDbContext>());

        // ── Repositórios de ativos (existentes) ──────────────────────────
        services.AddScoped<IApiAssetRepository, ApiAssetRepository>();
        services.AddScoped<IServiceAssetRepository, ServiceAssetRepository>();
        services.AddScoped<IServiceLinkRepository, ServiceLinkRepository>();

        // ── Repositórios de discovery automático ─────────────────────────
        services.AddScoped<IDiscoveredServiceRepository, DiscoveredServiceRepository>();
        services.AddScoped<IDiscoveryRunRepository, DiscoveryRunRepository>();
        services.AddScoped<IDiscoveryMatchRuleRepository, DiscoveryMatchRuleRepository>();

        // ── Provider de discovery via telemetria ─────────────────────────
        services.AddScoped<IServiceDiscoveryProvider, OtelServiceDiscoveryProvider>();

        // ── Repositórios de temporalidade, overlays e saved views ────────
        services.AddScoped<IGraphSnapshotRepository, GraphSnapshotRepository>();
        services.AddScoped<INodeHealthRepository, NodeHealthRepository>();
        services.AddScoped<ISavedGraphViewRepository, SavedGraphViewRepository>();

        // ── Repositório de Source of Truth ───────────────────────────────
        services.AddScoped<ILinkedReferenceRepository, LinkedReferenceRepository>();
        services.AddScoped<IIntegrationEventHandler<ReleasePublishedEvent>, ReleasePublishedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<DeploymentEventReceivedEvent>, DeploymentEventReceivedCatalogHandler>();

        // ── Integração cross-module via Contracts ────────────────────────
        services.AddScoped<ICatalogGraphModule, CatalogGraphModuleService>();

        // P5.2 — Developer Experience Score
        services.AddScoped<IDxScoreRepository, DxScoreRepository>();
        services.AddScoped<IProductivitySnapshotRepository, ProductivitySnapshotRepository>();

        return services;
    }
}
