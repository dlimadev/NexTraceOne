using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Services;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Catalog.Contracts.Portal.ServiceInterfaces;
using NexTraceOne.Catalog.Contracts.Templates.ServiceInterfaces;
using NexTraceOne.Catalog.Infrastructure.Contracts.EventHandlers;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Contracts.Services;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Graph.EventHandlers;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Graph.Services;
using NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Portal.Services;
using NexTraceOne.Catalog.Infrastructure.Readers;
using NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Templates.Services;

namespace NexTraceOne.Catalog.Infrastructure;

/// <summary>
/// Registra todos os serviços de infraestrutura do módulo ServiceCatalog.
/// Consolida CatalogGraph + Contracts + DependencyGovernance + DeveloperExperience +
/// LegacyAssets + Templates + DeveloperPortal num único DbContext e DI.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo ServiceCatalog ao container DI.</summary>
    public static IServiceCollection AddServiceCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ServiceCatalogDatabase", "NexTraceOne");

        services.AddDbContext<ServiceCatalogDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>());

            if (string.Equals(
                Environment.GetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            {
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<ICatalogGraphUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<IContractsUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<IDependencyGovernanceUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<IDeveloperExperienceUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<ILegacyAssetsUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<ITemplatesUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());
        services.AddScoped<IPortalUnitOfWork>(sp => sp.GetRequiredService<ServiceCatalogDbContext>());

        // ── Catalog Graph ─────────────────────────────────────────────────────
        services.AddScoped<IApiAssetRepository, ApiAssetRepository>();
        services.AddScoped<IServiceAssetRepository, ServiceAssetRepository>();
        services.AddScoped<IServiceLinkRepository, ServiceLinkRepository>();
        services.AddScoped<IFrameworkAssetDetailRepository, FrameworkAssetDetailRepository>();
        services.AddScoped<IDiscoveredServiceRepository, DiscoveredServiceRepository>();
        services.AddScoped<IDiscoveryRunRepository, DiscoveryRunRepository>();
        services.AddScoped<IDiscoveryMatchRuleRepository, DiscoveryMatchRuleRepository>();
        services.AddScoped<IServiceDiscoveryProvider, OtelServiceDiscoveryProvider>();
        services.AddScoped<IAssetDeploymentStateRepository, AssetDeploymentStateRepository>();
        services.AddScoped<IGraphSnapshotRepository, GraphSnapshotRepository>();
        services.AddScoped<INodeHealthRepository, NodeHealthRepository>();
        services.AddScoped<ISavedGraphViewRepository, SavedGraphViewRepository>();
        services.AddScoped<ILinkedReferenceRepository, LinkedReferenceRepository>();
        services.AddScoped<IIntegrationEventHandler<ReleasePublishedEvent>, ReleasePublishedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<DeploymentEventReceivedEvent>, DeploymentEventReceivedCatalogHandler>();
        services.AddScoped<ICatalogGraphModule, CatalogGraphModuleService>();
        services.AddScoped<IDxScoreRepository, DxScoreRepository>();
        services.AddScoped<IProductivitySnapshotRepository, ProductivitySnapshotRepository>();
        services.AddScoped<IServiceInterfaceRepository, ServiceInterfaceRepository>();
        services.AddScoped<IContractBindingRepository, ContractBindingRepository>();

        // ── Contracts ─────────────────────────────────────────────────────────
        services.AddScoped<IContractVersionRepository, ContractVersionRepository>();
        services.AddScoped<IContractDraftRepository, ContractDraftRepository>();
        services.AddScoped<IContractReviewRepository, ContractReviewRepository>();
        services.AddScoped<ISoapContractDetailRepository, SoapContractDetailRepository>();
        services.AddScoped<ISoapDraftMetadataRepository, SoapDraftMetadataRepository>();
        services.AddScoped<IEventContractDetailRepository, EventContractDetailRepository>();
        services.AddScoped<IEventDraftMetadataRepository, EventDraftMetadataRepository>();
        services.AddScoped<IBackgroundServiceContractDetailRepository, BackgroundServiceContractDetailRepository>();
        services.AddScoped<IBackgroundServiceDraftMetadataRepository, BackgroundServiceDraftMetadataRepository>();
        services.AddScoped<IContractDeploymentRepository, ContractDeploymentRepository>();
        services.AddScoped<ICanonicalEntityRepository, CanonicalEntityRepository>();
        services.AddScoped<ICanonicalEntityVersionRepository, CanonicalEntityVersionRepository>();
        services.AddScoped<IConsumerExpectationRepository, ConsumerExpectationRepository>();
        services.AddScoped<IContractHealthScoreRepository, ContractHealthScoreRepository>();
        services.AddScoped<IPipelineExecutionRepository, PipelineExecutionRepository>();
        services.AddScoped<IContractNegotiationRepository, ContractNegotiationRepository>();
        services.AddScoped<INegotiationCommentRepository, NegotiationCommentRepository>();
        services.AddScoped<ISchemaEvolutionAdviceRepository, SchemaEvolutionAdviceRepository>();
        services.AddScoped<ISemanticDiffResultRepository, SemanticDiffResultRepository>();
        services.AddScoped<IContractComplianceGateRepository, ContractComplianceGateRepository>();
        services.AddScoped<IContractComplianceResultRepository, ContractComplianceResultRepository>();
        services.AddScoped<IContractListingRepository, ContractListingRepository>();
        services.AddScoped<IMarketplaceReviewRepository, MarketplaceReviewRepository>();
        services.AddScoped<IImpactSimulationRepository, ImpactSimulationRepository>();
        services.AddScoped<IContractVerificationRepository, ContractVerificationRepository>();
        services.AddScoped<IContractChangelogRepository, ContractChangelogRepository>();
        services.AddScoped<IGraphQlSchemaSnapshotRepository, GraphQlSchemaSnapshotRepository>();
        services.AddScoped<IProtobufSchemaSnapshotRepository, ProtobufSchemaSnapshotRepository>();
        services.AddScoped<IDataContractSchemaRepository, DataContractSchemaRepository>();
        services.AddScoped<IContractConsumerInventoryRepository, ContractConsumerInventoryRepository>();
        services.AddScoped<IBreakingChangeProposalRepository, BreakingChangeProposalRepository>();
        services.AddScoped<ISbomRepository, EfSbomRepository>();
        services.AddScoped<ICodeQualityRepository, EfCodeQualityRepository>();
        services.AddScoped<IDataContractRepository, EfDataContractRepository>();
        services.AddScoped<IDeprecationScheduleRepository, EfDeprecationScheduleRepository>();
        services.AddScoped<IFeatureFlagRepository, EfFeatureFlagRepository>();
        services.AddScoped<IAiDraftGenerator, AiDraftGeneratorService>();
        services.AddScoped<IContractsModule, ContractsModuleService>();
        services.AddScoped<IIntegrationEventHandler<DeploymentCompletedIntegrationEvent>,
            DeploymentCompletedContractImpactHandler>();

        // ── Dependency Governance ─────────────────────────────────────────────
        services.AddScoped<IServiceDependencyProfileRepository, ServiceDependencyProfileRepository>();
        services.AddScoped<IVulnerabilityAdvisoryRepository, EfVulnerabilityAdvisoryRepository>();
        services.AddScoped<IDependencyEnrichmentService, DependencyEnrichmentService>();

        services.AddHttpClient<OSVVulnerabilityClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.osv.dev/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IVulnerabilityDataSource>(sp =>
            sp.GetRequiredService<OSVVulnerabilityClient>());

        services.AddHttpClient<NuGetPackageClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.nuget.org/v3/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IPackageMetadataClient>(sp =>
            sp.GetRequiredService<NuGetPackageClient>());

        services.AddHttpClient<ILlmCompletionClient, OllamaCompletionClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ── Legacy Assets ─────────────────────────────────────────────────────
        services.AddScoped<IMainframeSystemRepository, MainframeSystemRepository>();
        services.AddScoped<ICobolProgramRepository, CobolProgramRepository>();
        services.AddScoped<ICopybookRepository, CopybookRepository>();
        services.AddScoped<ICicsTransactionRepository, CicsTransactionRepository>();
        services.AddScoped<IImsTransactionRepository, ImsTransactionRepository>();
        services.AddScoped<IDb2ArtifactRepository, Db2ArtifactRepository>();
        services.AddScoped<IZosConnectBindingRepository, ZosConnectBindingRepository>();
        services.AddScoped<ICopybookVersionRepository, CopybookVersionRepository>();
        services.AddScoped<ILegacyDependencyRepository, LegacyDependencyRepository>();

        // ── Templates ─────────────────────────────────────────────────────────
        services.AddScoped<IServiceTemplateRepository, EfServiceTemplateRepository>();
        services.AddScoped<ICatalogTemplatesModule, CatalogTemplatesModuleService>();

        // ── Developer Portal ──────────────────────────────────────────────────
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlaygroundSessionRepository, PlaygroundSessionRepository>();
        services.AddScoped<ICodeGenerationRepository, CodeGenerationRepository>();
        services.AddScoped<IPortalAnalyticsRepository, PortalAnalyticsRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();
        services.AddScoped<IContractPublicationEntryRepository, ContractPublicationEntryRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IApiRateLimitPolicyRepository, RateLimitPolicyRepository>();
        services.AddScoped<IDeveloperPortalModule, DeveloperPortalModuleService>();

        // ── Developer Experience ──────────────────────────────────────────────
        services.AddScoped<IDeveloperSurveyRepository, EfDeveloperSurveyRepository>();
        services.AddScoped<IIdeContextReader, NullIdeContextReader>();
        services.AddScoped<IIDEUsageRepository, EfIdeUsageRepository>();

        // ── Readers (Analytics / Cross-context) ───────────────────────────────
        services.AddScoped<IKnowledgeRelationReader, EfKnowledgeRelationReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IOnboardingHealthReader,
            NexTraceOne.Catalog.Application.Services.NullOnboardingHealthReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IRetirementReadinessReader,
            NexTraceOne.Catalog.Infrastructure.Services.EfRetirementReadinessReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IMigrationProgressReader,
            NexTraceOne.Catalog.Application.Services.NullMigrationProgressReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IServiceLifecycleReader, EfServiceLifecycleReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.ISecretsExposureReader, EfSecretsExposureReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.IUncatalogedServicesReader, EfUncatalogedServicesReader>();
        services.AddScoped<IContractDriftReader, NexTraceOne.Catalog.Application.Contracts.NullContractDriftReader>();
        services.AddScoped<NexTraceOne.Catalog.Application.Services.Abstractions.ICatalogHealthMaintenanceReader, EfCatalogHealthMaintenanceReader>();
        services.AddScoped<ISbomCoverageReader, EfSbomCoverageReader>();
        services.AddScoped<IVulnerabilityExposureReader, EfVulnerabilityExposureReader>();
        services.AddScoped<IDependencyProvenanceReader, EfDependencyProvenanceReader>();
        services.AddScoped<ISupplyChainRiskReader, EfSupplyChainRiskReader>();
        services.AddScoped<ICodeQualityReader, EfCodeQualityReader>();
        services.AddScoped<IContractTestReader, EfContractTestReader>();
        services.AddScoped<ISchemaQualityReader, EfSchemaQualityReader>();
        services.AddScoped<ISchemaEvolutionSafetyReader, EfSchemaEvolutionSafetyReader>();
        services.AddScoped<IServiceTopologyReader, EfServiceTopologyReader>();
        services.AddScoped<ICriticalPathReader, EfCriticalPathReader>();
        services.AddScoped<IDependencyVersionAlignmentReader, EfDependencyVersionAlignmentReader>();
        services.AddScoped<IContractDeprecationPipelineReader, EfContractDeprecationPipelineReader>();
        services.AddScoped<IApiVersionStrategyReader, EfApiVersionStrategyReader>();
        services.AddScoped<IContractDeprecationForecastReader, EfContractDeprecationForecastReader>();
        services.AddScoped<IContractVersionHistoryReader, NullContractVersionHistoryReader>();
        services.AddScoped<IBreakingChangeImpactReader,
            NexTraceOne.Catalog.Application.Contracts.NullBreakingChangeImpactReader>();
        services.AddScoped<IContractCompatibilityReader,
            NexTraceOne.Catalog.Application.Contracts.NullContractCompatibilityReader>();
        services.AddScoped<IEventSchemaEvolutionReader,
            NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventSchemaEvolutionReader>();
        services.AddScoped<IEventProducerConsumerReader,
            NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventProducerConsumerReader>();
        services.AddScoped<IEventComplianceReader,
            NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventComplianceReader>();

        return services;
    }
}
