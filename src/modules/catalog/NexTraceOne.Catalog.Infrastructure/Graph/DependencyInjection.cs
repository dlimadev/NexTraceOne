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

        // ── Repositório de Framework/SDK details ─────────────────────────
        services.AddScoped<IFrameworkAssetDetailRepository, FrameworkAssetDetailRepository>();

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

        // ── Interfaces de serviço e vínculos de contrato ─────────────────
        services.AddScoped<IServiceInterfaceRepository, ServiceInterfaceRepository>();
        services.AddScoped<IContractBindingRepository, ContractBindingRepository>();

        // ── Wave AB.1 — Knowledge Relation Graph (null reader) ────────────
        services.AddScoped<IKnowledgeRelationReader, NexTraceOne.Catalog.Application.Graph.Services.NullKnowledgeRelationReader>();

        // ── Wave AC.1 — Onboarding Health Report (null reader) ────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IOnboardingHealthReader, NexTraceOne.Catalog.Application.Services.NullOnboardingHealthReader>();

        // ── Wave AM — null readers ─────────────────────────────────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IUncatalogedServicesReader, NexTraceOne.Catalog.Application.Services.NullUncatalogedServicesReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IContractDriftReader, NexTraceOne.Catalog.Application.Contracts.NullContractDriftReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.ICatalogHealthMaintenanceReader, NexTraceOne.Catalog.Application.Services.NullCatalogHealthMaintenanceReader>();

        // ── Wave AO — Supply Chain null readers ─────────────────────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISbomRepository,
            NexTraceOne.Catalog.Application.Contracts.NullSbomRepository>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISbomCoverageReader,
            NexTraceOne.Catalog.Application.Contracts.NullSbomCoverageReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IDependencyProvenanceReader,
            NexTraceOne.Catalog.Application.Contracts.NullDependencyProvenanceReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISupplyChainRiskReader,
            NexTraceOne.Catalog.Application.Contracts.NullSupplyChainRiskReader>();

        return services;
    }
}
