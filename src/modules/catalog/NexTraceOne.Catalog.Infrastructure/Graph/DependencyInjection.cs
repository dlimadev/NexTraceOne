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
using NexTraceOne.Catalog.Infrastructure.Readers;

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

        // ── Repositório de estado de deployment por ambiente ─────────────
        services.AddScoped<IAssetDeploymentStateRepository, AssetDeploymentStateRepository>();

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

        // ── Wave AB.1 — Knowledge Relation Graph (EF Core real reader) ──────
        services.AddScoped<IKnowledgeRelationReader, EfKnowledgeRelationReader>();

        // ── Wave AC.1 — Onboarding Health Report (null reader) ────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IOnboardingHealthReader, NexTraceOne.Catalog.Application.Services.NullOnboardingHealthReader>();

        // ── Wave AF.2 — Retirement Readiness Report (EF Core real reader) ───────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IRetirementReadinessReader,
            NexTraceOne.Catalog.Infrastructure.Services.EfRetirementReadinessReader>();

        // ── Wave AF.3 — Migration Progress Report (null reader) ──────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IMigrationProgressReader, NexTraceOne.Catalog.Application.Services.NullMigrationProgressReader>();

        // ── Wave AF.1 — Service Lifecycle Transition Report (EF Core real reader) ──
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IServiceLifecycleReader, EfServiceLifecycleReader>();

        // ── Wave AD.2 — Secrets Exposure Risk Report (null reader) ───────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.ISecretsExposureReader, NexTraceOne.Catalog.Application.Services.NullSecretsExposureReader>();

        // ── Wave AM — EF Core real readers ────────────────────────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IUncatalogedServicesReader, EfUncatalogedServicesReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IContractDriftReader, NexTraceOne.Catalog.Application.Contracts.NullContractDriftReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.ICatalogHealthMaintenanceReader, EfCatalogHealthMaintenanceReader>();

        // ── Wave AO — Supply Chain readers ──────────────────────────────────
        // ISbomRepository — real EF Core implementation registered in Contracts/DependencyInjection.cs
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISbomCoverageReader,
            EfSbomCoverageReader>();
        // IVulnerabilityExposureReader — cruza CatalogGraphDbContext + DependencyGovernanceDbContext
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IVulnerabilityExposureReader,
            EfVulnerabilityExposureReader>();
        // IDependencyProvenanceReader — cruza ContractsDbContext (SbomRecords) por proveniência de componente
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IDependencyProvenanceReader,
            EfDependencyProvenanceReader>();
        // ISupplyChainRiskReader — cruza ContractsDbContext (SbomRecords) + CatalogGraphDbContext (ServiceAssets)
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISupplyChainRiskReader,
            EfSupplyChainRiskReader>();

        // ── Wave AQ.2 — Code Quality & Static Analysis (EF Core real reader) ──
        // ICodeQualityRepository registered in Contracts/DependencyInjection.cs
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ICodeQualityReader,
            EfCodeQualityReader>();

        // ── Wave AE.1 — Contract Test Coverage Report (EF Core real reader) ─────
        // Cruza ContractsDbContext (ContractVerifications, ConsumerExpectations) + CatalogGraphDbContext (ServiceAssets)
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IContractTestReader,
            EfContractTestReader>();

        // ── Wave AQ — Schema Quality & Evolution Safety (EF Core real readers) ───
        // IDataContractRepository — real EF Core implementation registered in Contracts/DependencyInjection.cs
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISchemaQualityReader,
            EfSchemaQualityReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ISchemaEvolutionSafetyReader,
            EfSchemaEvolutionSafetyReader>();

        // ── Wave AR — Service Topology Intelligence (EF Core real readers) ──────
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IServiceTopologyReader,
            EfServiceTopologyReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.ICriticalPathReader,
            EfCriticalPathReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IDependencyVersionAlignmentReader,
            NexTraceOne.Catalog.Application.Contracts.NullDependencyVersionAlignmentReader>();

        // ── Wave AV — Contract Lifecycle Automation null readers ─────────────
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IContractDeprecationPipelineReader,
            NexTraceOne.Catalog.Application.Contracts.NullContractDeprecationPipelineReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IApiVersionStrategyReader,
            NexTraceOne.Catalog.Application.Contracts.NullApiVersionStrategyReader>();
        // IDeprecationScheduleRepository — real EF Core implementation registered in Contracts/DependencyInjection.cs
        services.AddScoped<NexTraceOne.Catalog.Application.Contracts.Abstractions.IContractDeprecationForecastReader,
            NexTraceOne.Catalog.Application.Contracts.NullContractDeprecationForecastReader>();

        return services;
    }
}
